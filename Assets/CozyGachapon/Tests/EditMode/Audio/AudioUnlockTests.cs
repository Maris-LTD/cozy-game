using CozyGachapon.Audio;
using CozyGachapon.Contracts;
using NUnit.Framework;

namespace CozyGachapon.Tests.Audio
{
    public class AudioUnlockTests : AudioBootFixture
    {
        private const string CoinSpawnId = "Plinko.CoinSpawn";
        private const double PlayStepSeconds = 1d;

        private FakeVoiceBackend _backend;

        private AudioBootModule CreateUnlockModule(bool requiresUnlock)
        {
            AudioConfig config = LoadDefaultConfig();
            _backend = new FakeVoiceBackend(config.TotalVoices);
            return CreateModule(config, _backend, requiresUnlock);
        }

        private PlayResult PlayCoin(AudioBootModule module)
        {
            Clock.Advance(PlayStepSeconds);
            return module.Service.Play(CoinSpawnId, AudioPlayParams.None);
        }

        private static void AssertDropped(DropReason expected, PlayResult result)
        {
            Assert.IsFalse(result.Played);
            Assert.AreEqual(expected, result.Reason);
        }

        private static void AssertPlayed(PlayResult result)
        {
            Assert.IsTrue(result.Played, result.Reason.ToString());
        }

        [Test]
        public void AC24_RequiresUnlock_PlayBeforeFirstInteraction_Locked_AfterPlayed()
        {
            AudioBootModule module = CreateUnlockModule(true);
            module.ServiceStep.Run();

            Assert.IsTrue(module.Service.RuntimeState.IsLocked);
            Assert.IsFalse(module.Unlock.IsUnlocked);
            AssertDropped(DropReason.Locked, PlayCoin(module));
            Assert.AreEqual(0, _backend.StartCount);

            module.HandleFirstInteraction(new FirstInteractionEvent());

            Assert.IsFalse(module.Service.RuntimeState.IsLocked);
            Assert.IsTrue(module.Unlock.IsUnlocked);
            AssertPlayed(PlayCoin(module));
            Assert.AreEqual(1, _backend.StartCount);
            Assert.AreEqual(1, module.Service.Stats.DroppedLocked);
        }

        [Test]
        public void AC24_BackgroundWhileLocked_AfterFirstInteraction_Suspended()
        {
            AudioBootModule module = CreateUnlockModule(true);
            module.ServiceStep.Run();

            module.HandlePaused(new PausedEvent(PauseReason.Hidden));
            Assert.IsTrue(module.Service.RuntimeState.IsLocked);
            AssertDropped(DropReason.Locked, PlayCoin(module));
            module.HandleFirstInteraction(new FirstInteractionEvent());

            Assert.IsFalse(module.Service.RuntimeState.IsLocked);
            Assert.IsTrue(module.Service.RuntimeState.IsSuspended);
            AssertDropped(DropReason.Suspended, PlayCoin(module));
        }

        [Test]
        public void AC24_AdWhileLocked_AfterFirstInteraction_Suspended()
        {
            AudioBootModule module = CreateUnlockModule(true);
            module.ServiceStep.Run();

            module.Service.SetMuteReason(MuteReason.Ad, true);
            module.HandleFirstInteraction(new FirstInteractionEvent());

            Assert.IsFalse(module.Service.RuntimeState.IsLocked);
            Assert.IsTrue(module.Service.RuntimeState.IsSuspended);
            AssertDropped(DropReason.Suspended, PlayCoin(module));
        }

        [Test]
        public void AC24_UserMutedStored_AfterFirstInteraction_UserMuted()
        {
            Store.WriteUserMuted(true);
            AudioBootModule module = CreateUnlockModule(true);
            module.ServiceStep.Run();

            AssertDropped(DropReason.Locked, PlayCoin(module));
            module.HandleFirstInteraction(new FirstInteractionEvent());

            Assert.IsFalse(module.Service.RuntimeState.IsLocked);
            Assert.IsTrue(module.Service.IsUserMuted);
            AssertDropped(DropReason.UserMuted, PlayCoin(module));
        }

