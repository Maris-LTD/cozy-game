namespace CozyGachapon.Contracts
{
    public readonly struct TabChangedEvent
    {
        public AppTab Previous { get; }
        public AppTab Current { get; }

        public TabChangedEvent(AppTab previous, AppTab current)
        {
            Previous = previous;
            Current = current;
        }
    }
}
