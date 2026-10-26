namespace CozyGachapon.Economy
{
    public static class ChannelIncomeCalculator
    {
        public static double Compute(
            double baseValue,
            double flatValue,
            double synergy,
            double boost,
            double prestige,
            double global)
        {
            return (baseValue + flatValue) * synergy * boost * prestige * global;
        }
    }
}
