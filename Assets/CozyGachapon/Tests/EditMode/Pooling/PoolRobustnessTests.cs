using System.Collections.Generic;
using CozyGachapon.Contracts;
using CozyGachapon.Pooling;
using NUnit.Framework;
using UnityEngine;

namespace CozyGachapon.Tests.Pooling
{
    public class PoolRobustnessTests : PoolingTestFixture
    {
        private const int Capacity = 4;

        private PoolingTestTargets Targets => Track(ScriptableObject.CreateInstance<PoolingTestTargets>());

        [Test]
        public void AC21_AfterPeakAtCapacity_GcTestCyclesGetRelease_CreatedStaysCapacity()
        {
            ObjectPoolService service = CreateReadyService(null, Capacity);
            string id = PoolId(0);
            List<GameObject> initial = new List<GameObject>();
            service.CopyFreeObjects(id, initial);
            PoolLease[] leases = new PoolLease[Capacity];
            BorrowAll(service, id, leases);
            service.TryGetStats(id, out PoolStats peak);
            Assert.AreEqual(Capacity, peak.PeakInUse);
            ReleaseAll(service, leases);

            int cycles = Targets.GcTestCycles;
            for (int c = 0; c < cycles; c++)
            {
                for (int g = 0; g < Capacity; g++)
                {
                    leases[g] = service.Get(id);
                    Assert.IsTrue(initial.Contains(leases[g].Instance));
                    AssertCreatedIsCapacity(service, id);
                }

                for (int g = 0; g < Capacity; g++)
                {
                    service.Release(leases[g]);
                    AssertCreatedIsCapacity(service, id);
                }
            }

            for (int i = 0; i < initial.Count; i++)
            {
                Assert.IsTrue(initial[i] != null);
            }

            Assert.AreEqual(0, Sink.ErrorCount);
        }

        [Test]
        public void AC21_Edge_EveryObjectCycledAtLeastGcTestCyclesTimes()
        {
            ObjectPoolService service = CreateReadyService(null, Capacity);
            string id = PoolId(0);
            List<GameObject> initial = new List<GameObject>();
            service.CopyFreeObjects(id, initial);
            Dictionary<GameObject, int> counts = new Dictionary<GameObject, int>();
            PoolLease[] leases = new PoolLease[Capacity];
            int cycles = Targets.GcTestCycles;
            for (int c = 0; c < cycles; c++)
            {
                BorrowAll(service, id, leases);
                for (int g = 0; g < Capacity; g++)
                {
                    counts.TryGetValue(leases[g].Instance, out int seen);
                    counts[leases[g].Instance] = seen + 1;
                }

                ReleaseAll(service, leases);
            }

            Assert.AreEqual(Capacity, counts.Count);
            for (int i = 0; i < initial.Count; i++)
            {
                Assert.GreaterOrEqual(counts[initial[i]], cycles);
            }
        }

        [Test]
        public void AC22_FixedPool_DestroyedFreeObject_NeverReturned_Lost1_OneError_LastGetExhausts()
        {
            ObjectPoolService service = CreateReadyService(null, Capacity);
            string id = PoolId(0);
            GameObject destroyed = DestroyFreeAt(service, id, 1);
            PoolLease[] leases = new PoolLease[Capacity];
            for (int i = 0; i < Capacity - 1; i++)
            {
                leases[i] = service.Get(id);
                Assert.IsTrue(leases[i].IsValid);
                Assert.IsFalse(ReferenceEquals(leases[i].Instance, destroyed));
                Assert.IsTrue(leases[i].Instance != null);
            }

            Assert.AreNotSame(leases[0].Instance, leases[1].Instance);
            Assert.AreNotSame(leases[1].Instance, leases[2].Instance);
            Assert.AreNotSame(leases[0].Instance, leases[2].Instance);
            PoolLease last = service.Get(id);

            Assert.IsFalse(last.IsValid);
            service.TryGetStats(id, out PoolStats stats);
            Assert.AreEqual(1, stats.Lost);
            Assert.AreEqual(Capacity, stats.Created);
            Assert.AreEqual(Capacity - 1, stats.InUse);
            Assert.AreEqual(1, stats.Exhausted);
            Assert.AreEqual(1, Sink.ErrorCount);
            Assert.AreEqual(PoolDiagnosticCode.PoolObjectLost, Sink.LastErrorCode);
            Assert.AreEqual(id, Sink.LastErrorPoolId);
            Assert.AreEqual(1, Sink.WarningCount);
            Assert.AreEqual(PoolDiagnosticCode.PoolExhausted, Sink.LastWarningCode);
        }

