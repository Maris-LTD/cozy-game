namespace CozyGachapon.Contracts
{
    public readonly struct SpendRejectedEvent
    {
        public string CurrencyId { get; }
        public double Amount { get; }
        public SpendReason Reason { get; }

        public SpendRejectedEvent(string currencyId, double amount, SpendReason reason)
        {
            CurrencyId = currencyId;
            Amount = amount;
            Reason = reason;
        }
    }
}
