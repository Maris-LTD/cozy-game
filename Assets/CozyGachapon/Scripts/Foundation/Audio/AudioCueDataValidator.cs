using System.Collections.Generic;
using CozyGachapon.Contracts;
using UnityEngine;

namespace CozyGachapon.Audio
{
    public static class AudioCueDataValidator
    {
        public static void Validate(
            AudioConfig config,
            AudioBalanceTargets targets,
            IReadOnlyList<SoundCue> cues,
            List<AudioConfigIssue> results)
        {
            for (int i = 0; i < cues.Count; i++)
            {
                SoundCue cue = cues[i];
                if (cue == null)
                {
                    results.Add(AudioConfigIssue.ForCue(AudioConfigIssueCode.CueMissing, null));
                    continue;
                }

                ValidateCue(config, targets, cue, results);
                ValidateDuplicate(cues, i, results);
            }
        }

        private static void ValidateCue(AudioConfig config, AudioBalanceTargets targets, SoundCue cue, List<AudioConfigIssue> results)
        {
            if (string.IsNullOrEmpty(cue.CueId))
            {
                results.Add(AudioConfigIssue.ForCue(AudioConfigIssueCode.CueIdMissing, null));
            }

            if (!AudioBusPath.TryGetPathLength(cue.Bus, out _))
            {
                results.Add(AudioConfigIssue.ForCue(AudioConfigIssueCode.InvalidBus, cue.CueId));
            }

            if (cue.MaxInstances < 1)
            {
                results.Add(AudioConfigIssue.ForCueValue(AudioConfigIssueCode.MaxInstancesInvalid, cue.CueId, cue.MaxInstances, 1f));
            }

            if (cue.MinInterval < 0f)
            {
                results.Add(AudioConfigIssue.ForCueValue(AudioConfigIssueCode.MinIntervalNegative, cue.CueId, cue.MinInterval, 0f));
            }

            if (cue.Priority < targets.CuePriorityMin || cue.Priority > targets.CuePriorityMax)
            {
                float limit = cue.Priority < targets.CuePriorityMin ? targets.CuePriorityMin : targets.CuePriorityMax;
                results.Add(AudioConfigIssue.ForCueValue(AudioConfigIssueCode.PriorityOutOfRange, cue.CueId, cue.Priority, limit));
            }

            if (cue.VolumeDb > 0f)
            {
                results.Add(new AudioConfigIssue(AudioConfigIssueCode.VolumeAboveZero, cue.CueId, null, -1, cue.VolumeDb, 0f, 0f, false));
            }

            if (cue.PitchMode == PitchMode.Scale)
            {
                ValidateScaleSamples(config, cue, results);
            }
            else if (!HasAnyClip(cue.Clips))
            {
                results.Add(AudioConfigIssue.ForCue(AudioConfigIssueCode.NoClip, cue.CueId));
            }
        }

        private static void ValidateDuplicate(IReadOnlyList<SoundCue> cues, int index, List<AudioConfigIssue> results)
        {
            string cueId = cues[index].CueId;
            if (string.IsNullOrEmpty(cueId))
            {
                return;
            }

            int earlier = 0;
            for (int i = 0; i < index; i++)
            {
                if (cues[i] != null && cues[i].CueId == cueId)
                {
                    earlier++;
                }
            }

            if (earlier == 1)
            {
                results.Add(AudioConfigIssue.ForCue(AudioConfigIssueCode.DuplicateCueId, cueId));
            }
        }

        private static void ValidateScaleSamples(AudioConfig config, SoundCue cue, List<AudioConfigIssue> results)
        {
            IReadOnlyList<PitchedSample> samples = cue.PitchedSamples;
            if (!HasAnyPitchedClip(samples))
            {
                results.Add(AudioConfigIssue.ForCue(AudioConfigIssueCode.ScaleSamplesMissing, cue.CueId));
                return;
            }

            IReadOnlyList<int> intervals = config.ScaleIntervals;
            if (intervals.Count == 0)
            {
                return;
            }

            for (int note = 0; note < config.ScaleNoteCount; note++)
            {
                int semitone = NoteIndexCalculator.Semitone(note, intervals);
                SamplePitchSelector.TrySelectSample(semitone, samples, out _, out int shift);
                if (Mathf.Abs(shift) > config.MaxSampleShiftSemitones)
                {
                    results.Add(new AudioConfigIssue(AudioConfigIssueCode.PitchShiftExceeded, cue.CueId, null, note, shift, 0f, config.MaxSampleShiftSemitones, true));
                    return;
                }
            }
        }

        private static bool HasAnyClip(IReadOnlyList<AudioClip> clips)
        {
            for (int i = 0; i < clips.Count; i++)
            {
                if (clips[i] != null)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool HasAnyPitchedClip(IReadOnlyList<PitchedSample> samples)
        {
            for (int i = 0; i < samples.Count; i++)
            {
                if (samples[i] != null && samples[i].Clip != null)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
