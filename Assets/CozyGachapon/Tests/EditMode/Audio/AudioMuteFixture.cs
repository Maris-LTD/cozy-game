using System;
using System.Collections.Generic;
using CozyGachapon.Audio;
using CozyGachapon.Contracts;
using NUnit.Framework;
using UnityEngine;

namespace CozyGachapon.Tests.Audio
{
    public abstract class AudioMuteFixture : AudioGainFixture
    {
        protected const string ShopLoopId = "Music.ShopLoop";
        protected const string PegHitId = "Plinko.PegHitScored";
        protected const string CrankId = "Gacha.Crank";
        protected const string TapId = "Ui.Tap";
        protected const string SoundOnId = "Ui.SoundOn";
        protected const float FadeTolerance = 0.001f;

        protected static float ResumeFadeSeconds => DefaultAudioAssets.LoadConfig().ResumeFadeInSeconds;
        protected static float UserMuteFadeSeconds => DefaultAudioAssets.LoadConfig().UserMuteFadeSeconds;
        protected static int DefaultVoiceCount => DefaultAudioAssets.LoadConfig().TotalVoices;

        protected int MuteChangedCount { get; private set; }
        protected bool LastMuteChanged { get; private set; }

        [SetUp]
        public void ResetMuteRecorder()
        {
            MuteChangedCount = 0;
            LastMuteChanged = false;
        }

        protected void InitializeWithMusic()
        {
            InitializeWithDefaultAssets(CreateMusicCue(ShopLoopId));
            Service.MuteChanged += RecordMuteChanged;
        }

        protected void RecordMuteChanged(bool muted)
        {
            MuteChangedCount++;
            LastMuteChanged = muted;
        }

        protected float[] CaptureVolumes(int[] voices)
        {
            float[] volumes = new float[voices.Length];
            for (int i = 0; i < voices.Length; i++)
            {
                volumes[i] = Backend.GetVolume(voices[i]);
            }

            return volumes;
        }

        protected float[] CapturePositions(int[] voices)
        {
            float[] positions = new float[voices.Length];
            for (int i = 0; i < voices.Length; i++)
            {
                positions[i] = Backend.GetPosition(voices[i]);
            }

            return positions;
        }

        protected void AssertScaledVolumes(int[] voices, float[] baseVolumes, float scale)
        {
            for (int i = 0; i < voices.Length; i++)
            {
                Assert.AreEqual(baseVolumes[i] * scale, Backend.GetVolume(voices[i]), FadeTolerance, voices[i].ToString());
            }
        }

        protected AudioService CreateServiceOnSameStore(FakeVoiceBackend backend, FakeAudioClock clock)
        {
            AudioConfig config = DefaultAudioAssets.LoadConfig();
            List<SoundCue> cues = DefaultAudioAssets.LoadCues();
            cues.Add(CreateMusicCue(ShopLoopId));
            AudioService service = new AudioService();
            Assert.IsTrue(service.Initialize(
                config,
                new SoundCueRepository(cues),
                backend,
                clock,
                new CountingAudioDiagnosticsSink(),
                new SystemAudioRandom(1),
                SettingsStore));
            return service;
        }

        protected static AudioClip FirstClipOf(string cueId)
        {
            List<SoundCue> cues = DefaultAudioAssets.LoadCues();
            for (int i = 0; i < cues.Count; i++)
            {
                if (cues[i].CueId == cueId)
                {
                    return cues[i].Clips[0];
                }
            }

            throw new ArgumentException(cueId);
        }
    }
}
