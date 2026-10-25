using CozyGachapon.Contracts;
using NUnit.Framework;

namespace CozyGachapon.Tests.Audio
{
    public class UserMuteTests : AudioMuteFixture
    {
        private const double ToggleStepSeconds = 0.02d;
        private const double EarlySoundOnLeadSeconds = 0.05d;

        [Test]
        public void AC23_MuteThenUnmute_FadesMasterKeepsMusicRunningAndPlaysSoundOnOnce()
        {
            InitializeWithMusic();
            int music = PlayHeld(ShopLoopId);
            float baseVolume = Backend.GetVolume(music);

            Service.SetUserMuted(true);

            Assert.AreEqual(1, MuteChangedCount);
            Assert.IsTrue(LastMuteChanged);
            Assert.IsTrue(Service.IsUserMuted);
            AdvanceAndTick(UserMuteFadeSeconds + SettleMargin);
            Assert.AreEqual(0f, Backend.GetVolume(music), FadeTolerance);
            Clock.Advance(1d);
            AssertDropped(DropReason.UserMuted, Service.Play(PegHitId, AudioPlayParams.None));
            float position = Backend.GetPosition(music);
            Backend.AdvancePlayback(1f);
            Assert.Greater(Backend.GetPosition(music), position);
            int played = Service.Stats.Played;

            Service.SetUserMuted(false);

            Assert.AreEqual(2, MuteChangedCount);
            Assert.IsFalse(LastMuteChanged);
            Assert.IsFalse(Service.IsUserMuted);
            AdvanceAndTick(UserMuteFadeSeconds * 0.5f);
            Assert.AreEqual(baseVolume * 0.5f, Backend.GetVolume(music), FadeTolerance);
            Assert.AreEqual(played, Service.Stats.Played);
            AdvanceAndTick(UserMuteFadeSeconds * 0.5f + SettleMargin);
            Assert.AreEqual(baseVolume, Backend.GetVolume(music), FadeTolerance);
            Assert.AreEqual(played + 1, Service.Stats.Played);
            Assert.AreEqual(FirstClipOf(SoundOnId), Backend.GetClip(Backend.LastStartedVoice));
            AdvanceAndTick(1d);
            Assert.AreEqual(played + 1, Service.Stats.Played);
            Assert.AreEqual(2, MuteChangedCount);
        }

        [Test]
        public void SetUserMuted_SameValue_DoesNotRaiseOrWrite()
        {
            InitializeWithMusic();
            int writes = SettingsStore.UserMutedWriteCount;

            Service.SetUserMuted(false);
            Service.SetUserMuted(true);
            Service.SetUserMuted(true);

            Assert.AreEqual(1, MuteChangedCount);
            Assert.AreEqual(writes + 1, SettingsStore.UserMutedWriteCount);
        }

        [Test]
        public void UnmuteMidFade_ReversesFromCurrentLevel()
        {
            InitializeWithMusic();
            int music = PlayHeld(ShopLoopId);
            float baseVolume = Backend.GetVolume(music);

            Service.SetUserMuted(true);
            AdvanceAndTick(UserMuteFadeSeconds * 0.5f);
            Assert.AreEqual(baseVolume * 0.5f, Backend.GetVolume(music), FadeTolerance);

            Service.SetUserMuted(false);
            Service.Tick();

            Assert.AreEqual(baseVolume * 0.5f, Backend.GetVolume(music), FadeTolerance);
            AdvanceAndTick(UserMuteFadeSeconds + SettleMargin);
            Assert.AreEqual(baseVolume, Backend.GetVolume(music), FadeTolerance);
        }

        [Test]
        public void RapidToggling_RaisesEveryChange_AndPlaysSoundOnAtMostOnce()
        {
            InitializeWithMusic();
            PlayHeld(ShopLoopId);
            int played = Service.Stats.Played;

            for (int i = 0; i < 4; i++)
            {
                Service.SetUserMuted(i % 2 == 0);
                AdvanceAndTick(ToggleStepSeconds);
            }

            Assert.AreEqual(4, MuteChangedCount);
            Assert.AreEqual(played, Service.Stats.Played);
            AdvanceAndTick(UserMuteFadeSeconds + SettleMargin);
            AdvanceAndTick(1d);
            Assert.AreEqual(played + 1, Service.Stats.Played);
            Assert.IsFalse(Service.IsUserMuted);
        }

        [Test]
        public void SoundOn_IsRateLimitedByItsMinInterval()
        {
            InitializeWithMusic();
            PlayHeld(ShopLoopId);
            Service.SetUserMuted(true);
            AdvanceAndTick(UserMuteFadeSeconds + SettleMargin);

            Service.SetUserMuted(false);
            Clock.Advance(UserMuteFadeSeconds - EarlySoundOnLeadSeconds);
            AssertPlayed(Service.Play(SoundOnId, AudioPlayParams.None));
            int rateLimited = Service.Stats.DroppedRateLimited;
            AdvanceAndTick(EarlySoundOnLeadSeconds + 0.01d);

            Assert.AreEqual(rateLimited + 1, Service.Stats.DroppedRateLimited);
        }

        [Test]
        public void SoundOn_IsKeptWhileSuspended_AndPlaysAfterResume()
        {
            InitializeWithMusic();
            PlayHeld(ShopLoopId);
            Service.SetUserMuted(true);
            AdvanceAndTick(UserMuteFadeSeconds + SettleMargin);
            int played = Service.Stats.Played;

            Service.SetUserMuted(false);
            Service.HandlePaused(new PausedEvent(PauseReason.Hidden));
            AdvanceAndTick(UserMuteFadeSeconds + SettleMargin);
            Assert.AreEqual(played, Service.Stats.Played);

            Service.HandleResumed(new ResumedEvent(PauseReason.Hidden, 1d));
            AdvanceAndTick(SettleMargin);

            Assert.AreEqual(played + 1, Service.Stats.Played);
        }

        [Test]
        public void SetUserVolume_SameValue_DoesNotWriteAgain()
        {
            InitializeWithMusic();
            Service.SetUserVolume(AudioBus.Sfx, 0.5f);
            int writes = SettingsStore.SliderWriteCount;

            Service.SetUserVolume(AudioBus.Sfx, 0.5f);

            Assert.AreEqual(writes, SettingsStore.SliderWriteCount);
        }

        [Test]
        public void SetUserMuted_BeforeInitialize_DoesNotThrow()
        {
            Service.MuteChanged += RecordMuteChanged;

            Assert.DoesNotThrow(() => Service.SetUserMuted(true));
            Assert.IsFalse(Service.IsUserMuted);
            Assert.AreEqual(0, MuteChangedCount);
        }
    }
}
