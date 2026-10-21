namespace CozyGachapon.Contracts
{
    public readonly struct InteractiveEvent
    {
        public float BootToInteractiveSeconds { get; }

        public InteractiveEvent(float bootToInteractiveSeconds)
        {
            BootToInteractiveSeconds = bootToInteractiveSeconds;
        }
    }
}
