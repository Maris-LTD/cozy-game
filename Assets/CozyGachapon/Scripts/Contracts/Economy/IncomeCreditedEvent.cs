namespace CozyGachapon.Contracts
{
    public readonly struct IncomeCreditedEvent
    {
        public const int NoBucket = -1;
        public const string NoCoinId = "none";

        public IncomeChannel Channel { get; }
        public string CoinId { get; }
        public int BucketIndex { get; }
        public CoinOrigin Origin { get; }
        public CoinResolveReason Reason { get; }
        public string SourceKey { get; }
        public double BaseAmount { get; }
        public double CreditedAmount { get; }

        public IncomeCreditedEvent(
            IncomeChannel channel,
            string coinId,
            int bucketIndex,
            CoinOrigin origin,
            CoinResolveReason reason,
            string sourceKey,
            double baseAmount,
            double creditedAmount)
        {
            Channel = channel;
            CoinId = coinId;
            BucketIndex = bucketIndex;
            Origin = origin;
            Reason = reason;
            SourceKey = sourceKey;
            BaseAmount = baseAmount;
            CreditedAmount = creditedAmount;
        }
    }
}
