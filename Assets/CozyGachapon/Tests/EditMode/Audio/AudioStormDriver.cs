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

        private const double FinishEpsilon = 1e-9;

        private static readonly int CueKindCount = System.Enum.GetValues(typeof(StormCue)).Length;

        private readonly AudioService _service;
        private readonly FakeAudioClock _clock;
        private readonly FakeVoiceBackend _backend;
        private readonly AudioStormShape _shape;
        private readonly int[] _cueIndices;
        private readonly int[] _requests;
        private readonly int[] _played;
        private readonly int[] _sameCueSteals;
        private readonly double[] _minSameCueStealAge;
        private readonly double[] _voiceStart;
        private readonly bool[] _voiceActive;

        public int Steps { get; private set; }
        public int MaxPlayingVoices { get; private set; }
        public int PlayingVoiceOverflowSteps { get; private set; }

        public AudioStormDriver(
            AudioService service,
            FakeAudioClock clock,
            FakeVoiceBackend backend,
            AudioStormShape shape)
        {
            _service = service;
            _clock = clock;
            _backend = backend;
            _shape = shape;
            _cueIndices = new int[CueKindCount];
            _requests = new int[CueKindCount];
            _played = new int[CueKindCount];
            _sameCueSteals = new int[CueKindCount];
            _minSameCueStealAge = new double[CueKindCount];
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

        public int GetSameCueSteals(StormCue cue)
        {
            return _sameCueSteals[(int)cue];
        }

        public double GetMinSameCueStealAge(StormCue cue)
        {
            return _sameCueSteals[(int)cue] == 0 ? 0d : _minSameCueStealAge[(int)cue];
        }

        public void Run(double startTime)
        {
            Run(startTime, _shape.Seconds);
        }

        public void Run(double startTime, double seconds)
        {
            double pegPeriod = 1d / _shape.PegHitRate;
            int steps = (int)(seconds * _shape.PegHitRate);
            double nextCoin = startTime;
            double nextJackpot = startTime + _shape.JackpotIntervalSeconds;
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
                    nextCoin += _shape.CoinPeriodSeconds;
                }

                if (now >= nextJackpot)
                {
                    Request(StormCue.JackpotHit, now);
                    nextJackpot += _shape.JackpotIntervalSeconds;
                }

                Steps++;
                int playing = _backend.PlayingCount;
                MaxPlayingVoices = playing > MaxPlayingVoices ? playing : MaxPlayingVoices;
                if (playing > _backend.VoiceCount)
                {
                    PlayingVoiceOverflowSteps++;
                }
            }
        }

        private bool IsUnscoredStep(int step)
        {
            return System.Math.Floor((step + 1) * _shape.UnscoredRatio) > System.Math.Floor(step * _shape.UnscoredRatio);
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
            int stealsBefore = _service.Stats.SameCueSteals;
            PlayResult result = _service.Play(_cueIndices[kind], AudioPlayParams.None);
            if (!result.Played)
            {
                return;
            }

            _played[kind]++;
            int voice = _backend.LastStartedVoice;
            if (_service.Stats.SameCueSteals > stealsBefore)
            {
                RecordSameCueSteal(kind, now - _voiceStart[voice]);
            }

            _voiceStart[voice] = now;
            _voiceActive[voice] = true;
        }

        private void RecordSameCueSteal(int kind, double victimAge)
        {
            if (_sameCueSteals[kind] == 0 || victimAge < _minSameCueStealAge[kind])
            {
                _minSameCueStealAge[kind] = victimAge;
            }

            _sameCueSteals[kind]++;
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
