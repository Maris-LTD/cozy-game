namespace CozyGachapon.Contracts
{
    public readonly struct AudioStats
    {
        public int Requests { get; }
        public int Played { get; }
        public int ActiveVoices { get; }
        public int DroppedNotReady { get; }
        public int DroppedUnknownCue { get; }
        public int DroppedLocked { get; }
        public int DroppedSuspended { get; }
        public int DroppedUserMuted { get; }
        public int DroppedBusSilent { get; }
        public int DroppedBackgroundFiltered { get; }
        public int DroppedRateLimited { get; }
        public int DroppedInstanceCap { get; }
        public int DroppedNoVoice { get; }

        public int DroppedTotal =>
            DroppedNotReady
            + DroppedUnknownCue
            + DroppedLocked
            + DroppedSuspended
            + DroppedUserMuted
            + DroppedBusSilent
            + DroppedBackgroundFiltered
            + DroppedRateLimited
            + DroppedInstanceCap
            + DroppedNoVoice;

        public AudioStats(
            int requests,
            int played,
            int activeVoices,
            int droppedNotReady,
            int droppedUnknownCue,
            int droppedLocked,
            int droppedSuspended,
            int droppedUserMuted,
            int droppedBusSilent,
            int droppedBackgroundFiltered,
            int droppedRateLimited,
            int droppedInstanceCap,
            int droppedNoVoice)
        {
            Requests = requests;
            Played = played;
            ActiveVoices = activeVoices;
            DroppedNotReady = droppedNotReady;
            DroppedUnknownCue = droppedUnknownCue;
            DroppedLocked = droppedLocked;
            DroppedSuspended = droppedSuspended;
            DroppedUserMuted = droppedUserMuted;
            DroppedBusSilent = droppedBusSilent;
            DroppedBackgroundFiltered = droppedBackgroundFiltered;
            DroppedRateLimited = droppedRateLimited;
            DroppedInstanceCap = droppedInstanceCap;
            DroppedNoVoice = droppedNoVoice;
        }

        public int GetDropped(DropReason reason)
        {
            switch (reason)
            {
                case DropReason.NotReady:
                    return DroppedNotReady;
                case DropReason.UnknownCue:
                    return DroppedUnknownCue;
                case DropReason.Locked:
                    return DroppedLocked;
                case DropReason.Suspended:
                    return DroppedSuspended;
                case DropReason.UserMuted:
                    return DroppedUserMuted;
                case DropReason.BusSilent:
                    return DroppedBusSilent;
                case DropReason.BackgroundFiltered:
                    return DroppedBackgroundFiltered;
                case DropReason.RateLimited:
                    return DroppedRateLimited;
                case DropReason.InstanceCap:
                    return DroppedInstanceCap;
                case DropReason.NoVoice:
                    return DroppedNoVoice;
                default:
                    return 0;
            }
        }
    }
}
