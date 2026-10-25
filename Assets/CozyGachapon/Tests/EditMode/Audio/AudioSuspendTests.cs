using CozyGachapon.Audio;
using CozyGachapon.Contracts;
using NUnit.Framework;

namespace CozyGachapon.Tests.Audio
{
    public class AudioSuspendTests : AudioMuteFixture
    {
        private const double AwaySeconds = 60d;
        private const float PlaybackBeforePause = 2f;

        [TestCase(PauseReason.Hidden)]
        [TestCase(PauseReason.Background)]
        public void AC21_PausedThenResumed_HoldsPositionsDropsPlaysAndFadesIn(PauseReason reason)
        {
            InitializeWithMusic();
            int[] voices = { PlayHeld(PegHitId), PlayHeld(CrankId), PlayHeld(TapId), PlayHeld(ShopLoopId) };
            float[] baseVolumes = CaptureVolumes(voices);
            Backend.AdvancePlayback(PlaybackBeforePause);
            float[] heldPositions = CapturePositions(voices);
            int activeVoices = Service.Stats.ActiveVoices;

            Service.HandlePaused(new PausedEvent(reason));

            Assert.IsTrue(Service.RuntimeState.IsSuspended);
            Assert.AreEqual(voices.Length, Backend.PauseCount);
            for (int i = 0; i < voices.Length; i++)
            {
                Assert.IsTrue(Backend.IsPaused(voices[i]));
            }

            Clock.Advance(AwaySeconds);
            Backend.AdvancePlayback((float)AwaySeconds);
            Service.Tick();

            CollectionAssert.AreEqual(heldPositions, CapturePositions(voices));
            Assert.AreEqual(activeVoices, Service.Stats.ActiveVoices);
            AssertDropped(DropReason.Suspended, Service.Play(PegHitId, AudioPlayParams.None));
            AssertDropped(DropReason.Suspended, Service.Play(ShopLoopId, AudioPlayParams.None));

            Service.HandleResumed(new ResumedEvent(reason, AwaySeconds));

            Assert.IsFalse(Service.RuntimeState.IsSuspended);
            Assert.AreEqual(voices.Length, Backend.ResumeCount);
            AssertScaledVolumes(voices, baseVolumes, 0f);
            AdvanceAndTick(ResumeFadeSeconds * 0.5f);
            AssertScaledVolumes(voices, baseVolumes, 0.5f);
            AdvanceAndTick(ResumeFadeSeconds * 0.5f + SettleMargin);
            AssertScaledVolumes(voices, baseVolumes, 1f);

            Backend.AdvancePlayback(1f);
            float[] resumedPositions = CapturePositions(voices);
            for (int i = 0; i < voices.Length; i++)
            {
                Assert.AreEqual(heldPositions[i] + 1f, resumedPositions[i], FadeTolerance);
            }
        }

        [Test]
        public void Resumed_WithoutPaused_ChangesNothing()
        {
            InitializeWithMusic();
            int music = PlayHeld(ShopLoopId);
            float volume = Backend.GetVolume(music);

            Service.HandleResumed(new ResumedEvent(PauseReason.Hidden, AwaySeconds));
            AdvanceAndTick(ResumeFadeSeconds * 0.5f);

            Assert.AreEqual(0, Backend.ResumeCount);
            Assert.AreEqual(0, Backend.PauseCount);
            Assert.AreEqual(volume, Backend.GetVolume(music), FadeTolerance);
            Assert.AreEqual(0, MuteChangedCount);
            AssertPlayed(Service.Play(PegHitId, AudioPlayParams.None));
        }

        [Test]
        public void PausedTwice_PausesOnce_AndOneResumedIsEnough()
        {
            InitializeWithMusic();
            int[] voices = { PlayHeld(ShopLoopId), PlayHeld(PegHitId) };

            Service.HandlePaused(new PausedEvent(PauseReason.Hidden));
            Service.HandlePaused(new PausedEvent(PauseReason.Background));

            Assert.AreEqual(voices.Length, Backend.PauseCount);

            Service.HandleResumed(new ResumedEvent(PauseReason.Hidden, AwaySeconds));

            Assert.IsFalse(Service.RuntimeState.IsSuspended);
            Assert.AreEqual(voices.Length, Backend.ResumeCount);
            Clock.Advance(1d);
            AssertPlayed(Service.Play(CrankId, AudioPlayParams.None));
        }

        [Test]
        public void UserMuted_ThenPausedAndResumed_StaysMuted()
        {
            InitializeWithMusic();
            int music = PlayHeld(ShopLoopId);
            Service.SetUserMuted(true);
            AdvanceAndTick(UserMuteFadeSeconds + SettleMargin);

            Service.HandlePaused(new PausedEvent(PauseReason.Background));
            AdvanceAndTick(AwaySeconds);
            Service.HandleResumed(new ResumedEvent(PauseReason.Background, AwaySeconds));
            AdvanceAndTick(ResumeFadeSeconds + SettleMargin);

            Assert.IsFalse(Service.RuntimeState.IsSuspended);
            Assert.IsTrue(Service.IsUserMuted);
            Assert.AreEqual(0f, Backend.GetVolume(music), FadeTolerance);
            Assert.IsFalse(Backend.IsPaused(music));
            Assert.AreEqual(1, MuteChangedCount);
        }

        [Test]
        public void PausedNonLoopVoice_IsNotReleasedByTick()
        {
            InitializeWithMusic();
            int voice = PlayHeld(PegHitId);
            int activeVoices = Service.Stats.ActiveVoices;

            Service.HandlePaused(new PausedEvent(PauseReason.Hidden));
            AdvanceAndTick(1d);

            Assert.IsFalse(Backend.IsPlaying(voice));
            Assert.AreEqual(activeVoices, Service.Stats.ActiveVoices);

            Service.HandleResumed(new ResumedEvent(PauseReason.Hidden, 1d));
            Backend.FinishVoice(voice);
            AdvanceAndTick(SettleMargin);

            Assert.AreEqual(activeVoices - 1, Service.Stats.ActiveVoices);
        }

        [Test]
        public void HandlePaused_BeforeInitialize_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => Service.HandlePaused(new PausedEvent(PauseReason.Hidden)));
            Assert.DoesNotThrow(() => Service.HandleResumed(new ResumedEvent(PauseReason.Hidden, AwaySeconds)));
            Assert.IsFalse(Service.RuntimeState.IsSuspended);
        }

        [Test]
        public void MuteReasonSet_TogglesEachReasonIndependently()
        {
            MuteReasonSet set = default;

            Assert.IsTrue(set.Set(MuteReason.Background, true));
            Assert.IsFalse(set.Set(MuteReason.Background, true));
            Assert.IsTrue(set.IsSuspended);
            Assert.IsFalse(set.IsUserMuted);

            Assert.IsTrue(set.Set(MuteReason.User, true));
            Assert.IsTrue(set.Set(MuteReason.Ad, true));
            Assert.AreEqual(MuteReason.Ad | MuteReason.Background, set.SuspendReasons);

            Assert.IsTrue(set.Set(MuteReason.Background, false));
            Assert.IsTrue(set.IsSuspended);
            Assert.IsTrue(set.Set(MuteReason.Ad, false));
            Assert.IsFalse(set.IsSuspended);
            Assert.IsTrue(set.IsUserMuted);
            Assert.AreEqual(MuteReason.User, set.Reasons);

            Assert.IsFalse(set.Set(MuteReason.None, true));
            Assert.IsTrue(set.Set(MuteReason.User, false));
            Assert.AreEqual(MuteReason.None, set.Reasons);
        }
    }
}
