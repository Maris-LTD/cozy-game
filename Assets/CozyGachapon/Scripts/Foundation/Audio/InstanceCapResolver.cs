namespace CozyGachapon.Audio
{
    public sealed class InstanceCapResolver
    {
        private readonly VoicePool _pool;

        public InstanceCapResolver(VoicePool pool)
        {
            _pool = pool;
        }

        public bool TryFindOldestInstance(int cueIndex, out int voice)
        {
            voice = -1;
            double oldestStart = 0d;
            for (int i = 0; i < _pool.Count; i++)
            {
                VoiceSlot slot = _pool.GetSlot(i);
                if (!slot.IsPlaying || slot.CueIndex != cueIndex)
                {
                    continue;
                }

                if (voice < 0 || slot.StartTime < oldestStart)
                {
                    voice = i;
                    oldestStart = slot.StartTime;
                }
            }

            return voice >= 0;
        }
    }
}
