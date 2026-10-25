using System.Collections.Generic;

namespace CozyGachapon.Audio
{
    public static class AudioConfigValidator
    {
        public static void Validate(
            AudioConfig config,
            AudioBalanceTargets targets,
            IReadOnlyList<SoundCue> cues,
            float tapCooldownSeconds,
            List<AudioConfigIssue> results)
        {
            AudioCueDataValidator.Validate(config, targets, cues, results);
            AudioBudgetValidator.Validate(config, targets, cues, results);
            AudioSpawnClipValidator.Validate(targets, cues, tapCooldownSeconds, results);
        }

        public static int CountErrors(List<AudioConfigIssue> issues)
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
