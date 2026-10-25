using CozyGachapon.Contracts;

namespace CozyGachapon.Audio
{
    public sealed class MusicStarter
    {
        private const string MusicCueId = "Music.ShopLoop";
        private const int NoCueIndex = -1;

        private readonly AudioRuntimeState _state;
        private readonly AudioPlayPipeline _pipeline;
        private readonly MusicFadeIn _fade;
        private readonly int _musicIndex;
        private bool _attempted;

        public MusicStarter(AudioRuntimeState state, CueRuntimeTable cues, AudioPlayPipeline pipeline, MusicFadeIn fade)
        {
            _state = state;
            _pipeline = pipeline;
            _fade = fade;
            _musicIndex = cues.TryResolve(MusicCueId, out int index) ? index : NoCueIndex;
        }

        public bool Tick(double now)
        {
            if (!_attempted && CanStart())
            {
                _attempted = true;
                _fade.Begin(now);
                PlayResult result = _pipeline.Admit(_musicIndex, now, AudioPlayParams.None);
                if (!result.Played)
                {
                    _fade.Cancel();
                }
            }

            return _fade.ConsumeRefresh(now);
        }

        private bool CanStart()
        {
            return _musicIndex != NoCueIndex && _state.HasInteractive && !_state.IsLocked && !_state.IsSuspended;
        }
    }
}
