using CozyGachapon.Contracts;

namespace CozyGachapon.Audio
{
    public sealed class PlayAdmission
    {
        private readonly AudioRuntimeState _state;
        private readonly CueRuntimeTable _cues;
        private readonly VoicePool _pool;

        public PlayAdmission(AudioRuntimeState state, CueRuntimeTable cues, VoicePool pool)
        {
            _state = state;
            _cues = cues;
            _pool = pool;
        }

        public DropReason Evaluate(int cueIndex, double now, out int voice)
        {
            voice = -1;
            SoundCue cue = _cues.GetCue(cueIndex);
            if (_state.IsLocked)
            {
                return DropReason.Locked;
            }

            if (_state.IsSuspended)
            {
                return DropReason.Suspended;
            }

            if (_state.IsUserMuted && cue.Bus != AudioBus.Music)
            {
                return DropReason.UserMuted;
            }

            if (_state.IsBusSilentOnPath(cue.Bus))
            {
                return DropReason.BusSilent;
            }

            if (!cue.KeepWhenBusBackground && _state.IsBusBackgroundOnPath(cue.Bus))
            {
                return DropReason.BackgroundFiltered;
            }

            if (IsRateLimited(cueIndex, cue, now))
            {
                return DropReason.RateLimited;
            }

            if (_cues.GetInstances(cueIndex) >= cue.MaxInstances)
            {
                return cue.InstanceCapPolicy == InstanceCapPolicy.DropNew ? DropReason.InstanceCap : DropReason.NoVoice;
            }

            if (!_pool.TryAcquireFree(_cues.GetPartition(cueIndex), out voice))
            {
                return DropReason.NoVoice;
            }

            return DropReason.None;
        }

        private bool IsRateLimited(int cueIndex, SoundCue cue, double now)
        {
            if (!_cues.HasPlayed(cueIndex))
            {
                return false;
            }

            return (float)(now - _cues.GetLastPlayed(cueIndex)) < cue.MinInterval;
        }
    }
}
