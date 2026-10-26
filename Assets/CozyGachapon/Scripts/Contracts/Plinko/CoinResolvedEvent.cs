namespace CozyGachapon.Contracts
{
    public readonly struct CoinResolvedEvent
    {
        public const int NoBucket = -1;
        public const string NoCoinId = "none";

        public string CoinId { get; }
        public CoinOrigin Origin { get; }
        public int BucketIndex { get; }
        public double Multiplier { get; }
        public double RawValue { get; }
        public int ScoredPegHits { get; }
        public CoinResolveReason Reason { get; }

        public CoinResolvedEvent(
            string coinId,
            CoinOrigin origin,
            int bucketIndex,
            double multiplier,
            double rawValue,
            int scoredPegHits,
            CoinResolveReason reason)
        {
            CoinId = coinId;
            Origin = origin;
            BucketIndex = bucketIndex;
            Multiplier = multiplier;
            RawValue = rawValue;
            ScoredPegHits = scoredPegHits;
            Reason = reason;
        }
    }
}
