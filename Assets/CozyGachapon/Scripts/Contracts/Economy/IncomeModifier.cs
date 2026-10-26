namespace CozyGachapon.Contracts
{
    public readonly struct IncomeModifier
    {
        public string SourceId { get; }
        public IncomeChannel Channel { get; }
        public ModifierTier Tier { get; }
        public double Value { get; }
        public ModifierTarget Target { get; }
        public string TargetKey { get; }
        public bool IsTemporary { get; }

        public IncomeModifier(
            string sourceId,
            IncomeChannel channel,
            ModifierTier tier,
            double value,
            ModifierTarget target,
            string targetKey,
            bool isTemporary)
        {
            SourceId = sourceId;
            Channel = channel;
            Tier = tier;
            Value = value;
            Target = target;
            TargetKey = targetKey;
            IsTemporary = isTemporary;
        }

        public static IncomeModifier ForChannel(
            string sourceId,
            IncomeChannel channel,
            ModifierTier tier,
            double value,
            bool isTemporary = false)
        {
            return new IncomeModifier(sourceId, channel, tier, value, ModifierTarget.Channel, null, isTemporary);
        }

        public static IncomeModifier ForSource(
            string sourceId,
            IncomeChannel channel,
            ModifierTier tier,
            double value,
            string targetKey,
            bool isTemporary = false)
        {
            return new IncomeModifier(sourceId, channel, tier, value, ModifierTarget.Source, targetKey, isTemporary);
        }
    }
}
