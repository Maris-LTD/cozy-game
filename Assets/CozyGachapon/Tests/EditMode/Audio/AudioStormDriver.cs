using CozyGachapon.Audio;
using CozyGachapon.Contracts;
using NUnit.Framework;

namespace CozyGachapon.Tests.Audio
{
    public enum StormCue
    {
        PegHitScored,
        PegHitUnscored,
        CoinSpawn,
        JackpotHit
    }

    public sealed class AudioStormDriver
    {
        public const string PegHitScoredId = "Plinko.PegHitScored";
        public const string PegHitUnscoredId = "Plinko.PegHitUnscored";
        public const string CoinSpawnId = "Plinko.CoinSpawn";
        public const string JackpotHitId = "Plinko.JackpotHit";

        private const int UnscoredEveryNthPegHit = 10;
        private const double JackpotIntervalSeconds = 2d;
        private const double FinishEpsilon = 1e-9;

        private static readonly int CueKindCount = System.Enum.GetValues(typeof(StormCue)).Length;

        private readonly AudioService _service;
        private readonly FakeAudioClock _clock;
        private readonly FakeVoiceBackend _backend;
        private readonly AudioBalanceTargets _targets;
        private readonly double _coinPeriodSeconds;
        private readonly int[] _cueIndices;
        private readonly int[] _requests;
        private readonly int[] _played;
        private readonly double[] _voiceStart;
        private readonly bool[] _voiceActive;

        public int Steps { get; private set; }
        public int MaxPlayingVoices { get; private set; }

        public AudioStormDriver(
            AudioService service,
            FakeAudioClock clock,
            FakeVoiceBackend backend,
            AudioBalanceTargets targets,
            double coinPeriodSeconds)
        {
            _service = service;
            _clock = clock;
            _backend = backend;
            _targets = targets;
            _coinPeriodSeconds = coinPeriodSeconds;
            _cueIndices = new int[CueKindCount];
            _requests = new int[CueKindCount];
            _played = new int[CueKindCount];
            _voiceStart = new double[backend.VoiceCount];
            _voiceActive = new bool[backend.VoiceCount];
            Resolve(StormCue.PegHitScored, PegHitScoredId);
            Resolve(StormCue.PegHitUnscored, PegHitUnscoredId);
            Resolve(StormCue.CoinSpawn, CoinSpawnId);
            Resolve(StormCue.JackpotHit, JackpotHitId);
        }

        public int GetRequests(StormCue cue)
        {
            return _requests[(int)cue];
        }

        public int GetPlayed(StormCue cue)
        {
            return _played[(int)cue];
        }

        public int GetDropped(StormCue cue)
        {
            return _requests[(int)cue] - _played[(int)cue];
        }

        public void Run(double startTime)
        {
            Run(startTime, _targets.StormTestSeconds);
        }

        public void Run(double startTime, double seconds)
        {
            double pegPeriod = 1d / _targets.StormPegHitRate;
            int steps = (int)(seconds * _targets.StormPegHitRate);
            double nextCoin = startTime;
            double nextJackpot = startTime + JackpotIntervalSeconds;
            for (int i = 0; i < steps; i++)
            {
                double now = startTime + i * pegPeriod;
                _clock.Set(now);
                FinishElapsedVoices(now);
                _service.Tick();
                Request(IsUnscoredStep(i) ? StormCue.PegHitUnscored : StormCue.PegHitScored, now);
                if (now >= nextCoin)
                {
                    Request(StormCue.CoinSpawn, now);
                    nextCoin += _coinPeriodSeconds;
                }

                if (now >= nextJackpot)
                {
                    Request(StormCue.JackpotHit, now);
                    nextJackpot += JackpotIntervalSeconds;
                }

                Steps++;
                int playing = _backend.PlayingCount;
                MaxPlayingVoices = playing > MaxPlayingVoices ? playing : MaxPlayingVoices;
            }
        }

        private static bool IsUnscoredStep(int step)
        {
            return (step + 1) % UnscoredEveryNthPegHit == 0;
        }

        private void Resolve(StormCue cue, string cueId)
        {
            Assert.IsTrue(_service.TryResolveCue(cueId, out int index), cueId);
            _cueIndices[(int)cue] = index;
        }

        private void Request(StormCue cue, double now)
        {
            int kind = (int)cue;
            _requests[kind]++;
            PlayResult result = _service.Play(_cueIndices[kind], AudioPlayParams.None);
            if (!result.Played)
            {
                return;
            }

            _played[kind]++;
            int voice = _backend.LastStartedVoice;
            _voiceStart[voice] = now;
            _voiceActive[voice] = true;
        }

        private void FinishElapsedVoices(double now)
        {
            for (int voice = 0; voice < _voiceActive.Length; voice++)
            {
                if (!_voiceActive[voice] || _backend.GetLoop(voice) || !_backend.IsPlaying(voice))
                {
                    continue;
                }

                UnityEngine.AudioClip clip = _backend.GetClip(voice);
                double length = clip == null ? 0d : clip.length;
                if (now - _voiceStart[voice] + FinishEpsilon >= length)
                {
                    _backend.FinishVoice(voice);
                    _voiceActive[voice] = false;
                }
            }
        }
    }
}
