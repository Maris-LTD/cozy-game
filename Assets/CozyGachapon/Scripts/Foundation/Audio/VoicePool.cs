namespace CozyGachapon.Audio
{
    public sealed class VoicePool
    {
        private readonly VoiceSlot[] _slots;

        public int Count => _slots.Length;
        public int ActiveCount { get; private set; }

        public VoicePool(int totalVoices, int musicVoices, int ambienceVoices)
        {
            _slots = new VoiceSlot[totalVoices];
            for (int i = 0; i < totalVoices; i++)
            {
                _slots[i] = new VoiceSlot(GetPartitionForSlot(i, musicVoices, ambienceVoices));
            }
        }

        public VoiceSlot GetSlot(int voice)
        {
            return _slots[voice];
        }

        public bool TryAcquireFree(VoicePartition partition, out int voice)
        {
            for (int i = 0; i < _slots.Length; i++)
            {
                VoiceSlot slot = _slots[i];
                if (slot.Partition == partition && !slot.IsPlaying)
                {
                    voice = i;
                    return true;
                }
            }

            voice = -1;
            return false;
        }

        public void Assign(int voice, int cueIndex, double startTime, bool loop)
        {
            VoiceSlot slot = _slots[voice];
            if (!slot.IsPlaying)
            {
                ActiveCount++;
            }

            slot.Assign(cueIndex, startTime, loop);
        }

        public void Release(int voice)
        {
            VoiceSlot slot = _slots[voice];
            if (!slot.IsPlaying)
            {
                return;
            }

            slot.Free();
            ActiveCount--;
        }

        public void Evict(int voice, IVoiceBackend backend, CueRuntimeTable cues)
        {
            if (!_slots[voice].IsPlaying)
            {
                return;
            }

            backend.Stop(voice);
            ReleaseInstance(voice, cues);
        }

        public void Tick(IVoiceBackend backend, CueRuntimeTable cues)
        {
            for (int i = 0; i < _slots.Length; i++)
            {
                VoiceSlot slot = _slots[i];
                if (!slot.IsPlaying || slot.Loop || backend.IsPlaying(i))
                {
                    continue;
                }

                ReleaseInstance(i, cues);
            }
        }

        private void ReleaseInstance(int voice, CueRuntimeTable cues)
        {
            cues.RemoveInstance(_slots[voice].CueIndex);
            Release(voice);
        }

        private static VoicePartition GetPartitionForSlot(int slotIndex, int musicVoices, int ambienceVoices)
        {
            if (slotIndex < musicVoices)
            {
                return VoicePartition.Music;
            }

            if (slotIndex < musicVoices + ambienceVoices)
            {
                return VoicePartition.Ambience;
            }

            return VoicePartition.Sfx;
        }
    }
}
