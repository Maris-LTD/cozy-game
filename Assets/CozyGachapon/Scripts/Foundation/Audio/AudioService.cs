using System;
using CozyGachapon.Contracts;
using UnityEngine;

namespace CozyGachapon.Audio
{
    public sealed class AudioService : IAudioService
    {
        private const string SoundOnCueId = "Ui.SoundOn";
        private const int NoCueIndex = -1;

        private readonly AudioCounters _counters = new AudioCounters();
        private IVoiceBackend _backend;
        private IAudioClock _clock;
        private IAudioDiagnosticsSink _sink;
        private CueRuntimeTable _cues;
        private VoicePool _pool;
        private AudioPlayPipeline _pipeline;
        private BusStateTable _busTable;
        private BusBackgroundController _background;
        private VoiceGainUpdater _gainUpdater;
        private AudioUserSettings _settings;
        private AudioMuteCoordinator _mute;
        private AudioFailedSettings _failedSettings;
        private MusicStarter _music;
        private int _soundOnIndex = NoCueIndex;

        public event Action<bool> MuteChanged;

        public bool IsReady { get; private set; }
        public AudioRuntimeState RuntimeState { get; private set; }
        public bool IsUserMuted => RuntimeState.IsUserMuted;
        public int VoiceCount => _pool == null ? 0 : _pool.Count;
        public AudioStats Stats => _counters.ToStats(_pool == null ? 0 : _pool.ActiveCount);

        public AudioService()
        {
            RuntimeState = new AudioRuntimeState();
        }

        public bool Initialize(
            AudioConfig config,
            SoundCueRepository repository,
            IVoiceBackend backend,
            IAudioClock clock,
            IAudioDiagnosticsSink sink,
            IAudioRandom random,
            IAudioSettingsStore settingsStore)
        {
            if (IsReady || RuntimeState.Lifecycle == AudioLifecycle.Failed)
            {
                return false;
            }

            if (!AudioServiceInputValidator.HasValidInputs(config, repository, backend, clock, sink, random, settingsStore))
            {
                if (sink != null)
                {
                    sink.ReportError(AudioDiagnosticCode.InitializeInvalidInput, null);
                }

                return false;
            }

            _backend = backend;
            _clock = clock;
            _sink = sink;
            _cues = new CueRuntimeTable(repository, sink);
            _pool = new VoicePool(config.TotalVoices, config.MusicVoices, config.AmbienceVoices);
            _busTable = new BusStateTable(config, RuntimeState);
            _background = new BusBackgroundController(config, RuntimeState);
            AudioSuspendController suspend = new AudioSuspendController(config, RuntimeState);
            UserMuteController userMute = new UserMuteController(config, RuntimeState);
            MusicFadeIn musicFade = new MusicFadeIn(config);
            VoiceGainCalculator gain = new VoiceGainCalculator(config, RuntimeState, _busTable, _background, suspend, userMute, musicFade);
            _gainUpdater = new VoiceGainUpdater(_pool, _cues, backend, gain);
            _pipeline = new AudioPlayPipeline(
                config,
                backend,
                _cues,
                _pool,
                _counters,
                new PlayAdmission(RuntimeState, _cues, _pool, new InstanceCapResolver(_pool), new VoiceStealer(_pool, _cues)),
                new PitchResolver(config, random, new CueClipSelector(_cues.Count)),
                gain);
            _music = new MusicStarter(RuntimeState, _cues, _pipeline, musicFade);
            _settings = new AudioUserSettings(config, settingsStore);
            _mute = new AudioMuteCoordinator(
                suspend, userMute, _pool, backend, _gainUpdater, _settings, new AdMuteHandler(RuntimeState, _pool, backend, _cues));
            _busTable.SetUserVolume(AudioBus.Music, _settings.ReadSlider(AudioBus.Music));
            _busTable.SetUserVolume(AudioBus.Sfx, _settings.ReadSlider(AudioBus.Sfx));
            _mute.Restore(_settings.ReadUserMuted());
            _soundOnIndex = _cues.TryResolve(SoundOnCueId, out int soundOnIndex) ? soundOnIndex : NoCueIndex;
            RuntimeState.SetLifecycle(AudioLifecycle.Active);
            IsReady = true;
            return true;
        }

