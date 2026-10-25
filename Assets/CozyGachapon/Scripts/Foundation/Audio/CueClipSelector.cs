using UnityEngine;

namespace CozyGachapon.Audio
{
    public sealed class CueClipSelector
    {
        private const int NoClip = -1;
        private const int MinClipsForAvoidRepeat = 2;

        private readonly int[] _lastOrdinal;

        public CueClipSelector(int cueCount)
        {
            _lastOrdinal = new int[cueCount];
            for (int i = 0; i < cueCount; i++)
            {
                _lastOrdinal[i] = NoClip;
            }
        }

        public AudioClip Select(int cueIndex, SoundCue cue, IAudioRandom random)
        {
            int valid = CountValidClips(cue);
            if (valid == 0)
            {
                return null;
            }

            int ordinal = PickOrdinal(cueIndex, cue, random, valid);
            _lastOrdinal[cueIndex] = ordinal;
            return GetValidClip(cue, ordinal);
        }

        private int PickOrdinal(int cueIndex, SoundCue cue, IAudioRandom random, int valid)
        {
            int last = _lastOrdinal[cueIndex];
            if (!cue.AvoidRepeat || valid < MinClipsForAvoidRepeat || last < 0)
            {
                return Mathf.Clamp(random.NextIndex(valid), 0, valid - 1);
            }

            int ordinal = Mathf.Clamp(random.NextIndex(valid - 1), 0, valid - 2);
            return ordinal >= last ? ordinal + 1 : ordinal;
        }

        private static int CountValidClips(SoundCue cue)
        {
            int count = 0;
            for (int i = 0; i < cue.Clips.Count; i++)
            {
                if (cue.Clips[i] != null)
                {
                    count++;
                }
            }

            return count;
        }

        private static AudioClip GetValidClip(SoundCue cue, int ordinal)
        {
            int seen = 0;
            for (int i = 0; i < cue.Clips.Count; i++)
            {
                AudioClip clip = cue.Clips[i];
                if (clip == null)
                {
                    continue;
                }

                if (seen == ordinal)
                {
                    return clip;
                }

                seen++;
            }

            return null;
        }
    }
}
