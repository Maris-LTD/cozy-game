using CozyGachapon.Contracts;
using CozyGachapon.Pooling;
using NUnit.Framework;

namespace CozyGachapon.Tests.Pooling
{
    public class PoolSessionResetTests : PoolSessionTestBase
    {
        private void AssertOldSessionDestroyed()
        {
            Assert.IsTrue(OldModule.Session.IsTornDown);
            Assert.IsTrue(OldContainer == null);
            Assert.AreEqual(0, Root.childCount);
            for (int i = 0; i < OldObjects.Count; i++)
            {
                Assert.IsTrue(OldObjects[i] == null);
            }

            Assert.AreEqual(0, OldHooks.ReturnCount);
        }

        private static void AssertCountersZero(PoolStats stats)
        {
            Assert.AreEqual(0, stats.Created);
            Assert.AreEqual(0, stats.InUse);
            Assert.AreEqual(0, stats.PeakInUse);
            Assert.AreEqual(0, stats.Exhausted);
            Assert.AreEqual(0, stats.Recycled);
            Assert.AreEqual(0, stats.DoubleRelease);
            Assert.AreEqual(0, stats.StaleRelease);
            Assert.AreEqual(0, stats.ForeignRelease);
            Assert.AreEqual(0, stats.Lost);
        }

        [TestCase(OldSessionState.Accepting)]
        [TestCase(OldSessionState.Prewarming)]
        [TestCase(OldSessionState.Ready)]
        public void AC31_Teardown_DestroysContainerAndPooledObjects_WithoutReturnHooks(OldSessionState state)
        {
            PrepareOldSession(state);

            OldModule.Session.Teardown();
            OldModule.Tick();

            AssertOldSessionDestroyed();
        }

        [TestCase(OldSessionState.Accepting)]
        [TestCase(OldSessionState.Prewarming)]
        [TestCase(OldSessionState.Ready)]
        public void AC31_RunAfterTeardown_NewSessionIsAcceptingAndRegistersSameIdWithoutDuplicateError(OldSessionState state)
        {
            PrepareOldSession(state);
            OldModule.Session.Teardown();
            int errorsBefore = Sink.ErrorCount;

            PoolingBootModule fresh = NewSession();
            CountingPoolHooks freshHooks = new CountingPoolHooks();
            PoolRegistrationResult result = RegisterCoin(fresh, freshHooks);

            Assert.AreEqual(PoolServicePhase.Accepting, fresh.Service.Phase);
            Assert.AreNotSame(OldModule.Service, fresh.Service);
            Assert.IsTrue(result.Accepted);
            Assert.AreEqual(Capacity, result.Capacity);
            Assert.AreEqual(errorsBefore, Sink.ErrorCount);
            AssertCountersZero(Stats(fresh.Service));
        }

        [TestCase(OldSessionState.Accepting)]
        [TestCase(OldSessionState.Prewarming)]
        [TestCase(OldSessionState.Ready)]
        public void AC31_NewSession_PrewarmsFromScratch_AndFirstGetHasSequenceOne(OldSessionState state)
        {
            PrepareOldSession(state);
            OldModule.Session.Teardown();
            PoolingBootModule fresh = NewSession();
            CountingPoolHooks freshHooks = new CountingPoolHooks();
            RegisterCoin(fresh, freshHooks);

            PrewarmToReady(fresh);
            PoolStats prewarmed = Stats(fresh.Service);
            PoolLease first = fresh.Service.Get(CoinId);

            Assert.AreEqual(Capacity, prewarmed.Created);
            Assert.AreEqual(Capacity, prewarmed.Capacity);
            Assert.AreEqual(1L, first.Sequence);
            Assert.IsTrue(first.Instance.activeInHierarchy);
            Assert.AreEqual(1, freshHooks.AcquireCount);
            Assert.AreEqual(0, OldHooks.ReturnCount);
            Assert.AreEqual(0, freshHooks.ReturnCount);
            Assert.AreEqual(1, Root.childCount);
        }
    }
}
