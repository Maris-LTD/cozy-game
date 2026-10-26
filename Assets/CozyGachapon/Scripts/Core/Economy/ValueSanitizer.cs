namespace CozyGachapon.Economy
{
    public static class ValueSanitizer
    {
        public static double Sanitize(double value)
        {
            if (value >= 0d && !double.IsInfinity(value))
            {
                return value;
            }

            return 0d;
        }
    }
}
