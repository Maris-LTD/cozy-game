using System.Collections.Generic;

namespace CozyGachapon.Economy
{
    public static class EconomyConfigValidator
    {
        public static void Validate(EconomyConfig config, EconomySafeRanges ranges, List<EconomyConfigIssue> results)
        {
            if (config == null)
            {
                results.Add(EconomyConfigIssue.ForSubject(EconomyConfigIssueCode.ConfigMissing, null));
                return;
            }

            if (ranges == null)
            {
                results.Add(EconomyConfigIssue.ForSubject(EconomyConfigIssueCode.SafeRangesMissing, null));
                return;
            }

            EconomyCurrencyValidator.Validate(config.Currencies, results);
            EconomyKnobRangeValidator.Validate(config, ranges, results);
            TierOwnerPrefixValidator.Validate(config.TierOwnerPrefixes, results);
        }

        public static int CountErrors(List<EconomyConfigIssue> issues)
        {
            int count = 0;
            for (int i = 0; i < issues.Count; i++)
            {
                if (issues[i].IsError)
                {
                    count++;
                }
            }

            return count;
        }
    }
}
