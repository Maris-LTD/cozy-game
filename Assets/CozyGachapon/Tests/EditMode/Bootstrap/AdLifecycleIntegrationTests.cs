using System;
using System.Collections.Generic;
using CozyGachapon.Contracts;
using CozyGachapon.Foundation;
using NUnit.Framework;

namespace CozyGachapon.Tests.Bootstrap
{
    public class AdLifecycleIntegrationTests
    {
        private sealed class FakeTimeProvider : ITimeProvider
        {
            public DateTimeOffset CurrentTime { get; set; } = new DateTimeOffset(2026, 10, 23, 12, 0, 0, TimeSpan.Zero);
            public DateTimeOffset UtcNow => CurrentTime;
        }

        [Test]
        public void AC42_SetAdOpen_WhenRunning_SetsIsAdOpenTrue_AndMaintainsRunning()
        {
            AppLifecycleWatchdog watchdog = new AppLifecycleWatchdog();
            watchdog.NotifyBootCompleted();
            watchdog.NotifyInteractive();

            Assert.AreEqual(AppLifecycleState.Running, watchdog.State);
            Assert.IsFalse(watchdog.IsAdOpen);

            watchdog.SetAdOpen(true);

            Assert.IsTrue(watchdog.IsAdOpen);
            Assert.AreEqual(AppLifecycleState.Running, watchdog.State);
            Assert.IsTrue(watchdog.IsSimulationRunning);
        }

        [Test]
        public void AC42_VisibilityChangesDuringAd_DoNotEmitPausedOrResumed_AndSimulationKeepsRunning()
        {
            AppLifecycleWatchdog watchdog = new AppLifecycleWatchdog();
            int pausedCount = 0;
            int resumedCount = 0;

            watchdog.OnPaused += _ => pausedCount++;
            watchdog.OnResumed += _ => resumedCount++;

            watchdog.NotifyBootCompleted();
            watchdog.NotifyInteractive();

            watchdog.SetAdOpen(true);
            watchdog.OnVisibilityChanged(false, PauseReason.Hidden);

            Assert.AreEqual(0, pausedCount);
            Assert.AreEqual(0, resumedCount);
            Assert.AreEqual(AppLifecycleState.Running, watchdog.State);
            Assert.IsTrue(watchdog.IsSimulationRunning);

            watchdog.OnVisibilityChanged(true);

            Assert.AreEqual(0, pausedCount);
            Assert.AreEqual(0, resumedCount);
            Assert.AreEqual(AppLifecycleState.Running, watchdog.State);
            Assert.IsTrue(watchdog.IsSimulationRunning);
        }

        [Test]
        public void AC42_SetAdOpenFalse_WhenPageStillHidden_ImmediatelyTriggersPausedWithHiddenReason()
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

            watchdog.SetAdOpen(true);
            watchdog.OnVisibilityChanged(false, PauseReason.Hidden);

            Assert.AreEqual(0, pausedCount);

            watchdog.SetAdOpen(false);

            Assert.AreEqual(1, pausedCount);
            Assert.IsNotNull(lastPaused);
            Assert.AreEqual(PauseReason.Hidden, lastPaused.Value.Reason);
            Assert.AreEqual(AppLifecycleState.Paused, watchdog.State);
            Assert.IsFalse(watchdog.IsSimulationRunning);
            Assert.IsFalse(watchdog.IsAdOpen);
        }

        [Test]
        public void AC43_SetAdOpenFalse_EmitsAdClosedWithRealUtcDuration()
        {
            FakeTimeProvider timeProvider = new FakeTimeProvider();
            AppLifecycleWatchdog watchdog = new AppLifecycleWatchdog(timeProvider);

            int adClosedCount = 0;
            AdClosedEvent? lastAdClosed = null;

            watchdog.OnAdClosed += evt =>
            {
                adClosedCount++;
                lastAdClosed = evt;
            };

            watchdog.NotifyBootCompleted();
            watchdog.NotifyInteractive();

            watchdog.SetAdOpen(true);
            timeProvider.CurrentTime = timeProvider.CurrentTime.AddSeconds(32.5);
            watchdog.SetAdOpen(false);

            Assert.AreEqual(1, adClosedCount);
            Assert.IsNotNull(lastAdClosed);
            Assert.AreEqual(32.5, lastAdClosed.Value.AdSeconds, 0.001);
        }

