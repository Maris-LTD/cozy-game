using System;

namespace CozyGachapon.Audio
{
    public sealed class SystemAudioRandom : IAudioRandom
    {
        private const double SignedRange = 2d;
        private const double SignedOffset = 1d;

        private readonly Random _random;

        public SystemAudioRandom(int seed)
        {
            _random = new Random(seed);
        }

        public float NextSigned()
        {
            return (float)(_random.NextDouble() * SignedRange - SignedOffset);
        }

        public int NextIndex(int count)
        {
            return count < 1 ? 0 : _random.Next(count);
        }
    }
}
