using CozyGachapon.Audio;
using CozyGachapon.Contracts;
using NUnit.Framework;

namespace CozyGachapon.Tests.Audio
{
    public class AdMuteReasonTests : AudioMuteFixture
    {
        private const string JackpotHitId = "Plinko.JackpotHit";
        private const string SfxLoopId = "Test.SfxLoop";
        private const float PlaybackBeforeAd = 2f;
        private const double AwaySeconds = 30d;

        private static float StormSeconds => DefaultAudioAssets.LoadTargets().StormTestSeconds;

        [Test]
        public void AC43_AdOpen_StopsSfxPausesMusicDropsPlaysAndResumesMusicOnly()
        {
            InitializeWithMusic();
            int[] sfx = { PlayHeld(PegHitId), PlayHeld(CrankId), PlayHeld(TapId) };
            int music = PlayHeld(ShopLoopId);
            float baseVolume = Backend.GetVolume(music);
            Backend.AdvancePlayback(PlaybackBeforeAd);
            float heldPosition = Backend.GetPosition(music);
            int startCount = Backend.StartCount;

            Service.SetMuteReason(MuteReason.Ad, true);

            Assert.IsTrue(Service.RuntimeState.IsSuspended);
            for (int i = 0; i < sfx.Length; i++)
            {
                Assert.IsFalse(Backend.IsPlaying(sfx[i]));
                Assert.AreEqual(1, Backend.GetStopCount(sfx[i]));
            }

            Assert.AreEqual(1, Service.Stats.ActiveVoices);
            Assert.IsTrue(Backend.IsPaused(music));
            Assert.AreEqual(1, Backend.PauseCount);

            Clock.Advance(1d);
            AssertDropped(DropReason.Suspended, Service.Play(PegHitId, AudioPlayParams.None));
            AssertDropped(DropReason.Suspended, Service.Play(JackpotHitId, AudioPlayParams.None));
            Assert.AreEqual(2, Service.Stats.GetDropped(DropReason.Suspended));

            Clock.Advance(StormSeconds);
            Backend.AdvancePlayback(StormSeconds);
            Service.Tick();
            Assert.AreEqual(heldPosition, Backend.GetPosition(music), FadeTolerance);

            Service.SetMuteReason(MuteReason.Ad, false);

            Assert.IsFalse(Service.RuntimeState.IsSuspended);
            Assert.AreEqual(1, Backend.ResumeCount);
            Assert.AreEqual(1, Backend.PlayingCount);
            Assert.AreEqual(startCount, Backend.StartCount);
            Assert.AreEqual(0f, Backend.GetVolume(music), FadeTolerance);
            AdvanceAndTick(ResumeFadeSeconds * 0.5f);
            Assert.AreEqual(baseVolume * 0.5f, Backend.GetVolume(music), FadeTolerance);
            AdvanceAndTick(ResumeFadeSeconds * 0.5f + SettleMargin);
            Assert.AreEqual(baseVolume, Backend.GetVolume(music), FadeTolerance);
            Assert.AreEqual(heldPosition, Backend.GetPosition(music), FadeTolerance);
            Assert.AreEqual(1, Backend.PlayingCount);
            Assert.AreEqual(1, Service.Stats.ActiveVoices);

            Clock.Advance(1d);
            AssertPlayed(Service.Play(PegHitId, AudioPlayParams.None));
        }

        [Test]
        public void AdWhileBackground_StopsPausedSfx_KeepsMusicPausedOnce()
        {
            InitializeWithMusic();
            int sfx = PlayHeld(PegHitId);
            int music = PlayHeld(ShopLoopId);
            Service.HandlePaused(new PausedEvent(PauseReason.Hidden));
            int pauseCount = Backend.PauseCount;

            Service.SetMuteReason(MuteReason.Ad, true);

            Assert.AreEqual(1, Backend.GetStopCount(sfx));
            Assert.IsFalse(Backend.IsPaused(sfx));
            Assert.IsTrue(Backend.IsPaused(music));
            Assert.AreEqual(pauseCount, Backend.PauseCount);
            Assert.AreEqual(1, Service.Stats.ActiveVoices);

            Service.HandleResumed(new ResumedEvent(PauseReason.Hidden, AwaySeconds));
            Assert.IsTrue(Service.RuntimeState.IsSuspended);
            Assert.AreEqual(0, Backend.ResumeCount);

            Service.SetMuteReason(MuteReason.Ad, false);
            Assert.IsFalse(Service.RuntimeState.IsSuspended);
            Assert.AreEqual(1, Backend.ResumeCount);
            Assert.IsFalse(Backend.IsPaused(music));
            Assert.AreEqual(1, Backend.PlayingCount);
        }

        [Test]
        public void AdRaisedTwice_DoesNotStopOrPauseAgain()
        {
            InitializeWithMusic();
            PlayHeld(PegHitId);
            PlayHeld(ShopLoopId);

            Service.SetMuteReason(MuteReason.Ad, true);
            int stops = Backend.StopCount;
            int pauses = Backend.PauseCount;
            Service.SetMuteReason(MuteReason.Ad, true);

            Assert.AreEqual(stops, Backend.StopCount);
            Assert.AreEqual(pauses, Backend.PauseCount);
        }

        [Test]
        public void AdCleared_WhenNeverRaised_ChangesNothing()
        {
            InitializeWithMusic();
            int music = PlayHeld(ShopLoopId);
            float volume = Backend.GetVolume(music);

            Service.SetMuteReason(MuteReason.Ad, false);
            AdvanceAndTick(ResumeFadeSeconds * 0.5f);

            Assert.AreEqual(0, Backend.ResumeCount);
            Assert.AreEqual(0, Backend.StopCount);
            Assert.AreEqual(volume, Backend.GetVolume(music), FadeTolerance);
        }

        [Test]
        public void AdRaised_StopsLoopingSfxToo()
        {
            SoundCue loopCue = CreateLimitedCue(SfxLoopId, AudioBus.Sfx, 2, 0f, InstanceCapPolicy.StealOldest);
            SetLoop(loopCue, true);
            InitializeWithDefaultAssets(CreateMusicCue(ShopLoopId), loopCue);
            int loop = PlayHeld(SfxLoopId);
            int music = PlayHeld(ShopLoopId);

            Service.SetMuteReason(MuteReason.Ad, true);

            Assert.IsFalse(Backend.IsPlaying(loop));
            Assert.AreEqual(1, Backend.GetStopCount(loop));
            Assert.AreEqual(1, Service.Stats.ActiveVoices);
            Assert.IsTrue(Backend.IsPaused(music));
        }

        [Test]
        public void TickDuringAd_KeepsPausedMusicVoice()
        {
            InitializeWithMusic();
            PlayHeld(PegHitId);
            PlayHeld(ShopLoopId);

            Service.SetMuteReason(MuteReason.Ad, true);
            AdvanceAndTick(StormSeconds);

            Assert.AreEqual(1, Service.Stats.ActiveVoices);
        }

        [Test]
        public void SetMuteReasonAd_BeforeInitialize_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => Service.SetMuteReason(MuteReason.Ad, true));
            Assert.DoesNotThrow(() => Service.SetMuteReason(MuteReason.Ad, false));
            Assert.IsFalse(Service.RuntimeState.IsSuspended);
        }
    }
}
