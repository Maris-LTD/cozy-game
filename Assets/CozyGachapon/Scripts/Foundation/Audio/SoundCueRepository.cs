using System;
using System.Collections.Generic;
using CozyGachapon.Contracts;

namespace CozyGachapon.Audio
{
    public class SoundCueRepository
    {
        private readonly List<SoundCue> _cues = new List<SoundCue>();
        private readonly Dictionary<string, int> _indexById = new Dictionary<string, int>(StringComparer.Ordinal);

        public int Count => _cues.Count;
        public int UnusableCount { get; private set; }

        public SoundCueRepository(IReadOnlyList<SoundCue> source)
        {
            if (source == null)
            {
                return;
            }

            Dictionary<string, int> occurrences = CountOccurrences(source);
            for (int i = 0; i < source.Count; i++)
            {
                SoundCue cue = source[i];
                if (IsUsable(cue, occurrences))
                {
                    _indexById.Add(cue.CueId, _cues.Count);
                    _cues.Add(cue);
                }
                else
                {
                    UnusableCount++;
                }
            }
        }

        public bool TryGetIndex(string cueId, out int index)
        {
            if (cueId == null)
            {
                index = -1;
                return false;
            }

            return _indexById.TryGetValue(cueId, out index);
        }

        public SoundCue GetCue(int index)
        {
            return _cues[index];
        }

        private static Dictionary<string, int> CountOccurrences(IReadOnlyList<SoundCue> source)
        {
            Dictionary<string, int> occurrences = new Dictionary<string, int>(StringComparer.Ordinal);
            for (int i = 0; i < source.Count; i++)
            {
                SoundCue cue = source[i];
                if (cue == null || string.IsNullOrWhiteSpace(cue.CueId))
                {
                    continue;
                }

                occurrences.TryGetValue(cue.CueId, out int count);
                occurrences[cue.CueId] = count + 1;
            }

            return occurrences;
        }

        private static bool IsUsable(SoundCue cue, Dictionary<string, int> occurrences)
        {
            if (cue == null || string.IsNullOrWhiteSpace(cue.CueId))
            {
                return false;
            }

            return occurrences[cue.CueId] == 1 && HasPlayableClip(cue);
        }

        private static bool HasPlayableClip(SoundCue cue)
        {
            if (cue.PitchMode == PitchMode.Scale)
            {
                for (int i = 0; i < cue.PitchedSamples.Count; i++)
                {
                    PitchedSample sample = cue.PitchedSamples[i];
                    if (sample != null && sample.Clip != null)
                    {
                        return true;
                    }
                }

                return false;
            }

            for (int i = 0; i < cue.Clips.Count; i++)
            {
                if (cue.Clips[i] != null)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
