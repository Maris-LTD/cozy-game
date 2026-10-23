using System;
using CozyGachapon.Contracts;
using CozyGachapon.Foundation;
using NUnit.Framework;

namespace CozyGachapon.Tests.Bootstrap
{
    public class LifecycleWatchdogTests
    {
        private sealed class MockTimeProvider : ITimeProvider
        {
            public DateTimeOffset CurrentTime { get; set; } = new DateTimeOffset(2026, 10, 22, 10, 0, 0, TimeSpan.Zero);
            public DateTimeOffset UtcNow => CurrentTime;
        }

        [Test]
        public void AC17_WindowFocusLoss_EmitsNoEvents_AndRunningMaintained()
        {
            AppLifecycleWatchdog watchdog = new AppLifecycleWatchdog();
            int pausedCount = 0;
            int resumedCount = 0;

            watchdog.OnPaused += _ => pausedCount++;
            watchdog.OnResumed += _ => resumedCount++;

            watchdog.NotifyBootCompleted();
            watchdog.NotifyInteractive();
            Assert.AreEqual(AppLifecycleState.Running, watchdog.State);

            watchdog.OnWindowFocusChanged(false);

            Assert.AreEqual(0, pausedCount);
            Assert.AreEqual(0, resumedCount);
            Assert.AreEqual(AppLifecycleState.Running, watchdog.State);
            Assert.IsTrue(watchdog.IsSimulationRunning);
        }

        [Test]
        public void AC17_PageHidden_FiresPausedOnce_AndIgnoresDuplicateHiddenSignals()
        {
            AppLifecycleWatchdog watchdog = new AppLifecycleWatchdog();
            int pausedCount = 0;
            PausedEvent? lastPaused = null;

            watchdog.OnPaused += evt =>
            {
                pausedCount++;
                lastPaused = evt;
            };

            watchdog.NotifyBootCompleted();
            watchdog.NotifyInteractive();

            watchdog.OnVisibilityChanged(false, PauseReason.Hidden);
            Assert.AreEqual(1, pausedCount);
            Assert.AreEqual(PauseReason.Hidden, lastPaused.Value.Reason);
            Assert.AreEqual(AppLifecycleState.Paused, watchdog.State);
            Assert.IsFalse(watchdog.IsSimulationRunning);

            watchdog.OnVisibilityChanged(false, PauseReason.Hidden);
            watchdog.OnVisibilityChanged(false, PauseReason.Background);
            Assert.AreEqual(1, pausedCount);
        }

        [Test]
        public void AC17_PageVisible_FiresResumedOnce_WithAwaySeconds()
        {
            MockTimeProvider timeProvider = new MockTimeProvider();
            AppLifecycleWatchdog watchdog = new AppLifecycleWatchdog(timeProvider);

            int resumedCount = 0;
            ResumedEvent? lastResumed = null;
            watchdog.OnResumed += evt =>
            {
                resumedCount++;
                lastResumed = evt;
            };

            watchdog.NotifyBootCompleted();
            watchdog.NotifyInteractive();

            watchdog.OnVisibilityChanged(false, PauseReason.Hidden);
            timeProvider.CurrentTime = timeProvider.CurrentTime.AddSeconds(45);
            watchdog.OnVisibilityChanged(true);

            Assert.AreEqual(1, resumedCount);
            Assert.AreEqual(45.0, lastResumed.Value.AwaySeconds, 0.001);
            Assert.AreEqual(PauseReason.Hidden, lastResumed.Value.Reason);
            Assert.AreEqual(AppLifecycleState.Running, watchdog.State);
            Assert.IsTrue(watchdog.IsSimulationRunning);
        }

