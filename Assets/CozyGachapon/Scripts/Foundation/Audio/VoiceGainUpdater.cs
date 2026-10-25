namespace CozyGachapon.Audio
{
    public sealed class VoiceGainUpdater
    {
        private readonly VoicePool _pool;
        private readonly CueRuntimeTable _cues;
        private readonly IVoiceBackend _backend;
        private readonly VoiceGainCalculator _calculator;

        public VoiceGainUpdater(
            VoicePool pool,
            CueRuntimeTable cues,
            IVoiceBackend backend,
            VoiceGainCalculator calculator)
        {
            _pool = pool;
            _cues = cues;
            _backend = backend;
            _calculator = calculator;
        }

        public void RefreshPlaying(double now)
        {
            for (int i = 0; i < _pool.Count; i++)
            {
                VoiceSlot slot = _pool.GetSlot(i);
                if (slot.IsPlaying)
                {
                    _backend.SetVolume(i, _calculator.Compute(_cues.GetCue(slot.CueIndex), now));
                }
            }
        }
    }
}
