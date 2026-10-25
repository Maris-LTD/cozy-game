using System.Collections.Generic;
using UnityEngine;

namespace CozyGachapon.Audio
{
    public static class AudioBudgetMath
    {
        public static int StormVoiceCapacity(AudioConfig config)
        {
            return config.TotalVoices - config.MusicVoices - config.AmbienceVoices - config.MinFreeVoicesForRewards;
        }

        public static bool IsStormCue(AudioConfig config, SoundCue cue)
        {
            return cue != null && cue.Priority <= config.StormPriorityCeiling;
        }

        public static int StormInstances(AudioConfig config, IReadOnlyList<SoundCue> cues)
        {
            int total = 0;
            for (int i = 0; i < cues.Count; i++)
            {
                if (IsStormCue(config, cues[i]))
                {
                    total += cues[i].MaxInstances;
                }
            }

            return total;
        }

        public static double StormPeakGain(AudioConfig config, IReadOnlyList<SoundCue> cues)
        {
            double total = 0d;
            for (int i = 0; i < cues.Count; i++)
            {
                SoundCue cue = cues[i];
                if (!IsStormCue(config, cue) || !AudioBusPath.TryGetPathDb(config, cue.Bus, out float pathDb))
                {
                    continue;
                }

                total += cue.MaxInstances * AudioMath.DbToLinear(cue.VolumeDb + pathDb, config.SilenceFloorDb);
            }

            return total;
        }

        public static double StealAge(SoundCue cue)
        {
            return (double)cue.MaxInstances * cue.MinInterval;
        }

        public static double SpawnPlaySeconds(SoundCue cue, out string longestClipName, out float longestLength)
        {
            longestClipName = null;
            longestLength = 0f;
            IReadOnlyList<AudioClip> clips = cue.Clips;
            for (int i = 0; i < clips.Count; i++)
            {
                if (clips[i] != null && clips[i].length > longestLength)
                {
                    longestLength = clips[i].length;
                    longestClipName = clips[i].name;
                }
            }

            double slowest = 1d - cue.PitchJitterRatio;
            return slowest > 0d ? longestLength / slowest : double.PositiveInfinity;
        }
    }
}
