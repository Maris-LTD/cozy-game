using CozyGachapon.Contracts;
using UnityEngine;

namespace CozyGachapon.Audio
{
    public sealed class AudioPlayPipeline
    {
        private readonly AudioConfig _config;
        private readonly IVoiceBackend _backend;
        private readonly CueRuntimeTable _cues;
        private readonly VoicePool _pool;
        private readonly AudioCounters _counters;
        private readonly PlayAdmission _admission;
        private readonly PitchResolver _pitch;
        private readonly VoiceGainCalculator _gain;

        public AudioPlayPipeline(
            AudioConfig config,
            IVoiceBackend backend,
            CueRuntimeTable cues,
            VoicePool pool,
            AudioCounters counters,
            PlayAdmission admission,
            PitchResolver pitch,
            VoiceGainCalculator gain)
        {
            _config = config;
            _backend = backend;
            _cues = cues;
            _pool = pool;
            _counters = counters;
            _admission = admission;
            _pitch = pitch;
            _gain = gain;
        }

        public PlayResult Admit(int cueIndex, double now, in AudioPlayParams playParams)
        {
            DropReason reason = _admission.Evaluate(cueIndex, now, out int voice, out VoiceAcquisition acquisition);
            if (reason != DropReason.None)
            {
                _counters.RecordDropped(reason);
                return PlayResult.Dropped(reason);
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
    }
}
