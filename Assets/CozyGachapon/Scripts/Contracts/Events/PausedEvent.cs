namespace CozyGachapon.Contracts
{
    public readonly struct PausedEvent
    {
        public PauseReason Reason { get; }

        public PausedEvent(PauseReason reason)
        {
            Reason = reason;
        }
    }
}
