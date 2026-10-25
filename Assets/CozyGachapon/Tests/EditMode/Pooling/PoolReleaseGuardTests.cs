using CozyGachapon.Contracts;
using CozyGachapon.Pooling;
using NUnit.Framework;
using UnityEngine;

namespace CozyGachapon.Tests.Pooling
{
    public class PoolReleaseGuardTests : PoolingTestFixture
    {
        [Test]
        public void AC13_DoubleRelease_IncrementsCounter_ReportsOneError_InUseUnchanged()
        {
            RecordingPoolHooks hooks = new RecordingPoolHooks();
            ObjectPoolService service = CreateReadyService(hooks, 3);
            PoolLease lease = service.Get(PoolId(0));
            service.Release(lease);
            service.TryGetStats(PoolId(0), out PoolStats before);
            int errorsBefore = Sink.ErrorCount;

            Assert.DoesNotThrow(() => service.Release(lease));

            service.TryGetStats(PoolId(0), out PoolStats after);
            Assert.AreEqual(1, after.DoubleRelease);
            Assert.AreEqual(errorsBefore + 1, Sink.ErrorCount);
            Assert.AreEqual(PoolDiagnosticCode.ReleaseDouble, Sink.LastErrorCode);
            Assert.AreEqual(before.InUse, after.InUse);
            Assert.AreEqual(1, hooks.ReturnCount);
        }

        [Test]
        public void AC13_AfterDoubleRelease_TwoGetsReturnDifferentObjects()
        {
            ObjectPoolService service = CreateReadyService(null, 3);
            PoolLease lease = service.Get(PoolId(0));
            service.Release(lease);
            service.Release(lease);

            PoolLease first = service.Get(PoolId(0));
            PoolLease second = service.Get(PoolId(0));

            Assert.IsTrue(first.IsValid);
            Assert.IsTrue(second.IsValid);
            Assert.AreNotSame(first.Instance, second.Instance);
        }

        [Test]
        public void AC14_ForeignReleases_AreIgnored_ForeignReleaseCountsThree()
        {
            RecordingPoolHooks hooks = new RecordingPoolHooks();
            ObjectPoolService service = CreateReadyService(hooks, 2, 2);
            PoolLease otherPoolLease = service.Get(PoolId(1));
            GameObject stranger = Track(new GameObject("stranger"));
            PoolLease wrappedOther = new PoolLease(otherPoolLease.InstanceId, PoolId(0), otherPoolLease.Sequence, otherPoolLease.Instance);
            PoolLease wrappedStranger = new PoolLease(stranger.GetInstanceID(), PoolId(0), 1, stranger);

            Assert.DoesNotThrow(() =>
            {
                service.Release(PoolId(0), wrappedOther);
                service.Release(PoolId(0), wrappedStranger);
                service.Release(PoolId(0), default);
            });

            service.TryGetStats(PoolId(0), out PoolStats pool0);
            service.TryGetStats(PoolId(1), out PoolStats pool1);
            Assert.AreEqual(3, pool0.ForeignRelease);
            Assert.AreEqual(0, pool0.InUse);
            Assert.AreEqual(1, pool1.InUse);
            Assert.AreEqual(0, pool1.ForeignRelease);
            Assert.IsTrue(otherPoolLease.Instance.activeInHierarchy);
            Assert.IsTrue(stranger.activeInHierarchy);
            Assert.AreEqual(0, hooks.ReturnCount);
        }

        [Test]
        public void AC14_Edge_LeaseOfOtherPool_ReleasedIntoTestedPool_CountsForeign()
        {
            ObjectPoolService service = CreateReadyService(null, 2, 2);
            PoolLease lease = service.Get(PoolId(1));

            service.Release(PoolId(0), lease);

            service.TryGetStats(PoolId(0), out PoolStats pool0);
            service.TryGetStats(PoolId(1), out PoolStats pool1);
            Assert.AreEqual(1, pool0.ForeignRelease);
            Assert.AreEqual(1, pool1.InUse);
            Assert.AreEqual(0, pool1.ForeignRelease);
        }

        [Test]
        public void Release_StaleLeaseOfReusedObject_IsIgnored()
        {
            ObjectPoolService service = CreateReadyService(null, 1);
            PoolLease stale = service.Get(PoolId(0));
            service.Release(stale);
            PoolLease current = service.Get(PoolId(0));

            service.Release(stale);

            service.TryGetStats(PoolId(0), out PoolStats stats);
            Assert.AreEqual(1, stats.StaleRelease);
            Assert.AreEqual(1, stats.InUse);
            Assert.IsTrue(current.Instance.activeInHierarchy);
            Assert.AreEqual(PoolDiagnosticCode.ReleaseStale, Sink.LastErrorCode);
        }

        [Test]
        public void Release_SingleArgOverload_UsesLeasePoolId()
        {
            RecordingPoolHooks hooks = new RecordingPoolHooks();
            ObjectPoolService service = CreateReadyService(hooks, 2);
            PoolLease lease = service.Get(PoolId(0));

            service.Release(lease);

            service.TryGetStats(PoolId(0), out PoolStats stats);
            Assert.AreEqual(0, stats.InUse);
            Assert.AreEqual(1, hooks.ReturnCount);
            Assert.AreEqual(0, Sink.ErrorCount);
        }

        [Test]
        public void Release_SingleArgOverload_EmptyLease_ReportsForeignWithoutException()
        {
            ObjectPoolService service = CreateReadyService(new RecordingPoolHooks(), 2);

            Assert.DoesNotThrow(() => service.Release(default(PoolLease)));

            service.TryGetStats(PoolId(0), out PoolStats stats);
            Assert.AreEqual(0, stats.InUse);
            Assert.AreEqual(1, Sink.ErrorCount);
            Assert.AreEqual(PoolDiagnosticCode.ReleaseForeign, Sink.LastErrorCode);
        }
    }
}
