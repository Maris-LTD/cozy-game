using System;

namespace CozyGachapon.Economy
{
    public static class NiceCeiling
    {
        private const double NiceEpsilon = 1e-9d;
        private const double Base = 10d;

        public static double Apply(double x, int sigDigits)
        {
            if (!(x > 0d))
            {
                return 0d;
            }

            if (double.IsPositiveInfinity(x))
            {
                return x;
            }

            int digits = Math.Max(sigDigits, 1);
            int exponent = (int)Math.Floor(Math.Log10(x)) - (digits - 1);
            if (exponent < 0)
            {
                exponent = 0;
            }

            double step = Math.Pow(Base, exponent);
            if (x >= step * Math.Pow(Base, digits))
            {
                step *= Base;
            }

            return Math.Ceiling(x / step - NiceEpsilon) * step;
        }
    }
}
