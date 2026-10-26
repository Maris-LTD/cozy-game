namespace CozyGachapon.Economy
{
    public sealed class PipelineDirtyTracker
    {
        public bool IsDirty { get; private set; }
        public int IdleRecomputeCount { get; private set; }

        public PipelineDirtyTracker()
        {
            IsDirty = true;
        }

        public void MarkDirty()
        {
            IsDirty = true;
        }

        public void OnRecomputed()
        {
            if (IsDirty)
            {
                IdleRecomputeCount++;
                IsDirty = false;
            }
        }
    }
}
