namespace CozyGachapon.Contracts
{
    public readonly struct BootFailedEvent
    {
        public string StepId { get; }
        public BootPhase Phase { get; }
        public string Reason { get; }

        public BootFailedEvent(string stepId, BootPhase phase, string reason)
        {
            StepId = stepId;
            Phase = phase;
            Reason = reason;
        }
    }
}
