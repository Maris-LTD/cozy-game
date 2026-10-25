using CozyGachapon.Contracts;
using UnityEngine;

namespace CozyGachapon.Audio
{
    public sealed class AudioService : IAudioService
    {
        private const float NeutralPitch = 1f;
        private const float CenterPan = 0f;
        private const float MaxLinearVolume = 1f;

        private readonly AudioCounters _counters = new AudioCounters();
        private AudioConfig _config;
        private IVoiceBackend _backend;
        private IAudioClock _clock;
        private IAudioDiagnosticsSink _sink;
        private CueRuntimeTable _cues;
        private VoicePool _pool;
        private PlayAdmission _admission;

        public bool IsReady { get; private set; }
        public AudioRuntimeState RuntimeState { get; private set; }
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
            IAudioDiagnosticsSink sink)
        {
            if (IsReady)
            {
                return false;
            }

            if (!HasValidInputs(config, repository, backend, clock, sink))
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

            return Admit(cueIndex);
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

            return Admit(cueIndex);
        }

        public void Tick()
        {
            if (IsReady)
            {
                _pool.Tick(_backend, _cues);
            }
        }

        private PlayResult Admit(int cueIndex)
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
            float volume = Mathf.Min(MaxLinearVolume, AudioMath.DbToLinear(cue.VolumeDb, _config.SilenceFloorDb));
            _backend.Start(voice, _cues.GetDefaultClip(cueIndex), volume, NeutralPitch, CenterPan, cue.Loop);
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

        private static bool HasValidInputs(
            AudioConfig config,
            SoundCueRepository repository,
            IVoiceBackend backend,
            IAudioClock clock,
            IAudioDiagnosticsSink sink)
        {
            if (config == null || repository == null || backend == null || clock == null || sink == null)
            {
                return false;
            }

            if (config.TotalVoices < 1 || config.MusicVoices < 0 || config.AmbienceVoices < 0)
            {
                return false;
            }

            return config.MusicVoices + config.AmbienceVoices <= config.TotalVoices
                && backend.VoiceCount >= config.TotalVoices;
        }
    }
}
