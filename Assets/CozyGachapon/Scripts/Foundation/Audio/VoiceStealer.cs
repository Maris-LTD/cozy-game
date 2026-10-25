namespace CozyGachapon.Audio
{
    public sealed class VoiceStealer
    {
        private readonly VoicePool _pool;
        private readonly CueRuntimeTable _cues;

        public VoiceStealer(VoicePool pool, CueRuntimeTable cues)
        {
            _pool = pool;
            _cues = cues;
        }

        public bool TryFindVictim(int requestPriority, out int voice)
        {
            voice = -1;
            int victimPriority = 0;
            double victimStart = 0d;
            for (int i = 0; i < _pool.Count; i++)
            {
                VoiceSlot slot = _pool.GetSlot(i);
                if (slot.Partition != VoicePartition.Sfx || !slot.IsPlaying)
                {
                    continue;
                }

                int priority = _cues.GetPriority(slot.CueIndex);
                if (priority >= requestPriority)
                {
                    continue;
                }

                if (voice < 0 || IsBetterVictim(priority, slot.StartTime, victimPriority, victimStart))
                {
                    voice = i;
                    victimPriority = priority;
                    victimStart = slot.StartTime;
                }
            }

            return voice >= 0;
        }

        private static bool IsBetterVictim(int priority, double startTime, int bestPriority, double bestStart)
        {
            return priority < bestPriority || (priority == bestPriority && startTime < bestStart);
        }
    }
}
