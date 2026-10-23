using CozyGachapon.Contracts;

namespace CozyGachapon.Foundation
{
    public sealed class InputBarrier : InputBlockGate
    {
        public int AcquireToken(string owner = null)
        {
            return AcquireInputBlock(owner);
        }
    }
}
