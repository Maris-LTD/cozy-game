using System;

namespace CozyGachapon.Contracts
{
    public interface ITimeProvider
    {
        DateTimeOffset UtcNow { get; }
    }
}
