using CozyGachapon.Audio;
using CozyGachapon.Contracts;
using NUnit.Framework;

namespace CozyGachapon.Tests.Audio
{
    public class AudioFailedStateTests : AudioBootFixture
    {
        private int _muteChangedCount;
        private bool _lastMuteChanged;

        [SetUp]
        public void ResetMuteCounters()
        {
            _muteChangedCount = 0;
            _lastMuteChanged = false;
        }

        private AudioBootModule CreateFailedModule()
        {
            AudioBootModule module = CreateMissingConfigModule();
            module.Service.MuteChanged += OnMuteChanged;
            module.ServiceStep.Run();
            Assert.AreEqual(AudioLifecycle.Failed, module.Service.RuntimeState.Lifecycle);
            return module;
        }

        private void OnMuteChanged(bool muted)
        {
            _muteChangedCount++;
            _lastMuteChanged = muted;
        }

        [Test]
        public void AC32_MissingConfig_PlayDropsNotReady_SetUserMutedPersists()
        {
            AudioBootModule module = CreateFailedModule();

            AssertNotReady(module.Service.Play("Ui.Tap", AudioPlayParams.None));
            module.Service.SetUserMuted(true);

            Assert.IsTrue(module.Service.IsUserMuted);
            Assert.IsTrue(Store.StoredUserMuted);
            Assert.AreEqual(1, Store.UserMutedWriteCount);
            Assert.AreEqual(1, _muteChangedCount);
            Assert.IsTrue(_lastMuteChanged);
        }

        [Test]
        public void AC32_NewSessionOnSameStore_ReadsMutedTrue_WhenConfigMissing()
        {
            AudioBootModule first = CreateFailedModule();
            first.Service.SetUserMuted(true);

            AudioBootModule second = CreateMissingConfigModule();
            second.ServiceStep.Run();

            Assert.AreEqual(AudioLifecycle.Failed, second.Service.RuntimeState.Lifecycle);
            Assert.IsTrue(second.Service.IsUserMuted);
        }

        [Test]
        public void AC32_NewSessionOnSameStore_ReadsMutedTrue_WhenConfigValid()
        {
            AudioBootModule first = CreateFailedModule();
            first.Service.SetUserMuted(true);

            AudioBootModule second = CreateValidModule();
            second.ServiceStep.Run();

            Assert.AreEqual(AudioLifecycle.Active, second.Service.RuntimeState.Lifecycle);
            Assert.IsTrue(second.Service.IsUserMuted);
        }

        [Test]
        public void AC32_Failed_EmptyStore_DefaultsUnmuted()
        {
            AudioBootModule module = CreateFailedModule();

            Assert.IsFalse(module.Service.IsUserMuted);
            Assert.AreEqual(0, Store.UserMutedWriteCount);
        }

        [Test]
        public void AC32_Failed_UnmuteAfterMute_PersistsFalse()
        {
            AudioBootModule module = CreateFailedModule();

            module.Service.SetUserMuted(true);
            module.Service.SetUserMuted(false);

            Assert.IsFalse(module.Service.IsUserMuted);
            Assert.IsFalse(Store.StoredUserMuted);
            Assert.AreEqual(2, Store.UserMutedWriteCount);
            Assert.AreEqual(2, _muteChangedCount);
        }

        [Test]
        public void AC32_Failed_SetUserMutedSameValue_DoesNotWriteOrNotify()
        {
            AudioBootModule module = CreateFailedModule();

            module.Service.SetUserMuted(false);
            module.Service.SetUserMuted(true);
            module.Service.SetUserMuted(true);

            Assert.AreEqual(1, Store.UserMutedWriteCount);
            Assert.AreEqual(1, _muteChangedCount);
        }

        [Test]
        public void AC32_Failed_NullStore_StillTracksUserMuted()
        {
            AudioBootModule module = new AudioBootModule(
                null, CreateDefaultRepository(), new FakeVoiceBackend(1), Clock, Sink, new SystemAudioRandom(1), null);
            module.ServiceStep.Run();

            module.Service.SetUserMuted(true);

            Assert.IsTrue(module.ServiceStep.IsFailed);
            Assert.IsTrue(module.Service.IsUserMuted);
        }

        [Test]
        public void AC32_Failed_OtherReasonsAreIgnored()
        {
            AudioBootModule module = CreateFailedModule();

            module.Service.SetMuteReason(MuteReason.Ad, true);
            module.Service.SetMuteReason(MuteReason.Background, true);

            Assert.IsFalse(module.Service.IsUserMuted);
            Assert.AreEqual(0, _muteChangedCount);
            Assert.AreEqual(0, Store.UserMutedWriteCount);
        }

        [Test]
        public void AC32_Failed_TickAndOtherApis_DoNotThrow()
        {
            AudioBootModule module = CreateFailedModule();

            Assert.DoesNotThrow(() =>
            {
                module.Service.Tick();
                module.HandlePaused(new PausedEvent(PauseReason.Hidden));
                module.HandleResumed(new ResumedEvent(PauseReason.Hidden, 1d));
                module.Service.SetBusBackground(AudioBus.Sfx, true);
                module.Service.SetUserVolume(AudioBus.Sfx, 0.5f);
                module.Service.TryResolveCue("Ui.Tap", out int cueIndex);
            });

            Assert.IsFalse(module.Service.IsUserMuted);
            Assert.AreEqual(0f, module.Service.GetUserVolume(AudioBus.Sfx));
        }
    }
}
