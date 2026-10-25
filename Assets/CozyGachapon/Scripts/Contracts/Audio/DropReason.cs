namespace CozyGachapon.Contracts
{
    public enum DropReason
    {
        None,
        NotReady,
        UnknownCue,
        Locked,
        Suspended,
        UserMuted,
        BusSilent,
        BackgroundFiltered,
        RateLimited,
        InstanceCap,
        NoVoice
    }
}
