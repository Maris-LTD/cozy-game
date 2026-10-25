namespace CozyGachapon.Audio
{
    public sealed class VoiceSlot
    {
        public const int NoCue = -1;

        public VoicePartition Partition { get; private set; }
        public bool IsPlaying { get; private set; }
        public int CueIndex { get; private set; }
        public double StartTime { get; private set; }
        public bool Loop { get; private set; }
        public bool IsPaused { get; private set; }

        public VoiceSlot(VoicePartition partition)
        {
            Partition = partition;
            CueIndex = NoCue;
        }

        public void Assign(int cueIndex, double startTime, bool loop)
        {
            IsPlaying = true;
            IsPaused = false;
            CueIndex = cueIndex;
            StartTime = startTime;
            Loop = loop;
        }

        public void SetPaused(bool paused)
        {
            IsPaused = paused;
        }

        public void Free()
        {
            IsPlaying = false;
            IsPaused = false;
            CueIndex = NoCue;
            StartTime = 0d;
            Loop = false;
        }
    }
}
