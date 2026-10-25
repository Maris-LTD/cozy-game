namespace CozyGachapon.Contracts
{
    public readonly struct WelcomeQueueCompletedEvent
    {
        public int ShownStepCount { get; }

        public WelcomeQueueCompletedEvent(int shownStepCount)
        {
            ShownStepCount = shownStepCount;
        }
    }
}