        [Test]
        public void AC22_RecycleOldestPool_DestroyedFreeObject_LastGetRecyclesOldest()
        {
            ObjectPoolService service = CreateReadyPolicyService(PoolExhaustionPolicy.RecycleOldest, null, Capacity);
            string id = PoolId(0);
            GameObject destroyed = DestroyFreeAt(service, id, 1);
            PoolLease[] leases = new PoolLease[Capacity];
            for (int i = 0; i < Capacity - 1; i++)
            {
                leases[i] = service.Get(id);
                Assert.IsTrue(leases[i].IsValid);
                Assert.IsFalse(ReferenceEquals(leases[i].Instance, destroyed));
            }

            PoolLease recycled = service.Get(id);

            Assert.IsTrue(recycled.IsValid);
            Assert.AreSame(leases[0].Instance, recycled.Instance);
            Assert.AreEqual(Capacity, recycled.Sequence);
            service.TryGetStats(id, out PoolStats stats);
            Assert.AreEqual(1, stats.Recycled);
            Assert.AreEqual(1, stats.Lost);
            Assert.AreEqual(Capacity, stats.Created);
            Assert.AreEqual(Capacity - 1, stats.InUse);
            Assert.AreEqual(1, Sink.ErrorCount);
            Assert.AreEqual(PoolDiagnosticCode.PoolObjectLost, Sink.LastErrorCode);
        }

        [Test]
        public void AC22_Edge_TwoDestroyed_Lost2_TwoErrors_CreatedUnchanged()
        {
            ObjectPoolService service = CreateReadyService(null, Capacity);
            string id = PoolId(0);
            List<GameObject> initial = new List<GameObject>();
            service.CopyFreeObjects(id, initial);
            Object.DestroyImmediate(initial[0]);
            Object.DestroyImmediate(initial[2]);

            PoolLease first = service.Get(id);
            PoolLease second = service.Get(id);
            PoolLease third = service.Get(id);

            Assert.IsTrue(first.IsValid);
            Assert.IsTrue(second.IsValid);
            Assert.IsFalse(third.IsValid);
            service.TryGetStats(id, out PoolStats stats);
            Assert.AreEqual(2, stats.Lost);
            Assert.AreEqual(Capacity, stats.Created);
            Assert.AreEqual(2, stats.InUse);
            Assert.AreEqual(2, Sink.ErrorCount);
        }

        [Test]
        public void AC22_Edge_LostObjectNeverReturnsAfterReleaseAndReget()
        {
            ObjectPoolService service = CreateReadyService(null, Capacity);
            string id = PoolId(0);
            GameObject destroyed = DestroyFreeAt(service, id, 2);
            PoolLease[] leases = new PoolLease[Capacity - 1];
            BorrowAll(service, id, leases);
            ReleaseAll(service, leases);
            List<GameObject> free = new List<GameObject>();
            service.CopyFreeObjects(id, free);
            Assert.AreEqual(Capacity - 1, free.Count);
            for (int i = 0; i < free.Count; i++)
            {
                Assert.IsFalse(ReferenceEquals(free[i], destroyed));
            }

            BorrowAll(service, id, leases);

            for (int i = 0; i < leases.Length; i++)
            {
                Assert.IsFalse(ReferenceEquals(leases[i].Instance, destroyed));
            }

            Assert.IsFalse(service.Get(id).IsValid);
            service.TryGetStats(id, out PoolStats stats);
            Assert.AreEqual(1, stats.Lost);
            Assert.AreEqual(1, Sink.ErrorCount);
        }

        [Test]
        public void AC22_Edge_LostCounterStartsAtZero()
        {
            ObjectPoolService service = CreateReadyService(null, Capacity);

            service.TryGetStats(PoolId(0), out PoolStats stats);

            Assert.AreEqual(0, stats.Lost);
            Assert.AreEqual(0, Sink.ErrorCount);
        }

        private static GameObject DestroyFreeAt(ObjectPoolService service, string id, int index)
        {
            List<GameObject> initial = new List<GameObject>();
            service.CopyFreeObjects(id, initial);
            GameObject victim = initial[index];
            Object.DestroyImmediate(victim);
            return victim;
        }

        private static void BorrowAll(ObjectPoolService service, string id, PoolLease[] leases)
        {
            for (int g = 0; g < leases.Length; g++)
            {
                leases[g] = service.Get(id);
                Assert.IsTrue(leases[g].IsValid);
            }
        }

        private static void ReleaseAll(ObjectPoolService service, PoolLease[] leases)
        {
            for (int g = 0; g < leases.Length; g++)
            {
                service.Release(leases[g]);
            }
        }

        private static void AssertCreatedIsCapacity(ObjectPoolService service, string id)
        {
            service.TryGetStats(id, out PoolStats stats);
            Assert.AreEqual(Capacity, stats.Created);
            Assert.AreEqual(Capacity, stats.Capacity);
        }
    }
}
