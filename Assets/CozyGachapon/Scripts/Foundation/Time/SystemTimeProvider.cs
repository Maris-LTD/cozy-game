using System;
using CozyGachapon.Contracts;

namespace CozyGachapon.Foundation
{
    public sealed class SystemTimeProvider : ITimeProvider
    {
        public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
    }
}
