using CozyGachapon.Contracts;
using CozyGachapon.Pooling;
using NUnit.Framework;

namespace CozyGachapon.Tests.Pooling
{
    public class PoolExhaustionTests : PoolingTestFixture
    {
        [Test]
        public void AC16_FixedExhausted_GetReturnsEmpty_CountsExhausted_WarnsOnce()
        {
            ObjectPoolService service = CreateReadyPolicyService(PoolExhaustionPolicy.Fixed, null, 3);
            for (int i = 0; i < 3; i++)
            {
                Assert.IsTrue(service.Get(PoolId(0)).IsValid);
            }

            for (int i = 1; i <= 3; i++)
            {
                PoolLease lease = service.Get(PoolId(0));

                Assert.IsFalse(lease.IsValid);
                service.TryGetStats(PoolId(0), out PoolStats stats);
                Assert.AreEqual(i, stats.Exhausted);
                Assert.AreEqual(3, stats.Created);
                Assert.AreEqual(3, stats.InUse);
                Assert.AreEqual(0, stats.Recycled);
            }

            Assert.AreEqual(1, Sink.WarningCount);
            Assert.AreEqual(PoolDiagnosticCode.PoolExhausted, Sink.LastWarningCode);
            Assert.AreEqual(PoolId(0), Sink.LastWarningPoolId);
            Assert.AreEqual(0, Sink.ErrorCount);
        }

        [Test]
        public void AC16_Edge_ReleaseThenGetThenExhaustAgain_NoExtraWarning()
        {
            ObjectPoolService service = CreateReadyPolicyService(PoolExhaustionPolicy.Fixed, null, 2);
            PoolLease first = service.Get(PoolId(0));
            service.Get(PoolId(0));
            service.Get(PoolId(0));
            Assert.AreEqual(1, Sink.WarningCount);

            service.Release(first);
            Assert.IsTrue(service.Get(PoolId(0)).IsValid);
            Assert.IsFalse(service.Get(PoolId(0)).IsValid);

            service.TryGetStats(PoolId(0), out PoolStats stats);
            Assert.AreEqual(1, Sink.WarningCount);
            Assert.AreEqual(2, stats.Exhausted);
            Assert.AreEqual(0, Sink.ErrorCount);
        }

        [Test]
        public void AC16_Edge_WarningIsPerPool()
        {
            ObjectPoolService service = CreateReadyPolicyService(PoolExhaustionPolicy.Fixed, null, 1, 1);
            for (int pool = 0; pool < 2; pool++)
            {
                service.Get(PoolId(pool));
                service.Get(PoolId(pool));
                service.Get(PoolId(pool));
            }

            Assert.AreEqual(2, Sink.WarningCount);
            Assert.AreEqual(PoolId(1), Sink.LastWarningPoolId);
        }

        [Test]
        public void AC16_Edge_FixedExhausted_DoesNotCallHooks()
        {
            RecordingPoolHooks hooks = new RecordingPoolHooks();
            ObjectPoolService service = CreateReadyPolicyService(PoolExhaustionPolicy.Fixed, hooks, 2);
            service.Get(PoolId(0));
            service.Get(PoolId(0));
            int acquires = hooks.AcquireCount;

            service.Get(PoolId(0));
            service.Get(PoolId(0));

            service.TryGetStats(PoolId(0), out PoolStats stats);
            Assert.AreEqual(acquires, hooks.AcquireCount);
            Assert.AreEqual(0, hooks.ReturnCount);
            Assert.AreEqual(0, stats.Recycled);
            Assert.AreEqual(2, stats.Exhausted);
        }
    }
}
