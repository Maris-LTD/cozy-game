using CozyGachapon.Contracts;

namespace CozyGachapon.Economy
{
    public static class IdleCpsCalculator
    {
        public static double Compute(
            IncomeSourceTable sources,
            double idleCpsUnit,
            double boostMultiplier,
            double prestigeMultiplier,
            double globalMultiplier)
        {
            if (sources == null || sources.Count == 0)
            {
                return 0d;
            }

            double sum = 0d;
            for (int i = 0; i < sources.Count; i++)
            {
                if (sources.ChannelAt(i) != IncomeChannel.Idle)
                {
                    continue;
                }

                double baseRate = sources.BaseRateAt(i);
                double flat = sources.FlatUnitsAt(i) * idleCpsUnit;
                double synergy = sources.SynergyAt(i);
                sum += (baseRate + flat) * synergy;
            }

            return sum * boostMultiplier * prestigeMultiplier * globalMultiplier;
        }
    }
}
