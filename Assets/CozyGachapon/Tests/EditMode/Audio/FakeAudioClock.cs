using CozyGachapon.Contracts;

namespace CozyGachapon.Tests.Audio
{
    public sealed class FakeAudioClock : IAudioClock
    {
        public double Now { get; private set; }

        public void Set(double now)
        {
            Now = now;
        }

        public void Advance(double seconds)
        {
            Now += seconds;
        }
    }
}
