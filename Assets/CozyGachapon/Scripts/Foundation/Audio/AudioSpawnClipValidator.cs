using System.Collections.Generic;
using CozyGachapon.Contracts;

namespace CozyGachapon.Audio
{
    public static class AudioSpawnClipValidator
    {
        private const string MarginSubject = "spawnClipMarginSeconds";
        private const string ManualMaxSubject = "manualSpawnClipMaxSeconds";

        public static void Validate(
            AudioBalanceTargets targets,
            IReadOnlyList<SoundCue> cues,
            float tapCooldownSeconds,
            List<AudioConfigIssue> results)
        {
            ValidateMargin(targets, results);
            ValidateCooldownBound(targets, tapCooldownSeconds, results);

            IReadOnlyList<string> spawnIds = targets.SpawnCueIds;
            for (int i = 0; i < spawnIds.Count; i++)
            {
                ValidateSpawnCue(targets, cues, spawnIds[i], results);
            }
        }

        private static void ValidateMargin(AudioBalanceTargets targets, List<AudioConfigIssue> results)
        {
            float margin = targets.SpawnClipMarginSeconds;
            if (margin < targets.SpawnClipMarginMinSeconds)
            {
                results.Add(AudioConfigIssue.ForKnob(AudioConfigIssueCode.SpawnClipMarginOutOfRange, MarginSubject, margin, targets.SpawnClipMarginMinSeconds));
            }
            else if (margin > targets.SpawnClipMarginMaxSeconds)
            {
                results.Add(AudioConfigIssue.ForKnob(AudioConfigIssueCode.SpawnClipMarginOutOfRange, MarginSubject, margin, targets.SpawnClipMarginMaxSeconds));
            }
        }

        private static void ValidateCooldownBound(AudioBalanceTargets targets, float tapCooldownSeconds, List<AudioConfigIssue> results)
        {
            double manualMax = targets.ManualSpawnClipMaxSeconds;
            double margin = targets.SpawnClipMarginSeconds;
            if (manualMax + margin > tapCooldownSeconds + targets.BoundaryTolerance)
            {
                float limit = (float)(tapCooldownSeconds - margin);
                results.Add(AudioConfigIssue.ForKnob(AudioConfigIssueCode.ManualSpawnClipExceedsCooldownMargin, ManualMaxSubject, (float)manualMax, limit));
            }
        }

        private static void ValidateSpawnCue(
            AudioBalanceTargets targets,
            IReadOnlyList<SoundCue> cues,
            string cueId,
            List<AudioConfigIssue> results)
        {
            SoundCue cue = Find(cues, cueId);
            if (cue == null)
            {
                results.Add(AudioConfigIssue.ForCue(AudioConfigIssueCode.SpawnCueMissing, cueId));
                return;
            }

            if (cue.InstanceCapPolicy != InstanceCapPolicy.DropNew)
            {
                results.Add(AudioConfigIssue.ForCue(AudioConfigIssueCode.SpawnCueNotDropNew, cueId));
            }

            double playSeconds = AudioBudgetMath.SpawnPlaySeconds(cue, out string clipName, out float length);
            if (playSeconds > targets.ManualSpawnClipMaxSeconds + targets.BoundaryTolerance)
            {
                results.Add(new AudioConfigIssue(
                    AudioConfigIssueCode.SpawnClipTooLong,
                    cueId,
                    clipName,
                    -1,
                    length,
                    (float)playSeconds,
                    targets.ManualSpawnClipMaxSeconds,
                    true));
            }
        }

        private static SoundCue Find(IReadOnlyList<SoundCue> cues, string cueId)
        {
            for (int i = 0; i < cues.Count; i++)
            {
                if (cues[i] != null && cues[i].CueId == cueId)
                {
                    return cues[i];
                }
            }

            return null;
        }
    }
}
