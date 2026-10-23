namespace CozyGachapon.Contracts
{
    public readonly struct AdClosedEvent
    {
        public double AdSeconds { get; }

        public AdClosedEvent(double adSeconds)
        {
            AdSeconds = adSeconds;
        }
    }
}
