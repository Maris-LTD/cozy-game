namespace CozyGachapon.Audio
{
    public sealed class AudioUnlockController
    {
        private readonly AudioRuntimeState _state;
        private readonly AudioPlatformPolicy _policy;
        private bool _applied;

        public bool IsUnlocked => _state.Lifecycle == AudioLifecycle.Active && !_state.IsLocked;

        public AudioUnlockController(AudioRuntimeState state, AudioPlatformPolicy policy)
        {
            _state = state;
            _policy = policy;
        }

        public void ApplyOnInitialized()
        {
            if (_applied)
            {
                return;
            }

            _applied = true;
            if (_policy.RequiresUnlock && _state.Lifecycle == AudioLifecycle.Active && !_state.HasFirstInteraction)
            {
                _state.SetLocked(true);
            }
        }

        public bool HandleFirstInteraction()
        {
            if (_state.HasFirstInteraction)
            {
                return false;
            }

            _state.MarkFirstInteraction();
            if (_state.IsLocked)
            {
                _state.SetLocked(false);
            }

            return true;
        }
    }
}
