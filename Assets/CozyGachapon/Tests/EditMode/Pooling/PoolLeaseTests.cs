using CozyGachapon.Contracts;
using CozyGachapon.Pooling;
using NUnit.Framework;
using UnityEngine;

namespace CozyGachapon.Tests.Pooling
{
    public class PoolLeaseTests : PoolingTestFixture
    {
        [Test]
        public void AC11_Get_WhenReady_ActivatesObject_CallsAcquireHookOnce_IncrementsInUse()
        {
            RecordingPoolHooks hooks = new RecordingPoolHooks();
            ObjectPoolService service = CreateReadyService(hooks, 3);

            PoolLease lease = service.Get(PoolId(0));

            Assert.IsTrue(lease.IsValid);
            Assert.AreEqual(PoolId(0), lease.PoolId);
            AssertVisible(lease.Instance);
            Assert.AreEqual(1, hooks.AcquireCount);
            Assert.IsTrue(hooks.WasActiveOnAcquire);
            Assert.AreSame(lease.Instance, hooks.LastAcquired);
            service.TryGetStats(PoolId(0), out PoolStats stats);
            Assert.AreEqual(1, stats.InUse);
            Assert.GreaterOrEqual(stats.PeakInUse, stats.InUse);
            Assert.AreEqual(0, Sink.ErrorCount);
        }

        [Test]
        public void AC11_Edge_PeakInUseNeverDecreases_SequenceIncreases()
        {
            ObjectPoolService service = CreateReadyService(null, 4);

            PoolLease first = service.Get(PoolId(0));
            PoolLease second = service.Get(PoolId(0));
            PoolLease third = service.Get(PoolId(0));
            service.Release(first);
            service.Release(second);

            Assert.Greater(second.Sequence, first.Sequence);
            Assert.Greater(third.Sequence, second.Sequence);
            service.TryGetStats(PoolId(0), out PoolStats stats);
            Assert.AreEqual(1, stats.InUse);
            Assert.AreEqual(3, stats.PeakInUse);
        }

        [Test]
        public void AC12_Release_HidesObject_CallsReturnHookOnceWithReleased_DecrementsInUse()
        {
            RecordingPoolHooks hooks = new RecordingPoolHooks();
            ObjectPoolService service = CreateReadyService(hooks, 2);
            PoolLease lease = service.Get(PoolId(0));
            GameObject instance = lease.Instance;
            instance.name = "client-owned";
            instance.transform.localPosition = new Vector3(3f, 4f, 0f);
            SpriteRenderer sprite = instance.GetComponent<SpriteRenderer>();
            sprite.color = Color.red;

            service.Release(lease);

            AssertHidden(instance);
            Assert.AreEqual(1, hooks.ReturnCount);
            Assert.AreEqual(PoolReturnReason.Released, hooks.LastReason);
            Assert.AreSame(instance, hooks.LastReturned);
            service.TryGetStats(PoolId(0), out PoolStats stats);
            Assert.AreEqual(0, stats.InUse);
            Assert.AreEqual("client-owned", instance.name);
            Assert.AreEqual(new Vector3(3f, 4f, 0f), instance.transform.localPosition);
            Assert.AreEqual(Color.red, sprite.color);
            Assert.AreEqual(0, Sink.ErrorCount);
        }

        [Test]
        public void AC12_Edge_GetAfterRelease_CanReturnSameObjectWithNewSequence()
        {
            ObjectPoolService service = CreateReadyService(null, 1);
            PoolLease first = service.Get(PoolId(0));
            service.Release(first);

            PoolLease second = service.Get(PoolId(0));

            Assert.IsTrue(second.IsValid);
            Assert.AreSame(first.Instance, second.Instance);
            Assert.Greater(second.Sequence, first.Sequence);
        }

        [Test]
        public void AC15_Get_WhenAccepting_ReturnsEmpty_ReportsOneError_CreatedUnchanged()
        {
            RecordingPoolHooks hooks = new RecordingPoolHooks();
            ObjectPoolService service = CreateRegisteredService(3);

            PoolLease lease = service.Get(PoolId(0));

            Assert.IsFalse(lease.IsValid);
            Assert.AreEqual(1, Sink.ErrorCount);
            Assert.AreEqual(PoolDiagnosticCode.GetWrongPhase, Sink.LastErrorCode);
            service.TryGetStats(PoolId(0), out PoolStats stats);
            Assert.AreEqual(0, stats.Created);
            Assert.AreEqual(0, hooks.AcquireCount);
        }

        [Test]
        public void AC15_Get_WhenPrewarming_ReturnsEmpty_ReportsOneError_CreatedUnchanged()
        {
            Config.SetPrewarmForTest(1, 0.25f);
            ObjectPoolService service = CreateRegisteredService(4);
            service.BeginPrewarm(CreateRoot());
            service.StepPrewarm();
            Assert.AreEqual(PoolServicePhase.Prewarming, service.Phase);
            service.TryGetStats(PoolId(0), out PoolStats before);

            PoolLease lease = service.Get(PoolId(0));

            Assert.IsFalse(lease.IsValid);
            Assert.AreEqual(1, Sink.ErrorCount);
            Assert.AreEqual(PoolDiagnosticCode.GetWrongPhase, Sink.LastErrorCode);
            service.TryGetStats(PoolId(0), out PoolStats after);
            Assert.AreEqual(before.Created, after.Created);
        }

        [Test]
        public void AC15_Edge_GetUnknownPool_WhenReady_ReturnsEmptyWithoutException()
        {
            ObjectPoolService service = CreateReadyService(null, 2);
            PoolLease missing = default;
            PoolLease nullId = default;

            Assert.DoesNotThrow(() => missing = service.Get("missing"));
            Assert.DoesNotThrow(() => nullId = service.Get(null));

            Assert.IsFalse(missing.IsValid);
            Assert.IsFalse(nullId.IsValid);
            Assert.AreEqual(2, Sink.ErrorCount);
            Assert.AreEqual(PoolDiagnosticCode.GetUnknownPool, Sink.LastErrorCode);
        }
    }
}
