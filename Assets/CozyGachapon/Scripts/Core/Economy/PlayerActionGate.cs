using CozyGachapon.Contracts;

namespace CozyGachapon.Economy
{
    internal static class PlayerActionGate
    {
        public static bool IsValid(in PlayerActionContext context)
        {
            return context.IsValid;
        }
    }
}