        [Test]
        public void AC18_AwaySeconds_CalculatedFromUtcDifference_AndClampedToZeroOnClockRewind()
        {
            MockTimeProvider timeProvider = new MockTimeProvider();
            AppLifecycleWatchdog watchdog = new AppLifecycleWatchdog(timeProvider);

            ResumedEvent? lastResumed = null;
            watchdog.OnResumed += evt => lastResumed = evt;

            watchdog.NotifyBootCompleted();
            watchdog.NotifyInteractive();

            watchdog.OnVisibilityChanged(false, PauseReason.Background);
            timeProvider.CurrentTime = timeProvider.CurrentTime.AddSeconds(-60);
            watchdog.OnVisibilityChanged(true);

            Assert.IsNotNull(lastResumed);
            Assert.AreEqual(0.0, lastResumed.Value.AwaySeconds);
            Assert.AreEqual(PauseReason.Background, lastResumed.Value.Reason);
        }

        [Test]
        public void AC19_PageHiddenDuringBootSteps_DoesNotPauseUntilBootCompleted()
        {
            AppLifecycleWatchdog watchdog = new AppLifecycleWatchdog();
            int pausedCount = 0;
            watchdog.OnPaused += _ => pausedCount++;

            Assert.AreEqual(AppLifecycleState.BootingSteps, watchdog.State);

            watchdog.OnVisibilityChanged(false, PauseReason.Hidden);
            Assert.AreEqual(0, pausedCount);
            Assert.AreEqual(AppLifecycleState.BootingSteps, watchdog.State);

            watchdog.NotifyBootCompleted();
            Assert.AreEqual(1, pausedCount);
            Assert.AreEqual(AppLifecycleState.Paused, watchdog.State);
            Assert.AreEqual(AppLifecycleState.BootingCurtain, watchdog.PreviousState);
        }

        [Test]
        public void AC19_BackgroundDuringBootSteps_PreservesBackgroundReasonOnBootCompleted()
        {
            AppLifecycleWatchdog watchdog = new AppLifecycleWatchdog();
            PausedEvent? lastPaused = null;
            watchdog.OnPaused += evt => lastPaused = evt;

            watchdog.OnVisibilityChanged(false, PauseReason.Background);
            watchdog.NotifyBootCompleted();

            Assert.IsNotNull(lastPaused);
            Assert.AreEqual(PauseReason.Background, lastPaused.Value.Reason);
        }

        [Test]
        public void AC17_RedundantBootCompleted_IgnoredWhenAlreadyRunning()
        {
            AppLifecycleWatchdog watchdog = new AppLifecycleWatchdog();
            watchdog.NotifyBootCompleted();
            watchdog.NotifyInteractive();
            Assert.AreEqual(AppLifecycleState.Running, watchdog.State);

            watchdog.NotifyBootCompleted();
            Assert.AreEqual(AppLifecycleState.Running, watchdog.State);
            Assert.IsTrue(watchdog.IsSimulationRunning);
        }

        [Test]
        public void AC21_ApplicationQuit_FiresQuittingOnce_AndBlocksSubsequentLifecycleEvents()
        {
            AppLifecycleWatchdog watchdog = new AppLifecycleWatchdog();
            int quittingCount = 0;
            int pausedCount = 0;
            int resumedCount = 0;

            watchdog.OnQuitting += _ => quittingCount++;
            watchdog.OnPaused += _ => pausedCount++;
            watchdog.OnResumed += _ => resumedCount++;

            watchdog.NotifyBootCompleted();
            watchdog.NotifyInteractive();

            watchdog.OnApplicationQuit();
            Assert.AreEqual(1, quittingCount);
            Assert.AreEqual(AppLifecycleState.Quitting, watchdog.State);
            Assert.IsTrue(watchdog.IsQuitting);

            watchdog.OnApplicationQuit();
            watchdog.OnVisibilityChanged(false);
            watchdog.OnVisibilityChanged(true);

            Assert.AreEqual(1, quittingCount);
            Assert.AreEqual(0, pausedCount);
            Assert.AreEqual(0, resumedCount);
        }
    }
}
