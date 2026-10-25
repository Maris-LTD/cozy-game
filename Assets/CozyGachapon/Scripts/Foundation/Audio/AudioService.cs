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
        private AudioConfig _config;
        private IVoiceBackend _backend;
        private IAudioClock _clock;
        private IAudioDiagnosticsSink _sink;
        private CueRuntimeTable _cues;
        private VoicePool _pool;
        private PlayAdmission _admission;
        private PitchResolver _pitch;
        private BusStateTable _busTable;
        private BusBackgroundController _background;
        private VoiceGainCalculator _gain;
        private VoiceGainUpdater _gainUpdater;
        private AudioUserSettings _settings;
        private AudioMuteCoordinator _mute;
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
            if (IsReady)
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

            _config = config;
            _backend = backend;
            _clock = clock;
            _sink = sink;
            _cues = new CueRuntimeTable(repository, sink);
            _pool = new VoicePool(config.TotalVoices, config.MusicVoices, config.AmbienceVoices);
            _admission = new PlayAdmission(
                RuntimeState,
                _cues,
                _pool,
                new InstanceCapResolver(_pool),
                new VoiceStealer(_pool, _cues));
            _pitch = new PitchResolver(config, random, new CueClipSelector(_cues.Count));
            _busTable = new BusStateTable(config, RuntimeState);
            _background = new BusBackgroundController(config, RuntimeState);
            AudioSuspendController suspend = new AudioSuspendController(config, RuntimeState);
            UserMuteController userMute = new UserMuteController(config, RuntimeState);
            _gain = new VoiceGainCalculator(config, RuntimeState, _busTable, _background, suspend, userMute);
            _gainUpdater = new VoiceGainUpdater(_pool, _cues, backend, _gain);
            _settings = new AudioUserSettings(config, settingsStore);
            _mute = new AudioMuteCoordinator(suspend, userMute, _pool, backend, _gainUpdater, _settings);
            _busTable.SetUserVolume(AudioBus.Music, _settings.ReadSlider(AudioBus.Music));
            _busTable.SetUserVolume(AudioBus.Sfx, _settings.ReadSlider(AudioBus.Sfx));
            _mute.Restore(_settings.ReadUserMuted());
            _soundOnIndex = _cues.TryResolve(SoundOnCueId, out int soundOnIndex) ? soundOnIndex : NoCueIndex;
            IsReady = true;
            return true;
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

            return Admit(cueIndex, playParams);
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

            return Admit(cueIndex, playParams);
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
            if (_mute.Tick(now, out bool soundOnDue) || backgroundRefresh)
            {
                _gainUpdater.RefreshPlaying(now);
            }

            if (soundOnDue && _soundOnIndex != NoCueIndex)
            {
                Admit(_soundOnIndex, AudioPlayParams.None);
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
            if (IsReady && _mute.Apply(reason, active, _clock.Now))
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

        private PlayResult Admit(int cueIndex, in AudioPlayParams playParams)
        {
            double now = _clock.Now;
            DropReason reason = _admission.Evaluate(cueIndex, now, out int voice, out VoiceAcquisition acquisition);
            if (reason != DropReason.None)
            {
                return Drop(reason);
            }

            if (acquisition != VoiceAcquisition.Free)
            {
                RecordSteal(acquisition, voice, now);
                _pool.Evict(voice, _backend, _cues);
            }

            SoundCue cue = _cues.GetCue(cueIndex);
            float volume = _gain.Compute(cue, now);
            float pitch = _pitch.Resolve(cueIndex, cue, playParams, _cues.GetDefaultClip(cueIndex), out AudioClip clip);
            float pan = PanCalculator.Resolve(cue.PanFromPosition, playParams.PanPosition, _config.MaxPan, _backend.SupportsPan);
            _backend.Start(voice, clip, volume, pitch, pan, cue.Loop);
            _pool.Assign(voice, cueIndex, now, cue.Loop);
            _cues.AddInstance(cueIndex);
            _cues.MarkPlayed(cueIndex, now);
            _counters.RecordPlayed(_pool.ActiveCount);
            return PlayResult.Success();
        }

        private void RecordSteal(VoiceAcquisition acquisition, int voice, double now)
        {
            if (acquisition == VoiceAcquisition.SameCueSteal)
            {
                _counters.RecordSameCueSteal(now - _pool.GetSlot(voice).StartTime);
                return;
            }

            _counters.RecordPrioritySteal();
        }

        private PlayResult Drop(DropReason reason)
        {
            _counters.RecordDropped(reason);
            return PlayResult.Dropped(reason);
        }
    }
}
