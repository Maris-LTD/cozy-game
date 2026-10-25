using System.Collections.Generic;
using UnityEngine;

namespace CozyGachapon.Audio
{
    public static class NoteIndexCalculator
    {
        private const int SemitonesPerOctave = 12;
        private const float RoundingOffset = 0.5f;

        public static int Semitone(int noteIndex, IReadOnlyList<int> scaleIntervals)
        {
            int count = scaleIntervals.Count;
            int octave = noteIndex >= 0 ? noteIndex / count : -((-noteIndex + count - 1) / count);
            int degree = noteIndex - octave * count;
            return SemitonesPerOctave * octave + scaleIntervals[degree];
        }

        public static int NoteIndex(float pitchPosition, float fallbackPitchPosition, int scaleNoteCount)
        {
            if (scaleNoteCount < 1)
            {
                return 0;
            }

            float position = ResolvePosition(pitchPosition, fallbackPitchPosition);
            int index = Mathf.FloorToInt(position * (scaleNoteCount - 1) + RoundingOffset);
            return Mathf.Clamp(index, 0, scaleNoteCount - 1);
        }

        private static float ResolvePosition(float pitchPosition, float fallbackPitchPosition)
        {
            float source = AudioMath.IsFinite(pitchPosition) ? pitchPosition : fallbackPitchPosition;
            return AudioMath.IsFinite(source) ? Mathf.Clamp01(source) : 0f;
        }
    }
}
