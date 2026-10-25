using System;

namespace CozyGachapon.Contracts
{
    [Flags]
    public enum MuteReason
    {
        None = 0,
        User = 1,
        Ad = 2,
        Background = 4
    }
}
