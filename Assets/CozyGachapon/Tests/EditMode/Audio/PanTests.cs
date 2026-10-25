using CozyGachapon.Audio;
using CozyGachapon.Contracts;
using NUnit.Framework;

namespace CozyGachapon.Tests.Audio
{
    public class PanTests : AudioServiceFixture
    {
        private const string PannedId = "Test.Panned";
        private const string FlatId = "Test.Flat";
        private const float Tolerance = 1e-6f;

        [Test]
        public void AC17_PannedCue_ScalesAndClampsPosition()
        {
            InitializeWithPannedAndFlat(true);
            int cueIndex = ResolveCue(PannedId);
            float max = Config.MaxPan;
            float[] positions = { -1f, 0f, 1f, 5f, float.NaN, -5f, float.PositiveInfinity, float.NegativeInfinity };
            float[] expected = { -max, 0f, max, max, 0f, -max, 0f, 0f };

            for (int i = 0; i < positions.Length; i++)
            {
                int voice = PlayVoice(cueIndex, new AudioPlayParams(float.NaN, positions[i]));
                Assert.AreEqual(expected[i], Backend.GetPan(voice), Tolerance, "position " + positions[i]);
            }
        }

        [Test]
        public void AC17_CueWithoutFlag_AlwaysCentered()
        {
            InitializeWithPannedAndFlat(true);
            int cueIndex = ResolveCue(FlatId);
            float[] positions = { -1f, 0.5f, 1f, 5f, float.NaN };

            for (int i = 0; i < positions.Length; i++)
            {
                int voice = PlayVoice(cueIndex, new AudioPlayParams(float.NaN, positions[i]));
                Assert.AreEqual(0f, Backend.GetPan(voice), Tolerance);
            }
        }

        [Test]
        public void AC17_BackendWithoutPanSupport_AppliesCenter()
        {
            InitializeWithPannedAndFlat(false);
            int cueIndex = ResolveCue(PannedId);

            int voice = PlayVoice(cueIndex, new AudioPlayParams(float.NaN, 1f));

            Assert.AreEqual(0f, Backend.GetPan(voice), Tolerance);
        }

        [Test]
        public void AC17_EveryPlayedVoice_HasZeroSpatialBlend()
        {
            InitializeWithPannedAndFlat(true);
            int panned = ResolveCue(PannedId);
            int flat = ResolveCue(FlatId);

            int pannedVoice = PlayVoice(panned, new AudioPlayParams(float.NaN, 1f));
            int flatVoice = PlayVoice(flat, AudioPlayParams.None);

            Assert.AreEqual(0f, Backend.GetSpatialBlend(pannedVoice), Tolerance);
            Assert.AreEqual(0f, Backend.GetSpatialBlend(flatVoice), Tolerance);
        }

        [Test]
        public void PanCalculator_InvalidMaxPan_ReturnsCenter()
        {
            Assert.AreEqual(0f, PanCalculator.Resolve(true, 1f, float.NaN, true), Tolerance);
            Assert.AreEqual(1f, PanCalculator.Resolve(true, 1f, 7f, true), Tolerance);
        }

        private void InitializeWithPannedAndFlat(bool supportsPan)
        {
            SoundCue panned = CreateLimitedCue(PannedId, AudioBus.Plinko, 1, 0f, InstanceCapPolicy.DropNew);
            SetPanFromPosition(panned, true);
            SoundCue flat = CreateLimitedCue(FlatId, AudioBus.Plinko, 1, 0f, InstanceCapPolicy.DropNew);
            InitializeWithCues(panned, flat);
            Backend.SetSupportsPan(supportsPan);
        }
    }
}
