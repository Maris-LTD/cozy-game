using CozyGachapon.Contracts;
using NUnit.Framework;

namespace CozyGachapon.Tests.Audio
{
    public class MuteReasonStackTests : AudioMuteFixture
    {
        private const double AwaySeconds = 30d;

        [Test]
        public void AC22_AdBackgroundStacking_SuspendsUntilAllSuspendReasonsCleared()
        {
            InitializeWithMusic();
            int music = PlayHeld(ShopLoopId);
            float baseVolume = Backend.GetVolume(music);
            Assert.IsFalse(Service.RuntimeState.IsSuspended);

            Service.SetMuteReason(MuteReason.Ad, true);
            Assert.IsTrue(Service.RuntimeState.IsSuspended);

            Service.HandlePaused(new PausedEvent(PauseReason.Hidden));
            Assert.IsTrue(Service.RuntimeState.IsSuspended);

            Service.SetMuteReason(MuteReason.Ad, false);
            Assert.IsTrue(Service.RuntimeState.IsSuspended);
            Assert.AreEqual(0, Backend.ResumeCount);

            Service.HandleResumed(new ResumedEvent(PauseReason.Hidden, AwaySeconds));
            Assert.IsFalse(Service.RuntimeState.IsSuspended);
            Assert.AreEqual(1, Backend.ResumeCount);
            Assert.AreEqual(0f, Backend.GetVolume(music), FadeTolerance);
            AdvanceAndTick(ResumeFadeSeconds * 0.5f);
            Assert.AreEqual(baseVolume * 0.5f, Backend.GetVolume(music), FadeTolerance);
            AdvanceAndTick(ResumeFadeSeconds * 0.5f + SettleMargin);
            Assert.AreEqual(baseVolume, Backend.GetVolume(music), FadeTolerance);
        }

        [Test]
        public void AC22_FocusLostWithoutPaused_StaysActive()
        {
            InitializeWithMusic();
            PlayHeld(ShopLoopId);

            AdvanceAndTick(AwaySeconds);

            Assert.IsFalse(Service.RuntimeState.IsSuspended);
            Assert.AreEqual(0, Backend.PauseCount);
            AssertPlayed(Service.Play(PegHitId, AudioPlayParams.None));
        }

        [Test]
        public void UserEnabledMidway_AfterSuspendReasonsCleared_IsUserMuted()
        {
            InitializeWithMusic();
            int music = PlayHeld(ShopLoopId);

            Service.SetMuteReason(MuteReason.Ad, true);
            Service.HandlePaused(new PausedEvent(PauseReason.Background));
            Service.SetMuteReason(MuteReason.User, true);
            Service.SetMuteReason(MuteReason.Ad, false);
            Service.HandleResumed(new ResumedEvent(PauseReason.Background, AwaySeconds));
            AdvanceAndTick(ResumeFadeSeconds + UserMuteFadeSeconds + SettleMargin);

            Assert.IsFalse(Service.RuntimeState.IsSuspended);
            Assert.IsTrue(Service.IsUserMuted);
            Assert.AreEqual(1, MuteChangedCount);
            Assert.IsFalse(Backend.IsPaused(music));
            Assert.AreEqual(0f, Backend.GetVolume(music), FadeTolerance);
        }

        [Test]
        public void SetMuteReasonUser_RaisesOnce_AndSharesPathWithSetUserMuted()
        {
            InitializeWithMusic();

            Service.SetMuteReason(MuteReason.User, true);

            Assert.AreEqual(1, MuteChangedCount);
            Assert.IsTrue(LastMuteChanged);
            Assert.IsTrue(Service.IsUserMuted);
            Assert.IsTrue(SettingsStore.StoredUserMuted);

            Service.SetMuteReason(MuteReason.User, true);
            Service.SetUserMuted(true);
            Assert.AreEqual(1, MuteChangedCount);

            Service.SetMuteReason(MuteReason.User, false);
            Service.SetUserMuted(false);
            Assert.AreEqual(2, MuteChangedCount);
            Assert.IsFalse(LastMuteChanged);
            Assert.IsFalse(Service.IsUserMuted);
        }

        [Test]
        public void CombinedFlags_SuspendUntilLastReasonCleared()
        {
            InitializeWithMusic();
            PlayHeld(ShopLoopId);

            Service.SetMuteReason(MuteReason.Ad | MuteReason.Background, true);
            Assert.IsTrue(Service.RuntimeState.IsSuspended);

            Service.SetMuteReason(MuteReason.Ad, false);
            Assert.IsTrue(Service.RuntimeState.IsSuspended);

            Service.SetMuteReason(MuteReason.Background, false);
            Assert.IsFalse(Service.RuntimeState.IsSuspended);
        }

        [Test]
        public void AdClosedWhileTabHidden_StaysSuspendedAndDropsPlays()
        {
            InitializeWithMusic();
            PlayHeld(ShopLoopId);

            Service.HandlePaused(new PausedEvent(PauseReason.Hidden));
            Service.SetMuteReason(MuteReason.Ad, true);
            Service.SetMuteReason(MuteReason.Ad, false);

            Assert.IsTrue(Service.RuntimeState.IsSuspended);
            Clock.Advance(1d);
            AssertDropped(DropReason.Suspended, Service.Play(PegHitId, AudioPlayParams.None));
        }

        [Test]
        public void AdReason_NeverRaisesMuteChanged()
        {
            InitializeWithMusic();
            PlayHeld(ShopLoopId);

            Service.SetMuteReason(MuteReason.Ad, true);
            Service.SetMuteReason(MuteReason.Ad, false);

            Assert.AreEqual(0, MuteChangedCount);
            Assert.IsFalse(Service.IsUserMuted);
        }
    }
}
