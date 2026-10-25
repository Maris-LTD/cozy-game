using CozyGachapon.Contracts;

namespace CozyGachapon.Audio
{
    public sealed class AudioFailedSettings
    {
        private const bool FallbackUserMuted = false;

        private readonly AudioRuntimeState _state;
        private readonly IAudioSettingsStore _store;

        public AudioFailedSettings(AudioRuntimeState state, IAudioSettingsStore store)
        {
            _state = state;
            _store = store;
        }

        public void Restore()
        {
            bool muted = FallbackUserMuted;
            if (_store != null && _store.TryReadUserMuted(out bool stored))
            {
                muted = stored;
            }

            _state.SetUserMuted(muted);
        }

        public bool Apply(MuteReason reason, bool active)
        {
            if ((reason & MuteReason.User) == MuteReason.None || !_state.SetUserMuted(active))
            {
                return false;
            }

            if (_store != null)
            {
                _store.WriteUserMuted(active);
            }

            return true;
        }
    }
}
