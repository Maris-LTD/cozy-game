using CozyGachapon.Contracts;
using CozyGachapon.Pooling;
using NUnit.Framework;
using UnityEngine;

namespace CozyGachapon.Tests.Pooling
{
    public class PoolRecycleTests : PoolingTestFixture
    {
        [Test]
        public void AC17_RecycleTwice_ReturnsSeq1ThenSeq2_ReturnHookBeforeAcquire()
        {
            RecordingPoolHooks hooks = new RecordingPoolHooks();
            ObjectPoolService service = CreateReadyPolicyService(PoolExhaustionPolicy.RecycleOldest, hooks, 4);
            PoolLease[] leases = new PoolLease[4];
            for (int i = 0; i < 4; i++)
            {
                leases[i] = service.Get(PoolId(0));
            }

            int callsBefore = hooks.Calls.Count;
            PoolLease first = service.Get(PoolId(0));
            AssertCounts(service, 4, 4, 1);
            PoolLease second = service.Get(PoolId(0));
            AssertCounts(service, 4, 4, 2);

            Assert.AreSame(leases[0].Instance, first.Instance);
            Assert.AreSame(leases[1].Instance, second.Instance);
            Assert.AreEqual(5, first.Sequence);
            Assert.AreEqual(6, second.Sequence);
            Assert.AreEqual(callsBefore + 4, hooks.Calls.Count);
            AssertReturnThenAcquire(hooks, callsBefore, leases[0].Instance);
            AssertReturnThenAcquire(hooks, callsBefore + 2, leases[1].Instance);
            Assert.AreEqual(0, Sink.ErrorCount);
        }

        [Test]
        public void AC18_RecycleAfterMidRelease_VictimsAreSeq1_2_4()
        {
            ObjectPoolService service = CreateReadyPolicyService(PoolExhaustionPolicy.RecycleOldest, null, 4);
            PoolLease[] leases = new PoolLease[4];
            for (int i = 0; i < 4; i++)
            {
                leases[i] = service.Get(PoolId(0));
            }

            service.Release(leases[2]);
            PoolLease reacquired = service.Get(PoolId(0));
            Assert.AreSame(leases[2].Instance, reacquired.Instance);
            Assert.AreEqual(5, reacquired.Sequence);

            PoolLease v1 = service.Get(PoolId(0));
            PoolLease v2 = service.Get(PoolId(0));
            PoolLease v3 = service.Get(PoolId(0));

            Assert.AreSame(leases[0].Instance, v1.Instance);
            Assert.AreSame(leases[1].Instance, v2.Instance);
            Assert.AreSame(leases[3].Instance, v3.Instance);
            Assert.AreNotSame(leases[2].Instance, v1.Instance);
            Assert.AreNotSame(leases[2].Instance, v2.Instance);
            Assert.AreNotSame(leases[2].Instance, v3.Instance);
            AssertCounts(service, 4, 4, 3);
        }

        [Test]
        public void AC19_CapacityOne_ThreeGets_SameObject_IncreasingSeq()
        {
            ObjectPoolService service = CreateReadyPolicyService(PoolExhaustionPolicy.RecycleOldest, null, 1);
            PoolLease owner = service.Get(PoolId(0));
            PoolLease[] recycled = new PoolLease[3];

            Assert.DoesNotThrow(() =>
            {
                for (int i = 0; i < 3; i++)
                {
                    recycled[i] = service.Get(PoolId(0));
                }
            });

            long previous = owner.Sequence;
            for (int i = 0; i < 3; i++)
            {
                Assert.AreSame(owner.Instance, recycled[i].Instance);
                Assert.Greater(recycled[i].Sequence, previous);
                previous = recycled[i].Sequence;
            }

            AssertCounts(service, 1, 1, 3);
        }

        [Test]
        public void AC20_OldOwnerReleasesRecycledLease_IsIgnored_StillVisibleForNewOwner()
        {
            RecordingPoolHooks hooks = new RecordingPoolHooks();
            ObjectPoolService service = CreateReadyPolicyService(PoolExhaustionPolicy.RecycleOldest, hooks, 1);
            PoolLease old = service.Get(PoolId(0));
            PoolLease fresh = service.Get(PoolId(0));
            int returnsBefore = hooks.ReturnCount;
            int errorsBefore = Sink.ErrorCount;

            Assert.DoesNotThrow(() => service.Release(old));

            service.TryGetStats(PoolId(0), out PoolStats stats);
            Assert.AreEqual(1, stats.StaleRelease);
            Assert.AreEqual(1, stats.InUse);
            AssertVisible(fresh.Instance);
            Assert.AreEqual(returnsBefore, hooks.ReturnCount);
            Assert.AreEqual(errorsBefore + 1, Sink.ErrorCount);
            Assert.AreEqual(PoolDiagnosticCode.ReleaseStale, Sink.LastErrorCode);
        }

        [Test]
        public void AC20_Edge_NewOwnerReleaseSucceedsAfterStaleAttempt()
        {
            RecordingPoolHooks hooks = new RecordingPoolHooks();
            ObjectPoolService service = CreateReadyPolicyService(PoolExhaustionPolicy.RecycleOldest, hooks, 1);
            PoolLease old = service.Get(PoolId(0));
            PoolLease fresh = service.Get(PoolId(0));
            service.Release(old);
            int returnsBefore = hooks.ReturnCount;

            service.Release(fresh);

            service.TryGetStats(PoolId(0), out PoolStats stats);
            Assert.AreEqual(0, stats.InUse);
            Assert.AreEqual(returnsBefore + 1, hooks.ReturnCount);
            Assert.AreEqual(PoolReturnReason.Released, hooks.LastReason);
            AssertHidden(fresh.Instance);
        }

        [Test]
        public void Recycle_DoesNotReportWarningOrError_NorCountExhausted()
        {
            ObjectPoolService service = CreateReadyPolicyService(PoolExhaustionPolicy.RecycleOldest, null, 2);
            for (int i = 0; i < 6; i++)
            {
                service.Get(PoolId(0));
            }

            service.TryGetStats(PoolId(0), out PoolStats stats);
            Assert.AreEqual(0, Sink.WarningCount);
            Assert.AreEqual(0, Sink.ErrorCount);
            Assert.AreEqual(0, stats.Exhausted);
            Assert.AreEqual(4, stats.Recycled);
            Assert.AreEqual(2, stats.PeakInUse);
        }

        private void AssertCounts(ObjectPoolService service, int created, int inUse, int recycled)
        {
            service.TryGetStats(PoolId(0), out PoolStats stats);
            Assert.AreEqual(created, stats.Created);
            Assert.AreEqual(inUse, stats.InUse);
            Assert.AreEqual(recycled, stats.Recycled);
        }

        private static void AssertReturnThenAcquire(RecordingPoolHooks hooks, int index, GameObject instance)
        {
            Assert.IsTrue(hooks.Calls[index].IsReturn);
            Assert.AreEqual(PoolReturnReason.Recycled, hooks.Calls[index].Reason);
            Assert.AreSame(instance, hooks.Calls[index].Instance);
            Assert.IsFalse(hooks.Calls[index + 1].IsReturn);
            Assert.AreSame(instance, hooks.Calls[index + 1].Instance);
        }
    }
}
