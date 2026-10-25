using CozyGachapon.Contracts;

namespace CozyGachapon.Audio
{
    public sealed class PlayAdmission
    {
        private readonly AudioRuntimeState _state;
        private readonly CueRuntimeTable _cues;
        private readonly VoicePool _pool;
        private readonly InstanceCapResolver _capResolver;
        private readonly VoiceStealer _stealer;

        public PlayAdmission(
            AudioRuntimeState state,
            CueRuntimeTable cues,
            VoicePool pool,
            InstanceCapResolver capResolver,
            VoiceStealer stealer)
        {
            _state = state;
            _cues = cues;
            _pool = pool;
            _capResolver = capResolver;
            _stealer = stealer;
        }

        public DropReason Evaluate(int cueIndex, double now, out int voice, out VoiceAcquisition acquisition)
        {
            voice = -1;
            acquisition = VoiceAcquisition.Free;
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
                return ResolveInstanceCap(cueIndex, cue, out voice, out acquisition);
            }

            return AcquireVoice(cueIndex, out voice, out acquisition);
        }

        private DropReason ResolveInstanceCap(int cueIndex, SoundCue cue, out int voice, out VoiceAcquisition acquisition)
        {
            acquisition = VoiceAcquisition.Free;
            voice = -1;
            if (cue.InstanceCapPolicy == InstanceCapPolicy.DropNew)
            {
                return DropReason.InstanceCap;
            }

            if (!_capResolver.TryFindOldestInstance(cueIndex, out voice))
            {
                return DropReason.NoVoice;
            }

            acquisition = VoiceAcquisition.SameCueSteal;
            return DropReason.None;
        }

        private DropReason AcquireVoice(int cueIndex, out int voice, out VoiceAcquisition acquisition)
        {
            acquisition = VoiceAcquisition.Free;
            VoicePartition partition = _cues.GetPartition(cueIndex);
            if (_pool.TryAcquireFree(partition, out voice))
            {
                return DropReason.None;
            }

            if (partition == VoicePartition.Sfx && _stealer.TryFindVictim(_cues.GetPriority(cueIndex), out voice))
            {
                acquisition = VoiceAcquisition.PrioritySteal;
                return DropReason.None;
            }

            voice = -1;
            return DropReason.NoVoice;
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
