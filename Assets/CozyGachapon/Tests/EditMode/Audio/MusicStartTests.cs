using CozyGachapon.Contracts;
using NUnit.Framework;

namespace CozyGachapon.Tests.Audio
{
    public class MusicStartTests : MusicStartFixture
    {
        private const float VolumeTolerance = 1e-4f;
        private const float SilentVolume = 0f;
        private const float HalfFactor = 0.5f;

        [Test]
        public void AC26_Vs_UnlockThenInteractive_StartsOnlyWhenBoth()
        {
            Boot(true);
            Module.ServiceStep.Run();
            TickAt(StartTime);
            Assert.AreEqual(0, MusicBackend.StartCount);

            Unlock();
            TickAt(StartTime + TickStep);
            Assert.AreEqual(0, MusicBackend.StartCount);

            MarkInteractive();
            TickAt(StartTime + 2 * TickStep);

            Assert.AreEqual(1, MusicBackend.StartCount);
            int voice = MusicBackend.LastStartedVoice;
            Assert.Less(voice, MusicConfig.MusicVoices);
            Assert.IsTrue(MusicBackend.GetLoop(voice));
            Assert.AreEqual(1, MusicBackend.GetStartCount(voice));
            Assert.IsTrue(MusicBackend.IsPlaying(voice));
        }

        [Test]
        public void AC26_Vs_InteractiveThenUnlock_StartsOnlyWhenBoth()
        {
            Boot(true);
            MarkInteractive();
            Module.ServiceStep.Run();
            TickAt(StartTime);
            Assert.AreEqual(0, MusicBackend.StartCount);

            Unlock();
            TickAt(StartTime + TickStep);

            Assert.AreEqual(1, MusicBackend.StartCount);
            Assert.Less(MusicBackend.LastStartedVoice, MusicConfig.MusicVoices);
            Assert.IsTrue(MusicBackend.GetLoop(MusicBackend.LastStartedVoice));
        }

        [Test]
        public void AC26_Vs_FadeIn_FollowsFadeFactor()
        {
            Boot(true);
            Unlock();
            MarkInteractive();
            Module.ServiceStep.Run();
            double fade = MusicConfig.MusicFadeInSeconds;

            TickAt(StartTime);
            int voice = MusicBackend.LastStartedVoice;
            float atStart = MusicBackend.GetVolume(voice);
            TickAt(StartTime + fade * HalfFactor);
            float atHalf = MusicBackend.GetVolume(voice);
            TickAt(StartTime + fade);
            float atFull = MusicBackend.GetVolume(voice);
            TickAt(StartTime + fade * 2d);
            float afterFull = MusicBackend.GetVolume(voice);

            Assert.AreEqual(SilentVolume, atStart, VolumeTolerance);
            Assert.Greater(atFull, SilentVolume);
            Assert.AreEqual(atFull * HalfFactor, atHalf, VolumeTolerance);
            Assert.AreEqual(atFull, afterFull, VolumeTolerance);
        }

        [Test]
        public void AC26_Vs_SecondInteractive_DoesNotRestart()
        {
            Boot(true);
            Unlock();
            MarkInteractive();
            Module.ServiceStep.Run();
            TickAt(StartTime);

            MarkInteractive();
            RunFadeTicks();
            Unlock();
            MarkInteractive();
            RunFadeTicks();

            Assert.AreEqual(1, MusicVoiceStartTotal());
            Assert.AreEqual(1, MusicBackend.StartCount);
        }

        [Test]
        public void AC26_Vs_SuspendedWhenReady_StartsAfterResume()
        {
            Boot(true);
            Module.ServiceStep.Run();
            Unlock();
            Module.HandlePaused(new PausedEvent(PauseReason.Hidden));
            MarkInteractive();
            TickAt(StartTime);
            Assert.AreEqual(0, MusicBackend.StartCount);

            Module.HandleResumed(new ResumedEvent(PauseReason.Hidden, 1d));
            TickAt(StartTime + TickStep);

            Assert.AreEqual(1, MusicBackend.StartCount);
            Assert.Less(MusicBackend.LastStartedVoice, MusicConfig.MusicVoices);
        }

        [Test]
        public void AC26_NullMusicClip_NoMusic_SfxNormal()
        {
            Boot(true, false);
            Unlock();
            MarkInteractive();
            Module.ServiceStep.Run();
            TickAt(StartTime);

            Clock.Advance(1d);
            PlayResult result = Module.Service.Play("Plinko.CoinSpawn", AudioPlayParams.None);

            Assert.IsTrue(result.Played, result.Reason.ToString());
            Assert.AreEqual(0, MusicVoiceStartTotal());
            Assert.Greater(Sink.CountWarnings(AudioDiagnosticCode.CueExcluded), 0);
        }
    }
}
