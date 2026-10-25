using CozyGachapon.Audio;

namespace CozyGachapon.Tests.Audio.BoundaryFakes
{
    public sealed class ReferencesAudioServiceFake
    {
        public AudioService Create()
        {
            return null;
        }
    }

    public sealed class ReferencesAudioArrayFake
    {
        public AudioBootModule[] Modules { get; set; }
    }

    public sealed class CleanFake
    {
        public int Value { get; set; }
    }
}