        [Test]
        public void AC24_NoUnlockRequired_ActiveRightAfterRun()
        {
            AudioBootModule module = CreateUnlockModule(false);
            module.ServiceStep.Run();

            Assert.AreEqual(AudioLifecycle.Active, module.Service.RuntimeState.Lifecycle);
            Assert.IsFalse(module.Service.RuntimeState.IsLocked);
            Assert.IsTrue(module.Unlock.IsUnlocked);
            AssertPlayed(PlayCoin(module));
        }

        [Test]
        public void AC24_SecondFirstInteraction_ChangesNothing()
        {
            AudioBootModule module = CreateUnlockModule(true);
            module.ServiceStep.Run();
            module.HandleFirstInteraction(new FirstInteractionEvent());
            AssertPlayed(PlayCoin(module));
            _backend.FinishVoice(_backend.LastStartedVoice);
            module.Service.Tick();
            AudioStats before = module.Service.Stats;

            module.HandleFirstInteraction(new FirstInteractionEvent());
            module.ServiceStep.Run();

            AudioStats after = module.Service.Stats;
            Assert.IsFalse(module.Service.RuntimeState.IsLocked);
            Assert.AreEqual(AudioLifecycle.Active, module.Service.RuntimeState.Lifecycle);
            Assert.AreEqual(before.Played, after.Played);
            Assert.AreEqual(before.DroppedTotal, after.DroppedTotal);
            AssertPlayed(PlayCoin(module));
        }

        [Test]
        public void AC24_SecondFirstInteraction_ReturnsFalseFromController()
        {
            AudioBootModule module = CreateUnlockModule(true);
            module.ServiceStep.Run();

            Assert.IsTrue(module.Unlock.HandleFirstInteraction());
            Assert.IsFalse(module.Unlock.HandleFirstInteraction());
        }

        [Test]
        public void AC42_FirstInteractionBeforeRun_ActiveAfterIsDone_NoReplay()
        {
            AudioBootModule module = CreateUnlockModule(true);

            AssertNotReady(module.Service.Play(CoinSpawnId, AudioPlayParams.None));
            module.HandleFirstInteraction(new FirstInteractionEvent());
            module.ServiceStep.Run();

            Assert.IsTrue(module.ServiceStep.IsDone);
            Assert.AreEqual(AudioLifecycle.Active, module.Service.RuntimeState.Lifecycle);
            Assert.IsFalse(module.Service.RuntimeState.IsLocked);
            Assert.AreEqual(0, _backend.StartCount);
            AssertPlayed(PlayCoin(module));
            Assert.AreEqual(1, _backend.StartCount);
        }

        [Test]
        public void AC42_BackgroundBeforeRun_SuspendedAfterRun()
        {
            AudioBootModule module = CreateUnlockModule(true);

            module.HandlePaused(new PausedEvent(PauseReason.Hidden));
            module.HandleFirstInteraction(new FirstInteractionEvent());
            module.ServiceStep.Run();

            Assert.IsTrue(module.ServiceStep.IsDone);
            Assert.IsFalse(module.Service.RuntimeState.IsLocked);
            Assert.IsTrue(module.Service.RuntimeState.IsSuspended);
            AssertDropped(DropReason.Suspended, PlayCoin(module));

            module.HandleResumed(new ResumedEvent(PauseReason.Hidden, 1d));

            AssertPlayed(PlayCoin(module));
        }

        [Test]
        public void AC42_BackgroundBeforeRun_WithoutFirstInteraction_LockedThenSuspended()
        {
            AudioBootModule module = CreateUnlockModule(true);

            module.HandlePaused(new PausedEvent(PauseReason.Hidden));
            module.ServiceStep.Run();

            Assert.IsTrue(module.Service.RuntimeState.IsLocked);
            module.HandleFirstInteraction(new FirstInteractionEvent());
            Assert.IsTrue(module.Service.RuntimeState.IsSuspended);
            AssertDropped(DropReason.Suspended, PlayCoin(module));
        }

        [Test]
        public void FirstInteraction_OnFailedService_DoesNotThrow()
        {
            AudioBootModule module = CreateMissingConfigModule();
            module.ServiceStep.Run();

            Assert.DoesNotThrow(() => module.HandleFirstInteraction(new FirstInteractionEvent()));
            Assert.DoesNotThrow(() => module.HandleInteractive(new InteractiveEvent(1f)));
            Assert.AreEqual(AudioLifecycle.Failed, module.Service.RuntimeState.Lifecycle);
            Assert.IsFalse(module.Service.RuntimeState.IsLocked);
        }
    }
}
