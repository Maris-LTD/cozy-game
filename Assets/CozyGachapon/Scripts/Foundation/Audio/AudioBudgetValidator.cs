using System.Collections.Generic;
using CozyGachapon.Contracts;

namespace CozyGachapon.Audio
{
    public static class AudioBudgetValidator
    {
        private const string StormInstancesSubject = "storm_instances";
        private const string StormPeakGainSubject = "storm_peak_gain";

        public static void Validate(
            AudioConfig config,
            AudioBalanceTargets targets,
            IReadOnlyList<SoundCue> cues,
            List<AudioConfigIssue> results)
        {
            ValidateVoiceBudget(config, cues, results);
            ValidatePeakGain(config, targets, cues, results);
            ValidateStealAge(config, targets, cues, results);
        }

        private static void ValidateVoiceBudget(AudioConfig config, IReadOnlyList<SoundCue> cues, List<AudioConfigIssue> results)
        {
            int instances = AudioBudgetMath.StormInstances(config, cues);
            int capacity = AudioBudgetMath.StormVoiceCapacity(config);
            if (instances > capacity)
            {
                results.Add(AudioConfigIssue.ForKnob(AudioConfigIssueCode.VoiceBudgetExceeded, StormInstancesSubject, instances, capacity));
            }
        }

        private static void ValidatePeakGain(
            AudioConfig config,
            AudioBalanceTargets targets,
            IReadOnlyList<SoundCue> cues,
            List<AudioConfigIssue> results)
        {
            double gain = AudioBudgetMath.StormPeakGain(config, cues);
            if (gain > targets.StormPeakGainMax + targets.BoundaryTolerance)
            {
                results.Add(AudioConfigIssue.ForKnob(AudioConfigIssueCode.StormPeakGainExceeded, StormPeakGainSubject, (float)gain, targets.StormPeakGainMax));
            }
        }

        private static void ValidateStealAge(
            AudioConfig config,
            AudioBalanceTargets targets,
            IReadOnlyList<SoundCue> cues,
            List<AudioConfigIssue> results)
        {
            for (int i = 0; i < cues.Count; i++)
            {
                SoundCue cue = cues[i];
                if (!AudioBudgetMath.IsStormCue(config, cue) || cue.InstanceCapPolicy != InstanceCapPolicy.StealOldest)
                {
                    continue;
                }

                double stealAge = AudioBudgetMath.StealAge(cue);
                if (stealAge < targets.MinStealAgeSeconds - targets.BoundaryTolerance)
                {
                    results.Add(AudioConfigIssue.ForCueValue(AudioConfigIssueCode.StealAgeTooShort, cue.CueId, (float)stealAge, targets.MinStealAgeSeconds));
                }
            }
        }
    }
}
