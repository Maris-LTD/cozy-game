using System;
using CozyGachapon.Contracts;
using CozyGachapon.Pooling;
using NUnit.Framework;
using UnityEngine;

namespace CozyGachapon.Tests.Pooling
{
    public class PoolZeroAllocTests : PoolingTestFixture
    {
        private const int PoolCapacity = 2;
        private const int ControlBufferLength = 16;

        private ObjectPoolService _service;
        private string _fixedId;
        private string _recycleId;
        private PoolLease[] _fixedLeases;
        private PoolLease _recycleLease;
        private int _counterTotal;
        private object[] _controlBuffer;
        private int _cycles;
        private Action _measuredLoop;
        private Action _controlLoop;

        [SetUp]
        public void SetUpProbe()
        {
            _measuredLoop = MeasuredLoop;
            _controlLoop = ControlLoop;
            _fixedLeases = new PoolLease[PoolCapacity];
            _cycles = Track(ScriptableObject.CreateInstance<PoolingTestTargets>()).GcTestCycles;
            _fixedId = PoolId(0);
            _recycleId = PoolId(1);
        }

        [Test]
        public void AC24_ReadyMixedPools_GcTestCycles_ZeroGcAlloc()
        {
            PrepareReadyPools(null);
            RunCycle();

            int allocations = GcAllocProbe.CountAllocations(_measuredLoop);

            Assert.AreEqual(0, allocations);
            _service.TryGetStats(_fixedId, out PoolStats fixedStats);
            _service.TryGetStats(_recycleId, out PoolStats recycleStats);
            Assert.GreaterOrEqual(recycleStats.Recycled, _cycles);
            Assert.GreaterOrEqual(fixedStats.Exhausted, _cycles);
            Assert.AreEqual(0, Sink.ErrorCount);
            Assert.AreEqual(1, Sink.WarningCount);
            Assert.AreNotEqual(0, _counterTotal);
        }

        [Test]
        public void AC24_WithNonAllocatingHooks_GcTestCycles_ZeroGcAlloc()
        {
            CountingPoolHooks hooks = new CountingPoolHooks();
            PrepareReadyPools(hooks);
            RunCycle();
            int acquiresBefore = hooks.AcquireCount;

            int allocations = GcAllocProbe.CountAllocations(_measuredLoop);

            Assert.AreEqual(0, allocations);
            Assert.Greater(hooks.AcquireCount, acquiresBefore);
            Assert.Greater(hooks.ReturnCount, 0);
        }

        [Test]
        public void AC24_Control_DeliberateAllocationInsideMeasuredBlock_ReportsAllocations()
        {
            PrepareReadyPools(null);
            RunCycle();

            int allocations = GcAllocProbe.CountAllocations(_controlLoop);

            Assert.Greater(allocations, 0);
            Assert.IsNotNull(_controlBuffer);
        }

        [Test]
        public void AC24_Control_AllocatingHookOnPoolPath_ReportsAllocations()
        {
            AllocatingPoolHooks hooks = new AllocatingPoolHooks();
            PrepareReadyPools(hooks);
            RunCycle();

            int allocations = GcAllocProbe.CountAllocations(_measuredLoop);

            Assert.Greater(allocations, 0);
            Assert.IsNotNull(hooks.Last);
        }

        private void PrepareReadyPools(IPoolHooks hooks)
        {
            _service = CreateReadyMixedService(hooks, PoolCapacity, PoolCapacity);
            for (int i = 0; i < PoolCapacity; i++)
            {
                _fixedLeases[i] = _service.Get(_fixedId);
                _service.Get(_recycleId);
            }

            _recycleLease = _service.Get(_recycleId);
            Assert.IsTrue(_recycleLease.IsValid);
            Assert.IsFalse(_service.Get(_fixedId).IsValid);
            Assert.AreEqual(1, Sink.WarningCount);
        }

        private void MeasuredLoop()
        {
            for (int i = 0; i < _cycles; i++)
            {
                RunCycle();
            }
        }

        private void ControlLoop()
        {
            for (int i = 0; i < _cycles; i++)
            {
                RunCycle();
                _controlBuffer = new object[ControlBufferLength];
            }
        }

        private void RunCycle()
        {
            _service.Release(_fixedLeases[0]);
            _fixedLeases[0] = _service.Get(_fixedId);
            _service.Get(_fixedId);

            PoolLease recycled = _service.Get(_recycleId);
            _service.Release(recycled);
            _recycleLease = _service.Get(_recycleId);

            ReadCounters(_fixedId);
            ReadCounters(_recycleId);
        }

        private void ReadCounters(string id)
        {
            _service.TryGetStats(id, out PoolStats stats);
            _counterTotal += stats.Capacity + stats.Created + stats.InUse + stats.PeakInUse + stats.Exhausted;
            _counterTotal += stats.Recycled + stats.DoubleRelease + stats.StaleRelease + stats.ForeignRelease + stats.Lost;
        }
    }
}
