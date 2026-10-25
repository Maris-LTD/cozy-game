using System;
using System.Globalization;

namespace CozyGachapon.Economy
{
    public sealed class ThousandPowerTable
    {
        private const double LogStep = 3d;
        private const int ParseBufferLength = 16;
        private const int ExponentStep = 3;
        private const int ExponentStart = 2;
        private const char ExponentMarker = 'e';
        private const char One = '1';

        private readonly double[] _powers;

        public ThousandPowerTable()
        {
            MaxGroup = (int)Math.Floor(Math.Log10(double.MaxValue) / LogStep);
            _powers = new double[MaxGroup + 1];
            char[] buffer = new char[ParseBufferLength];
            buffer[0] = One;
            buffer[1] = ExponentMarker;
            for (int k = 0; k <= MaxGroup; k++)
            {
                int length = SignificantDigitsWriter.WriteInteger(k * ExponentStep, buffer, ExponentStart);
                _powers[k] = double.Parse(new ReadOnlySpan<char>(buffer, 0, length), NumberStyles.Float, CultureInfo.InvariantCulture);
            }
        }

        public int MaxGroup { get; private set; }

        public double PowerOf(int group)
        {
            return _powers[group];
        }

        public int GroupOf(double value)
        {
            int group = (int)(Math.Log10(value) / LogStep);
            if (group > MaxGroup)
            {
                group = MaxGroup;
            }

            if (group < 1)
            {
                group = 1;
            }

            while (group < MaxGroup && value >= _powers[group + 1])
            {
                group++;
            }

            while (group > 1 && value < _powers[group])
            {
                group--;
            }

            return group;
        }
    }
}