        public void EnterFailed(IAudioSettingsStore settingsStore)
        {
            if (IsReady)
            {
                return;
            }

            RuntimeState.SetLifecycle(AudioLifecycle.Failed);
            _cues = null;
            _pool = null;
            _pipeline = null;
            _failedSettings = new AudioFailedSettings(RuntimeState, settingsStore);
            _failedSettings.Restore();
        }

        public bool TryResolveCue(string cueId, out int cueIndex)
        {
            if (!IsReady)
            {
                cueIndex = -1;
                return false;
            }

            return _cues.TryResolve(cueId, out cueIndex);
        }

        public PlayResult Play(string cueId, in AudioPlayParams playParams)
        {
            if (!IsReady)
            {
                return Drop(DropReason.NotReady);
            }

            if (!_cues.TryResolve(cueId, out int cueIndex))
            {
                _sink.ReportWarning(AudioDiagnosticCode.UnknownCue, cueId);
                return Drop(DropReason.UnknownCue);
            }

            return _pipeline.Admit(cueIndex, _clock.Now, playParams);
        }

        public PlayResult Play(int cueIndex, in AudioPlayParams playParams)
        {
            if (!IsReady)
            {
                return Drop(DropReason.NotReady);
            }

            if (!_cues.IsKnown(cueIndex))
            {
                _sink.ReportWarning(AudioDiagnosticCode.UnknownCue, null);
                return Drop(DropReason.UnknownCue);
            }

            return _pipeline.Admit(cueIndex, _clock.Now, playParams);
        }

        public void Tick()
        {
            if (!IsReady)
            {
                return;
            }

            _pool.Tick(_backend, _cues);
            double now = _clock.Now;
            bool backgroundRefresh = _background.ConsumeRefresh(now);
            bool musicRefresh = _music.Tick(now);
            if (_mute.Tick(now, out bool soundOnDue) || backgroundRefresh || musicRefresh)
            {
                _gainUpdater.RefreshPlaying(now);
            }

            if (soundOnDue && _soundOnIndex != NoCueIndex)
            {
                _pipeline.Admit(_soundOnIndex, _clock.Now, AudioPlayParams.None);
            }
        }

        public void SetBusBackground(AudioBus bus, bool background)
        {
            if (IsReady)
            {
                _background.Set(bus, background, _clock.Now);
            }
        }

        public void SetUserVolume(AudioBus bus, float slider)
        {
            if (IsReady && _busTable.SetUserVolume(bus, slider))
            {
                _settings.SaveSlider(bus, _busTable.GetUserSlider(bus));
                _gainUpdater.RefreshPlaying(_clock.Now);
            }
        }

        public float GetUserVolume(AudioBus bus)
        {
            return IsReady ? _busTable.GetUserSlider(bus) : 0f;
        }

        public void SetUserMuted(bool muted)
        {
            SetMuteReason(MuteReason.User, muted);
        }

        public void SetMuteReason(MuteReason reason, bool active)
        {
            if (!IsReady && RuntimeState.Lifecycle == AudioLifecycle.Uninitialized)
            {
                RuntimeState.SetSuspendReason(reason, active);
                return;
            }

            bool userChanged = IsReady
                ? _mute.Apply(reason, active, _clock.Now)
                : _failedSettings != null && _failedSettings.Apply(reason, active);
            if (userChanged)
            {
                MuteChanged?.Invoke(RuntimeState.IsUserMuted);
            }
        }

        public void HandlePaused(in PausedEvent pausedEvent)
        {
            SetMuteReason(MuteReason.Background, true);
        }

        public void HandleResumed(in ResumedEvent resumedEvent)
        {
            SetMuteReason(MuteReason.Background, false);
        }

        private PlayResult Drop(DropReason reason)
        {
            _counters.RecordDropped(reason);
            return PlayResult.Dropped(reason);
        }
    }
}
