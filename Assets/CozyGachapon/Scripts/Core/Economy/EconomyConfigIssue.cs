namespace CozyGachapon.Economy
{
    public readonly struct EconomyConfigIssue
    {
        public EconomyConfigIssueCode Code { get; }
        public string Subject { get; }
        public double Value { get; }
        public double Min { get; }
        public double Max { get; }
        public bool IsError { get; }

        public EconomyConfigIssue(EconomyConfigIssueCode code, string subject, double value, double min, double max, bool isError)
        {
            Code = code;
            Subject = subject;
            Value = value;
            Min = min;
            Max = max;
            IsError = isError;
        }

        public static EconomyConfigIssue ForSubject(EconomyConfigIssueCode code, string subject)
        {
            return new EconomyConfigIssue(code, subject, 0d, 0d, 0d, true);
        }

        public static EconomyConfigIssue ForIndex(EconomyConfigIssueCode code, int index)
        {
            return new EconomyConfigIssue(code, null, index, 0d, 0d, true);
        }

        public static EconomyConfigIssue ForKnob(EconomyConfigIssueCode code, string subject, double value, DoubleRange range)
        {
            return new EconomyConfigIssue(code, subject, value, range.Min, range.Max, true);
        }
    }
}
