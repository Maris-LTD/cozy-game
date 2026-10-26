using System;

namespace CozyGachapon.Economy
{
    public static class PlinkoCreditCalculator
    {
        public static double Calculate(
            double rawValue,
            double baseCoinValue,
            double flatUnitsCoin,
            double plinkoFlatPerCoinCap,
            double mSynergy,
            double mBoost,
            double mPrestige,
            double mGlobal)
        {
            if (double.IsNaN(rawValue) || double.IsInfinity(rawValue) || rawValue < 0d)
            {
                return -1d;
            }

            double validFlat = double.IsNaN(flatUnitsCoin) || double.IsInfinity(flatUnitsCoin) || flatUnitsCoin < 0d
                ? 0d
                : flatUnitsCoin;
            double validCap = double.IsNaN(plinkoFlatPerCoinCap) || double.IsInfinity(plinkoFlatPerCoinCap) || plinkoFlatPerCoinCap < 0d
                ? 0d
                : plinkoFlatPerCoinCap;
            double validBase = double.IsNaN(baseCoinValue) || double.IsInfinity(baseCoinValue) || baseCoinValue < 0d
                ? 0d
                : baseCoinValue;

            double cappedFlat = Math.Min(validFlat, validCap);
            double adjusted = rawValue + cappedFlat * validBase;
            double totalMultiplier = mSynergy * mBoost * mPrestige * mGlobal;

            return adjusted * totalMultiplier;
        }
    }
}
