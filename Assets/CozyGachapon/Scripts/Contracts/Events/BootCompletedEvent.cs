namespace CozyGachapon.Contracts
{
    public readonly struct BootCompletedEvent
    {
        public float DurationSeconds { get; }

        public BootCompletedEvent(float durationSeconds)
        {
            DurationSeconds = durationSeconds;
        }
    }
}
