using System.Collections.Generic;

namespace CozyGachapon.Audio
{
    public static class NoteIndexCalculator
    {
        private const int SemitonesPerOctave = 12;

        public static int Semitone(int noteIndex, IReadOnlyList<int> scaleIntervals)
        {
            int count = scaleIntervals.Count;
            int octave = noteIndex >= 0 ? noteIndex / count : -((-noteIndex + count - 1) / count);
            int degree = noteIndex - octave * count;
            return SemitonesPerOctave * octave + scaleIntervals[degree];
        }
    }
}
