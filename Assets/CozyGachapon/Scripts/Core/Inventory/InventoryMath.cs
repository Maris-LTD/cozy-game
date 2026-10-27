using System;
using System.Globalization;

namespace CozyGachapon.Core.Inventory
{
    public static class InventoryMath
    {
        public static int AddCopiesClamped(int copiesBefore, int amount, out bool isNew)
        {
            isNew = copiesBefore == 0;
            long sum = (long)copiesBefore + amount;
            if (sum > int.MaxValue)
            {
                return int.MaxValue;
            }
            return (int)sum;
        }

        public static double CalculateCollectionProgress(int releasedOwned, int releasedSpeciesCount)
        {
            if (releasedSpeciesCount <= 0)
            {
                return 0.0;
            }

            double ratio = (double)releasedOwned / releasedSpeciesCount;
            if (ratio < 0.0)
            {
                return 0.0;
            }
            if (ratio > 1.0)
            {
                return 1.0;
            }
            return ratio;
        }

        public static string FormatCopiesBadge(int copies, int copiesDisplayMax = 99)
        {
            if (copies <= copiesDisplayMax)
            {
                return "x" + copies.ToString(CultureInfo.InvariantCulture);
            }
            return "x" + copiesDisplayMax.ToString(CultureInfo.InvariantCulture) + "+";
        }
    }
}
