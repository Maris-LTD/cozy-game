using System.Collections.Generic;
using CozyGachapon.Audio;
using CozyGachapon.Contracts;
using NUnit.Framework;
using UnityEngine;

namespace CozyGachapon.Tests.Audio
{
    public class ClipSelectionTests : AudioServiceFixture
    {
        private const string NoneId = "Test.None";
        private const string RandomId = "Test.Random";
        private const string OtherRandomId = "Test.OtherRandom";
        private const string SingleId = "Test.Single";
        private const int Iterations = 1000;
        private const int ClipCount = 3;
        private const float Jitter = 0.03f;
        private const float Tolerance = 1e-6f;

        [Test]
        public void AC16_NoneCue_AlwaysNeutralPitch()
        {
            InitializeWithCues(CreateCue(NoneId));
            int cueIndex = ResolveCue(NoneId);

            for (int i = 0; i < Iterations; i++)
            {
                int voice = PlayVoice(cueIndex, AudioPlayParams.None);
                Assert.AreEqual(1f, Backend.GetPitch(voice), Tolerance);
            }
        }

        [Test]
        public void AC16_RandomCue_JitterInRange_NoImmediateRepeat_AllClipsUsed()
        {
            SoundCue cue = CreateRandomCue(RandomId, ClipCount, true);
            InitializeWithCues(cue);
            int cueIndex = ResolveCue(RandomId);
            HashSet<AudioClip> used = new HashSet<AudioClip>();
            AudioClip previous = null;

            for (int i = 0; i < Iterations; i++)
            {
                int voice = PlayVoice(cueIndex, AudioPlayParams.None);
                AudioClip clip = Backend.GetClip(voice);
                float pitch = Backend.GetPitch(voice);
                Assert.GreaterOrEqual(pitch, 1f - Jitter - Tolerance);
                Assert.LessOrEqual(pitch, 1f + Jitter + Tolerance);
                Assert.AreNotSame(previous, clip, "iteration " + i);
                used.Add(clip);
                previous = clip;
            }

            Assert.AreEqual(ClipCount, used.Count);
        }

        [Test]
        public void AvoidRepeat_FixedRandomZero_StillAlternates()
        {
            UseRandom(new FixedAudioRandom(0f, 0));
            SoundCue cue = CreateRandomCue(RandomId, ClipCount, true);
            InitializeWithCues(cue);
            int cueIndex = ResolveCue(RandomId);

            int first = PlayVoice(cueIndex, AudioPlayParams.None);
            AudioClip firstClip = Backend.GetClip(first);
            int second = PlayVoice(cueIndex, AudioPlayParams.None);
            AudioClip secondClip = Backend.GetClip(second);
            int third = PlayVoice(cueIndex, AudioPlayParams.None);

            Assert.AreSame(cue.Clips[0], firstClip);
            Assert.AreSame(cue.Clips[1], secondClip);
            Assert.AreSame(cue.Clips[0], Backend.GetClip(third));
            Assert.AreEqual(1f, Backend.GetPitch(first), Tolerance);
        }

        [Test]
        public void AvoidRepeat_FirstPlay_CanPickTheLastClip()
        {
            UseRandom(new FixedAudioRandom(0f, ClipCount - 1));
            SoundCue cue = CreateRandomCue(RandomId, ClipCount, true);
            InitializeWithCues(cue);
            int cueIndex = ResolveCue(RandomId);

            int first = PlayVoice(cueIndex, AudioPlayParams.None);

            Assert.AreSame(cue.Clips[ClipCount - 1], Backend.GetClip(first));
        }

        [Test]
        public void WithoutAvoidRepeat_FixedRandomZero_RepeatsSameClip()
        {
            UseRandom(new FixedAudioRandom(0f, 0));
            SoundCue cue = CreateRandomCue(RandomId, ClipCount, false);
            InitializeWithCues(cue);
            int cueIndex = ResolveCue(RandomId);

            int first = PlayVoice(cueIndex, AudioPlayParams.None);
            AudioClip firstClip = Backend.GetClip(first);
            int second = PlayVoice(cueIndex, AudioPlayParams.None);

            Assert.AreSame(firstClip, Backend.GetClip(second));
        }

        [Test]
        public void SingleClipCue_AvoidRepeat_StillPlaysThatClip()
        {
            SoundCue cue = CreateRandomCue(SingleId, 1, true);
            InitializeWithCues(cue);
            int cueIndex = ResolveCue(SingleId);

            for (int i = 0; i < Iterations; i++)
            {
                int voice = PlayVoice(cueIndex, AudioPlayParams.None);
                Assert.AreSame(cue.Clips[0], Backend.GetClip(voice));
            }
        }

        [Test]
        public void AvoidRepeat_MemoryIsPerCue()
        {
            UseRandom(new FixedAudioRandom(0f, 0));
            SoundCue first = CreateRandomCue(RandomId, ClipCount, true);
            SoundCue second = CreateRandomCue(OtherRandomId, ClipCount, true);
            InitializeWithCues(first, second);
            int firstIndex = ResolveCue(RandomId);
            int secondIndex = ResolveCue(OtherRandomId);

            PlayVoice(firstIndex, AudioPlayParams.None);
            int otherVoice = PlayVoice(secondIndex, AudioPlayParams.None);

            Assert.AreSame(second.Clips[0], Backend.GetClip(otherVoice));
        }

        private SoundCue CreateRandomCue(string cueId, int clipCount, bool avoidRepeat)
        {
            SoundCue cue = CreateLimitedCue(cueId, AudioBus.Plinko, 1, 0f, InstanceCapPolicy.DropNew);
            AudioClip[] clips = new AudioClip[clipCount];
            for (int i = 0; i < clipCount; i++)
            {
                clips[i] = CreateClip();
            }

            cue.SetClipsForTest(clips);
            cue.SetPitchForTest(PitchMode.Random, Jitter, new PitchedSample[0]);
            SetAvoidRepeat(cue, avoidRepeat);
            return cue;
        }
    }
}
