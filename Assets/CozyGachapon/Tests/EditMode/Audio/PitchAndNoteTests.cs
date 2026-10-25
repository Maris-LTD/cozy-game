using System.Collections.Generic;
using CozyGachapon.Audio;
using CozyGachapon.Contracts;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace CozyGachapon.Tests.Audio
{
    public class PitchAndNoteTests : AudioServiceFixture
    {
        private const string PegHitScoredId = "Plinko.PegHitScored";
        private const string JackpotBurstSpawnId = "Plinko.JackpotBurstSpawn";
        private const string MisconfiguredId = "Test.Misconfigured";
        private const float Tolerance = 1e-5f;
        private const float HugeJitter = 5f;

        private static readonly int[] ExpectedSemitones = { 0, 2, 4, 7, 9, 12, 14, 16, 19, 21 };
        private static readonly int[] ExpectedSamples = { 0, 0, 0, 0, 1, 1, 1, 2, 2, 2 };
        private static readonly int[] ExpectedShifts = { -4, -2, 0, 3, -3, 0, 2, -3, 0, 2 };
        private static readonly int[] BurstNotes = { 0, 2, 5, 7, 9 };

        [Test]
        public void AC14_NoteIndex_ClampsAndFallsBack()
        {
            int fallbackNote = Mathf.FloorToInt(Config.FallbackPitchPosition * (Config.ScaleNoteCount - 1) + 0.5f);
            float[] positions = { 0f, 1f, 0.727f, -0.5f, 1.7f, float.NaN, float.PositiveInfinity, float.NegativeInfinity };
            int[] expected = { 0, 9, 7, 0, 9, fallbackNote, fallbackNote, fallbackNote };

            for (int i = 0; i < positions.Length; i++)
            {
                int note = NoteIndexCalculator.NoteIndex(positions[i], Config.FallbackPitchPosition, Config.ScaleNoteCount);
                Assert.AreEqual(expected[i], note, "position " + positions[i]);
            }

            Assert.AreEqual(5, fallbackNote);
        }

        [Test]
        public void AC14_Semitone_FollowsPentatonicTable()
        {
            for (int n = 0; n < ExpectedSemitones.Length; n++)
            {
                Assert.AreEqual(ExpectedSemitones[n], NoteIndexCalculator.Semitone(n, Config.ScaleIntervals), "n " + n);
            }
        }

        [Test]
        public void NoteIndex_InvalidFallbackOrCount_StaysInRange()
        {
            Assert.AreEqual(0, NoteIndexCalculator.NoteIndex(float.NaN, float.NaN, Config.ScaleNoteCount));
            Assert.AreEqual(0, NoteIndexCalculator.NoteIndex(0.5f, 0.5f, 0));
            Assert.AreEqual(0, NoteIndexCalculator.NoteIndex(0.5f, 0.5f, 1));
        }

        [Test]
        public void AC14_PlayWithoutPitchPosition_UsesFallbackNote()
        {
            UseRandom(new FixedAudioRandom());
            InitializeWithDefaultAssets();

            int voice = PlayVoice(ResolveCue(PegHitScoredId), AudioPlayParams.None);

            AssertSample(voice, PegHitScoredId, 1, 0);
        }

        [Test]
        public void AC15_PegHitScored_FixedRandom_MatchesSamplePitchTable()
        {
            AssertScaleTable(PegHitScoredId);
        }

        [Test]
        public void AC15_JackpotBurstSpawn_FixedRandom_MatchesSamplePitchTable()
        {
            AssertScaleTable(JackpotBurstSpawnId);
        }

        [Test]
        public void AC15_JackpotBurstSpawn_BurstIndexMapsToNotes()
        {
            UseRandom(new FixedAudioRandom());
            InitializeWithDefaultAssets();
            int burst = ResolveCue(JackpotBurstSpawnId);
            int[] expectedSamples = { 0, 0, 1, 2, 2 };
            int[] expectedShifts = { -4, 0, 0, -3, 2 };

            for (int burstIndex = 0; burstIndex < BurstNotes.Length; burstIndex++)
            {
                float position = burstIndex / (float)(BurstNotes.Length - 1);
                int note = NoteIndexCalculator.NoteIndex(position, Config.FallbackPitchPosition, Config.ScaleNoteCount);
                Assert.AreEqual(BurstNotes[burstIndex], note, "burst " + burstIndex);

                int voice = PlayVoice(burst, new AudioPlayParams(position, float.NaN));
                AssertSample(voice, JackpotBurstSpawnId, expectedSamples[burstIndex], expectedShifts[burstIndex]);
            }
        }

        [Test]
        public void Jitter_MinusOne_LowersPitchByCueRatio()
        {
            AssertJitter(-1f);
        }

        [Test]
        public void Jitter_PlusOne_RaisesPitchByCueRatio()
        {
            AssertJitter(1f);
        }

        [Test]
        public void Tie_BetweenTwoSamples_PicksLowerNativeSemitone()
        {
            PitchedSample low = new PitchedSample(CreateClip(), 4);
            PitchedSample high = new PitchedSample(CreateClip(), 8);

            Assert.IsTrue(SamplePitchSelector.TrySelectSample(6, new[] { low, high }, out int index, out int shift));
            Assert.AreEqual(0, index);
            Assert.AreEqual(2, shift);
            Assert.IsTrue(SamplePitchSelector.TrySelectSample(6, new[] { high, low }, out index, out shift));
            Assert.AreEqual(1, index);
            Assert.AreEqual(2, shift);
        }

        [Test]
        public void MisconfiguredScale_EmptyIntervals_PlaysNeutralPitchWithDefaultClip()
        {
            SerializedObject serialized = new SerializedObject(Config);
            serialized.FindProperty("_scaleIntervals").arraySize = 0;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            SoundCue cue = CreateScaleCue(MisconfiguredId, true);
            InitializeWithCues(cue);

            int voice = PlayVoice(ResolveCue(MisconfiguredId), new AudioPlayParams(1f, float.NaN));

            Assert.AreEqual(1f, Backend.GetPitch(voice), Tolerance);
            Assert.AreSame(cue.PitchedSamples[0].Clip, Backend.GetClip(voice));
        }

        [Test]
        public void MisconfiguredJitter_NeverProducesNonPositivePitch()
        {
            UseRandom(new FixedAudioRandom(-1f));
            SoundCue random = CreateCue(MisconfiguredId);
            random.SetPitchForTest(PitchMode.Random, HugeJitter, new PitchedSample[0]);
            SoundCue scale = CreateScaleCue(MisconfiguredId + ".Scale", true);
            scale.SetPitchForTest(PitchMode.Scale, HugeJitter, new[] { new PitchedSample(CreateClip(), 4) });
            InitializeWithCues(random, scale);

            int randomVoice = PlayVoice(ResolveCue(MisconfiguredId), AudioPlayParams.None);
            int scaleVoice = PlayVoice(ResolveCue(MisconfiguredId + ".Scale"), AudioPlayParams.None);

            Assert.Greater(Backend.GetPitch(randomVoice), 0f);
            Assert.Greater(Backend.GetPitch(scaleVoice), 0f);
        }

        private void AssertJitter(float signed)
        {
            UseRandom(new FixedAudioRandom(signed));
            InitializeWithDefaultAssets();
            SoundCue cue = FindCue(PegHitScoredId);

            int voice = PlayVoice(ResolveCue(PegHitScoredId), new AudioPlayParams(1f, float.NaN));

            float expected = AudioMath.SemitoneRatio(ExpectedShifts[Config.ScaleNoteCount - 1]) * (1f + cue.PitchJitterRatio * signed);
            Assert.AreEqual(expected, Backend.GetPitch(voice), Tolerance);
        }

        private void AssertScaleTable(string cueId)
        {
            UseRandom(new FixedAudioRandom());
            InitializeWithDefaultAssets();
            int cueIndex = ResolveCue(cueId);
            int lastNote = Config.ScaleNoteCount - 1;

            for (int n = 0; n <= lastNote; n++)
            {
                float position = n / (float)lastNote;
                int voice = PlayVoice(cueIndex, new AudioPlayParams(position, float.NaN));
                AssertSample(voice, cueId, ExpectedSamples[n], ExpectedShifts[n]);
                Assert.LessOrEqual(Mathf.Abs(ExpectedShifts[n]), Config.MaxSampleShiftSemitones);
            }
        }

        private void AssertSample(int voice, string cueId, int sampleIndex, int shift)
        {
            SoundCue cue = FindCue(cueId);
            Assert.AreSame(cue.PitchedSamples[sampleIndex].Clip, Backend.GetClip(voice));
            Assert.AreEqual(AudioMath.SemitoneRatio(shift), Backend.GetPitch(voice), Tolerance);
        }

        private static SoundCue FindCue(string cueId)
        {
            List<SoundCue> cues = DefaultAudioAssets.LoadCues();
            for (int i = 0; i < cues.Count; i++)
            {
                if (cues[i].CueId == cueId)
                {
                    return cues[i];
                }
            }

            Assert.Fail(cueId);
            return null;
        }
    }
}
