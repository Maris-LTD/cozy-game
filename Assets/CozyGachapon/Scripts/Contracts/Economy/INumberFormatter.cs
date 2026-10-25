using System;

namespace CozyGachapon.Contracts
{
    public interface INumberFormatter
    {
        int MaxLength { get; }

        string FormatAmount(double value);
        string FormatRate(double value);
        int TryFormatAmount(double value, Span<char> destination);
        int TryFormatRate(double value, Span<char> destination);
    }
}
