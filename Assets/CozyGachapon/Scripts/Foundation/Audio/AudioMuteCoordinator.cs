using CozyGachapon.Contracts;

namespace CozyGachapon.Audio
{
    public sealed class AudioMuteCoordinator
    {
        private readonly AudioSuspendController _suspend;
        private readonly UserMuteController _userMute;
        private readonly VoicePool _pool;
        private readonly IVoiceBackend _backend;
        private readonly VoiceGainUpdater _gainUpdater;
        private readonly AudioUserSettings _settings;

        public AudioMuteCoordinator(
            AudioSuspendController suspend,
            UserMuteController userMute,
            VoicePool pool,
            IVoiceBackend backend,
            VoiceGainUpdater gainUpdater,
            AudioUserSettings settings)
        {
            _suspend = suspend;
            _userMute = userMute;
            _pool = pool;
            _backend = backend;
            _gainUpdater = gainUpdater;
            _settings = settings;
        }

        public void Restore(bool userMuted)
        {
            _userMute.Restore(userMuted);
        }

        public bool Apply(MuteReason reason, bool active, double now)
        {
            bool userChanged = (reason & MuteReason.User) != MuteReason.None && _userMute.Set(active, now);
            if (userChanged)
            {
                _settings.SaveUserMuted(active);
            }

            SuspendTransition transition = _suspend.Apply(reason, active, now);
            if (transition == SuspendTransition.Suspended)
            {
                _pool.PauseAll(_backend);
                _gainUpdater.RefreshPlaying(now);
            }
            else if (transition == SuspendTransition.Resumed)
            {
                _gainUpdater.RefreshPlaying(now);
                _pool.ResumeAll(_backend);
            }

            return userChanged;
        }

        public bool Tick(double now, out bool soundOnDue)
        {
            bool suspendRefresh = _suspend.ConsumeRefresh(now);
            bool userRefresh = _userMute.ConsumeRefresh(now);
            soundOnDue = !_suspend.IsSuspended && _userMute.ConsumeSoundOnDue(now);
            return suspendRefresh || userRefresh;
        }
    }
}
