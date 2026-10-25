using CozyGachapon.Audio;
using CozyGachapon.Contracts;
using NUnit.Framework;
using UnityEngine;

namespace CozyGachapon.Tests.Audio
{
    public class ScalePitchRangeTests : AudioServiceFixture
    {
        private const string PegHitScoredId = "Plinko.PegHitScored";
        private const string JackpotBurstSpawnId = "Plinko.JackpotBurstSpawn";
        private const int Iterations = 10000;
        private const float SemitonesPerOctave = 12f;

        [Test]
        public void AC15_PegHitScored_SeededRandom_StaysInRange()
        {
            AssertPitchRange(PegHitScoredId);
        }

        [Test]
        public void AC15_JackpotBurstSpawn_SeededRandom_StaysInRange()
        {
            AssertPitchRange(JackpotBurstSpawnId);
        }

        private void AssertPitchRange(string cueId)
        {
            InitializeWithDefaultAssets();
            AudioConfig config = DefaultAudioAssets.LoadConfig();
            int cueIndex = ResolveCue(cueId);
            SoundCue cue = null;
            foreach (SoundCue candidate in DefaultAudioAssets.LoadCues())
            {
                if (candidate.CueId == cueId)
                {
                    cue = candidate;
                }
            }

            Assert.IsNotNull(cue);
            float shiftRatio = Mathf.Pow(2f, config.MaxSampleShiftSemitones / SemitonesPerOctave);
            float lower = (1f / shiftRatio) * (1f - cue.PitchJitterRatio);
            float upper = shiftRatio * (1f + cue.PitchJitterRatio);
            float lastNote = config.ScaleNoteCount - 1;

            for (int i = 0; i < Iterations; i++)
            {
                float position = (i % config.ScaleNoteCount) / lastNote;
                int voice = PlayVoice(cueIndex, new AudioPlayParams(position, float.NaN));
                float pitch = Backend.GetPitch(voice);
                Assert.Greater(pitch, 0f);
                Assert.GreaterOrEqual(pitch, lower, "iteration " + i);
                Assert.LessOrEqual(pitch, upper, "iteration " + i);
            }

            Assert.AreEqual(Iterations, Service.Stats.Played);
        }
    }
}