        [Test]
        public void AC43_SetAdOpenFalse_ClampsDurationToZeroOnClockRewind()
        {
            FakeTimeProvider timeProvider = new FakeTimeProvider();
            AppLifecycleWatchdog watchdog = new AppLifecycleWatchdog(timeProvider);

            AdClosedEvent? lastAdClosed = null;
            watchdog.OnAdClosed += evt => lastAdClosed = evt;

            watchdog.NotifyBootCompleted();
            watchdog.NotifyInteractive();

            watchdog.SetAdOpen(true);
            timeProvider.CurrentTime = timeProvider.CurrentTime.AddSeconds(-15.0);
            watchdog.SetAdOpen(false);

            Assert.IsNotNull(lastAdClosed);
            Assert.AreEqual(0.0, lastAdClosed.Value.AdSeconds);
        }

        [Test]
        public void AC43_AdClosedEmittedBeforePaused_WhenPageIsHiddenAtCloseTime()
        {
            AppLifecycleWatchdog watchdog = new AppLifecycleWatchdog();
            List<string> eventOrder = new List<string>();

            watchdog.OnAdClosed += _ => eventOrder.Add("AdClosed");
            watchdog.OnPaused += _ => eventOrder.Add("Paused");

            watchdog.NotifyBootCompleted();
            watchdog.NotifyInteractive();

            watchdog.SetAdOpen(true);
            watchdog.OnVisibilityChanged(false, PauseReason.Hidden);
            watchdog.SetAdOpen(false);

            Assert.AreEqual(2, eventOrder.Count);
            Assert.AreEqual("AdClosed", eventOrder[0]);
            Assert.AreEqual("Paused", eventOrder[1]);
        }

        [Test]
        public void AC43_RedundantSetAdOpenCalls_DoNotResetAdTimerOrEmitExtraEvents()
        {
            FakeTimeProvider timeProvider = new FakeTimeProvider();
            AppLifecycleWatchdog watchdog = new AppLifecycleWatchdog(timeProvider);

            int adClosedCount = 0;
            AdClosedEvent? lastAdClosed = null;
            watchdog.OnAdClosed += evt =>
            {
                adClosedCount++;
                lastAdClosed = evt;
            };

            watchdog.NotifyBootCompleted();
            watchdog.NotifyInteractive();

            watchdog.SetAdOpen(false);
            Assert.AreEqual(0, adClosedCount);

            watchdog.SetAdOpen(true);
            timeProvider.CurrentTime = timeProvider.CurrentTime.AddSeconds(10.0);
            watchdog.SetAdOpen(true);
            timeProvider.CurrentTime = timeProvider.CurrentTime.AddSeconds(15.0);
            watchdog.SetAdOpen(false);

            Assert.AreEqual(1, adClosedCount);
            Assert.IsNotNull(lastAdClosed);
            Assert.AreEqual(25.0, lastAdClosed.Value.AdSeconds, 0.001);

            watchdog.SetAdOpen(false);
            Assert.AreEqual(1, adClosedCount);
        }

        [Test]
        public void AC42_ResumedAfterHiddenAdClose_MeasuresAwaySecondsFromPausedInstant()
        {
            FakeTimeProvider timeProvider = new FakeTimeProvider();
            AppLifecycleWatchdog watchdog = new AppLifecycleWatchdog(timeProvider);

            ResumedEvent? lastResumed = null;
            watchdog.OnResumed += evt => lastResumed = evt;

            watchdog.NotifyBootCompleted();
            watchdog.NotifyInteractive();

            watchdog.SetAdOpen(true);
            timeProvider.CurrentTime = timeProvider.CurrentTime.AddSeconds(20.0);
            watchdog.OnVisibilityChanged(false, PauseReason.Hidden);
            timeProvider.CurrentTime = timeProvider.CurrentTime.AddSeconds(10.0);

            watchdog.SetAdOpen(false);
            Assert.AreEqual(AppLifecycleState.Paused, watchdog.State);

            timeProvider.CurrentTime = timeProvider.CurrentTime.AddSeconds(12.0);
            watchdog.OnVisibilityChanged(true);

            Assert.IsNotNull(lastResumed);
            Assert.AreEqual(12.0, lastResumed.Value.AwaySeconds, 0.001);
            Assert.AreEqual(PauseReason.Hidden, lastResumed.Value.Reason);
            Assert.AreEqual(AppLifecycleState.Running, watchdog.State);
            Assert.IsTrue(watchdog.IsSimulationRunning);
        }
    }
}
