using CozyGachapon.Contracts;

namespace CozyGachapon.Audio
{
    public struct MuteReasonSet
    {
        public const MuteReason SuspendMask = MuteReason.Ad | MuteReason.Background;
        public const MuteReason AllMask = MuteReason.User | SuspendMask;

        public MuteReason Reasons { get; private set; }
        public MuteReason SuspendReasons => Reasons & SuspendMask;
        public bool IsSuspended => SuspendReasons != MuteReason.None;
        public bool IsUserMuted => (Reasons & MuteReason.User) != MuteReason.None;

        public bool Set(MuteReason reason, bool active)
        {
            MuteReason masked = reason & AllMask;
            MuteReason next = active ? Reasons | masked : Reasons & ~masked;
            if (next == Reasons)
            {
                return false;
            }

            Reasons = next;
            return true;
        }
    }
}
