using System;
using CozyGachapon.Contracts;

namespace CozyGachapon.Economy
{
    public sealed class NumberFormatter : INumberFormatter
    {
        private const double GroupThreshold = 1000d;
        private const int MaxIntegerDigits = 3;
        private const int RateTrailerLength = 2;
        private const char RateSeparator = '/';
        private const char RateUnit = 's';

        private readonly int _sigDigits;
        private readonly int _rateDecimals;
        private readonly ThousandPowerTable _powers;
        private readonly NumberSuffixTable _suffixes;
        private readonly char[] _buffer;
        private readonly DisplayStringCache _amountCache;
        private readonly DisplayStringCache _rateCache;

        public NumberFormatter(EconomyConfig config)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            _sigDigits = config.DisplaySigDigits;
            _rateDecimals = Math.Max(config.RateDecimals, 0);
            _powers = new ThousandPowerTable();
            _suffixes = new NumberSuffixTable(_powers.MaxGroup);
            int fixedLength = MaxIntegerDigits + 1 + _rateDecimals;
            int mantissaLength = Math.Max(_sigDigits, MaxIntegerDigits) + 1;
            MaxLength = Math.Max(fixedLength, mantissaLength) + _suffixes.MaxSuffixLength + RateTrailerLength;
            _buffer = new char[MaxLength];
            _amountCache = new DisplayStringCache(MaxLength);
            _rateCache = new DisplayStringCache(MaxLength);
        }

        public int MaxLength { get; private set; }

        public string FormatAmount(double value)
        {
            int length = WriteAmount(value, _buffer);
            return _amountCache.Get(new ReadOnlySpan<char>(_buffer, 0, length));
        }

        public string FormatRate(double value)
        {
            int length = WriteRate(value, _buffer);
            return _rateCache.Get(new ReadOnlySpan<char>(_buffer, 0, length));
        }

        public int TryFormatAmount(double value, Span<char> destination)
        {
            int length = WriteAmount(value, _buffer);
            return CopyIfFits(length, destination);
        }

        public int TryFormatRate(double value, Span<char> destination)
        {
            int length = WriteRate(value, _buffer);
            return CopyIfFits(length, destination);
        }

        private int CopyIfFits(int length, Span<char> destination)
        {
            if (destination.Length < length)
            {
                return 0;
            }

            new ReadOnlySpan<char>(_buffer, 0, length).CopyTo(destination);
            return length;
        }

        private int WriteAmount(double value, Span<char> destination)
        {
            double safe = Sanitize(value);
            if (safe < GroupThreshold)
            {
                return SignificantDigitsWriter.WriteInteger((long)Math.Floor(safe), destination, 0);
            }

            return WriteGrouped(safe, destination);
        }

        private int WriteRate(double value, Span<char> destination)
        {
            double safe = Sanitize(value);
            int position = safe < GroupThreshold
                ? SignificantDigitsWriter.WriteTruncatedFixed(safe, _rateDecimals, destination, 0)
                : WriteGrouped(safe, destination);
            destination[position++] = RateSeparator;
            destination[position++] = RateUnit;
            return position;
        }

        private int WriteGrouped(double value, Span<char> destination)
        {
            int group = _powers.GroupOf(value);
            double mantissa = value / _powers.PowerOf(group);
            int position = SignificantDigitsWriter.WriteTruncatedMantissa(mantissa, _sigDigits, destination, 0);
            return _suffixes.Write(group, destination, position);
        }

        private static double Sanitize(double value)
        {
            if (!(value >= 0d))
            {
                return 0d;
            }

            return value > double.MaxValue ? double.MaxValue : value;
        }
    }
}
