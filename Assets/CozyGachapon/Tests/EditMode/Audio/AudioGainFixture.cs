using CozyGachapon.Audio;
using CozyGachapon.Contracts;
using NUnit.Framework;
using UnityEngine;

namespace CozyGachapon.Tests.Audio
{
    public abstract class AudioGainFixture : AudioServiceFixture
    {
        protected const float VolumeTolerance = 0.0001f;
        protected const double SettleMargin = 0.05d;

        protected static float TransitionSeconds => DefaultAudioAssets.LoadConfig().BusTransitionSeconds;

        protected static float CueDb(string cueId)
        {
            System.Collections.Generic.List<SoundCue> cues = DefaultAudioAssets.LoadCues();
            for (int i = 0; i < cues.Count; i++)
            {
                if (cues[i].CueId == cueId)
                {
                    return cues[i].VolumeDb;
                }
            }

            Assert.Fail(cueId);
            return 0f;
        }

        protected static float Linear(float db)
        {
            return Mathf.Min(1f, Mathf.Pow(10f, db / 20f));
        }

        protected void AssertVolume(int voice, float expectedDb)
        {
            Assert.AreEqual(Linear(expectedDb), Backend.GetVolume(voice), VolumeTolerance);
        }

        protected int PlayHeld(string cueId)
        {
            Clock.Advance(1d);
            AssertPlayed(Service.Play(cueId, AudioPlayParams.None));
            return Backend.LastStartedVoice;
        }

        protected void AdvanceAndTick(double seconds)
        {
            Clock.Advance(seconds);
            Service.Tick();
        }

        protected void SettleBackground()
        {
            AdvanceAndTick(TransitionSeconds + SettleMargin);
        }
    }
}
