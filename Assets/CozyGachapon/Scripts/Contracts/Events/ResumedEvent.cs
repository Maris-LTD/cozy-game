namespace CozyGachapon.Contracts
{
    public readonly struct ResumedEvent
    {
        public PauseReason Reason { get; }
        public double AwaySeconds { get; }

        public ResumedEvent(PauseReason reason, double awaySeconds)
        {
            Reason = reason;
            AwaySeconds = awaySeconds;
        }
    }
}
