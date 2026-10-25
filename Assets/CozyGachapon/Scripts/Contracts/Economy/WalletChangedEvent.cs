namespace CozyGachapon.Contracts
{
    public readonly struct WalletChangedEvent
    {
        public string CurrencyId { get; }
        public double Previous { get; }
        public double Current { get; }
        public double Delta { get; }
        public WalletChangeReason Reason { get; }

        public WalletChangedEvent(string currencyId, double previous, double current, double delta, WalletChangeReason reason)
        {
            CurrencyId = currencyId;
            Previous = previous;
            Current = current;
            Delta = delta;
            Reason = reason;
        }
    }
}
