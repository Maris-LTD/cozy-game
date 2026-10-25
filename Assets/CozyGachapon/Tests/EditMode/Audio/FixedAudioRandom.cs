using CozyGachapon.Audio;

namespace CozyGachapon.Tests.Audio
{
    public sealed class FixedAudioRandom : IAudioRandom
    {
        private readonly float _signed;
        private readonly int _index;

        public int SignedCalls { get; private set; }
        public int IndexCalls { get; private set; }

        public FixedAudioRandom(float signed = 0f, int index = 0)
        {
            _signed = signed;
            _index = index;
        }

        public float NextSigned()
        {
            SignedCalls++;
            return _signed;
        }

        public int NextIndex(int count)
        {
            IndexCalls++;
            return _index < count ? _index : count - 1;
        }
    }
}
