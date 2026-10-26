namespace CozyGachapon.Economy
{
    public static class IncomeInputGuard
    {
        public static bool IsValidRawValue(double rawValue)
        {
            return !double.IsNaN(rawValue) && !double.IsInfinity(rawValue) && rawValue >= 0d;
        }

        public static bool IsValidSubmitAmount(double amount)
        {
            return !double.IsNaN(amount) && !double.IsInfinity(amount) && amount > 0d;
        }
    }
}
