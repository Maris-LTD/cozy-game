using System.Collections.Generic;

namespace CozyGachapon.Economy
{
    public static class EconomyBalanceTargetsValidator
    {
        private static readonly DoubleRange RealisticTapRateRange = new DoubleRange(2d, 5d);
        private static readonly DoubleRange PlinkoEvEffectiveRange = new DoubleRange(2.6d, 3.3d);
        private static readonly DoubleRange PlinkoBounceUpliftRange = new DoubleRange(0.2d, 0.55d);
        private static readonly DoubleRange[] PlinkoRawRateCeilingRanges =
        {
            new DoubleRange(18d, 24d),
            new DoubleRange(24d, 32d),
            new DoubleRange(26d, 36d),
            new DoubleRange(36d, 48d)
        };
        private static readonly DoubleRange PlinkoJackpotProbMeanRange = new DoubleRange(0.08d, 0.15d);
        private static readonly DoubleRange PlinkoScoredPegHitsRange = new DoubleRange(10d, 20d);
        private static readonly DoubleRange FirstSpinMaxSecondsRange = new DoubleRange(15d, 45d);
        private static readonly DoubleRange SpinCheckpointRange = new DoubleRange(0d, 200d);
        private static readonly DoubleRange PlinkoOnlySpinSecondsMaxRange = new DoubleRange(15d, 300d);
        private static readonly DoubleRange PipelineFrameBudgetMsRange = new DoubleRange(0.1d, 0.5d);
        private static readonly DoubleRange ValueRelToleranceRange = new DoubleRange(1e-12d, 1e-6d);
        private static readonly DoubleRange PlinkoSoftCapTestFactorRange = new DoubleRange(1.1d, 3.0d);

        public static void Validate(EconomyBalanceTargets targets, List<EconomyConfigIssue> results)
        {
            if (results == null)
            {
                return;
            }

            if (targets == null)
            {
                results.Add(EconomyConfigIssue.ForSubject(EconomyConfigIssueCode.ConfigMissing, "EconomyBalanceTargets"));
                return;
            }

            CheckRange("realisticTapRate", targets.RealisticTapRate, RealisticTapRateRange, results);
            CheckRange("plinkoEvEffectiveAssumed", targets.PlinkoEvEffectiveAssumed, PlinkoEvEffectiveRange, results);
            CheckRange("plinkoBounceUpliftAtCapAssumed", targets.PlinkoBounceUpliftAtCapAssumed, PlinkoBounceUpliftRange, results);
            CheckRawRateCeilings(targets.PlinkoRawRateCeilingAssumed, results);
            CheckRange("plinkoJackpotProbMeanAssumed", targets.PlinkoJackpotProbMeanAssumed, PlinkoJackpotProbMeanRange, results);
            CheckRange("plinkoScoredPegHitsPerCoinAssumed", targets.PlinkoScoredPegHitsPerCoinAssumed, PlinkoScoredPegHitsRange, results);
            CheckRange("firstSpinMaxSeconds", targets.FirstSpinMaxSeconds, FirstSpinMaxSecondsRange, results);
            CheckCheckpoints(targets.SpinCheckpoints, results);
            CheckSpinSecondsMax(targets.PlinkoOnlySpinSecondsMax, results);
            CheckRange("pipelineFrameBudgetMs", targets.PipelineFrameBudgetMs, PipelineFrameBudgetMsRange, results);
            CheckRange("valueRelTolerance", targets.ValueRelTolerance, ValueRelToleranceRange, results);
            CheckRange("plinkoSoftCapTestFactor", targets.PlinkoSoftCapTestFactor, PlinkoSoftCapTestFactorRange, results);
        }

        private static void CheckRawRateCeilings(IReadOnlyList<double> ceilings, List<EconomyConfigIssue> results)
        {
            if (ceilings == null || ceilings.Count != PlinkoRawRateCeilingRanges.Length)
            {
                results.Add(EconomyConfigIssue.ForKnob(
                    EconomyConfigIssueCode.KnobOutOfRange,
                    "plinkoRawRateCeilingAssumed.Count",
                    ceilings != null ? ceilings.Count : 0d,
                    new DoubleRange(PlinkoRawRateCeilingRanges.Length, PlinkoRawRateCeilingRanges.Length)));
                return;
            }

            for (int i = 0; i < PlinkoRawRateCeilingRanges.Length; i++)
            {
                CheckRange("plinkoRawRateCeilingAssumed[" + i + "]", ceilings[i], PlinkoRawRateCeilingRanges[i], results);
            }
        }

        private static void CheckCheckpoints(IReadOnlyList<int> checkpoints, List<EconomyConfigIssue> results)
        {
            if (checkpoints == null || checkpoints.Count == 0)
            {
                results.Add(EconomyConfigIssue.ForKnob(
                    EconomyConfigIssueCode.KnobOutOfRange,
                    "spinCheckpoints.Count",
                    0d,
                    new DoubleRange(1d, 10d)));
                return;
            }

            for (int i = 0; i < checkpoints.Count; i++)
            {
                CheckRange("spinCheckpoints[" + i + "]", checkpoints[i], SpinCheckpointRange, results);
            }
        }

        private static void CheckSpinSecondsMax(IReadOnlyList<double> maxSeconds, List<EconomyConfigIssue> results)
        {
            if (maxSeconds == null || maxSeconds.Count == 0)
            {
                results.Add(EconomyConfigIssue.ForKnob(
                    EconomyConfigIssueCode.KnobOutOfRange,
                    "plinkoOnlySpinSecondsMax.Count",
                    0d,
                    new DoubleRange(1d, 10d)));
                return;
            }

            for (int i = 0; i < maxSeconds.Count; i++)
            {
                CheckRange("plinkoOnlySpinSecondsMax[" + i + "]", maxSeconds[i], PlinkoOnlySpinSecondsMaxRange, results);
            }
        }

        private static void CheckRange(string name, double value, DoubleRange range, List<EconomyConfigIssue> results)
        {
            if (!range.Contains(value))
            {
                results.Add(EconomyConfigIssue.ForKnob(EconomyConfigIssueCode.KnobOutOfRange, name, value, range));
            }
        }
    }
}
