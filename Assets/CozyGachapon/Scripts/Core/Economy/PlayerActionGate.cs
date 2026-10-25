using CozyGachapon.Contracts;

namespace CozyGachapon.Economy
{
    public static class PlayerActionGate
    {
        public static bool IsValid(in PlayerActionContext context)
        {
            return context.IsValid;
        }
    }
}
