using CozyGachapon.Contracts;

namespace CozyGachapon.Audio
{
    public sealed class AdMuteHandler
    {
        private readonly AudioRuntimeState _state;
        private readonly VoicePool _pool;
        private readonly IVoiceBackend _backend;
        private readonly CueRuntimeTable _cues;

        public AdMuteHandler(AudioRuntimeState state, VoicePool pool, IVoiceBackend backend, CueRuntimeTable cues)
        {
            _state = state;
            _pool = pool;
            _backend = backend;
            _cues = cues;
        }

        public void Apply(MuteReason reason, bool active)
        {
            bool adRaised = active
                && (reason & MuteReason.Ad) != MuteReason.None
                && (_state.SuspendReasons & MuteReason.Ad) == MuteReason.None;
            if (adRaised)
            {
                _pool.StopAllSfx(_backend, _cues);
            }
        }
    }
}
