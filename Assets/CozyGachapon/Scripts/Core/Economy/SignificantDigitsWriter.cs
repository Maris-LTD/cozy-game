using System;

namespace CozyGachapon.Economy
{
    public static class SignificantDigitsWriter
    {
        private const double RelEpsilon = 1e-12d;
        private const long Base = 10L;
        private const long ThreeDigitLimit = 1000L;
        private const char DecimalSeparator = '.';
        private const char ZeroDigit = '0';
        private const double TwoDigitFloor = 10d;
        private const double ThreeDigitFloor = 100d;

        public static int WriteInteger(long value, Span<char> destination, int position)
        {
            int digitCount = CountDigits(value);
            int end = position + digitCount;
            long remaining = value;
            for (int i = end - 1; i >= position; i--)
            {
                destination[i] = (char)(ZeroDigit + (int)(remaining % Base));
                remaining /= Base;
            }

            return end;
        }

        public static int WriteTruncatedMantissa(double mantissa, int sigDigits, Span<char> destination, int position)
        {
            int integerDigits = mantissa >= ThreeDigitFloor ? 3 : mantissa >= TwoDigitFloor ? 2 : 1;
            int decimals = Math.Max(sigDigits - integerDigits, 0);
            return WriteScaled(mantissa, integerDigits, decimals, destination, position);
        }

        public static int WriteTruncatedFixed(double value, int decimals, Span<char> destination, int position)
        {
            int maxIntegerDigits = CountDigits(ThreeDigitLimit - 1L);
            return WriteScaled(value, maxIntegerDigits, Math.Max(decimals, 0), destination, position);
        }

        private static int WriteScaled(double value, int integerDigits, int decimals, Span<char> destination, int position)
        {
            long divisor = PowerOfBase(decimals);
            long limit = PowerOfBase(integerDigits + decimals) - 1L;
            double scaledValue = Math.Floor(value * divisor * (1d + RelEpsilon));
            long scaled = scaledValue >= limit ? limit : (long)scaledValue;
            int cursor = WriteInteger(scaled / divisor, destination, position);
            if (decimals == 0)
            {
                return cursor;
            }

            destination[cursor++] = DecimalSeparator;
            long fraction = scaled % divisor;
            int fractionEnd = cursor + decimals;
            for (int i = fractionEnd - 1; i >= cursor; i--)
            {
                destination[i] = (char)(ZeroDigit + (int)(fraction % Base));
                fraction /= Base;
            }

            return fractionEnd;
        }

        private static int CountDigits(long value)
        {
            int count = 1;
            long remaining = value / Base;
            while (remaining > 0L)
            {
                count++;
                remaining /= Base;
            }

            return count;
        }

        private static long PowerOfBase(int exponent)
        {
            long result = 1L;
            for (int i = 0; i < exponent; i++)
            {
                result *= Base;
            }

            return result;
        }
    }
}
