using System.Collections.Generic;
using CozyGachapon.Contracts;
using CozyGachapon.Pooling;
using NUnit.Framework;

namespace CozyGachapon.Tests.Pooling
{
    public class PoolSessionResetEdgeTests : PoolSessionTestBase
    {
        [Test]
        public void AC31_Edge_ExhaustionWarningFlag_IsNotInheritedByNewSession()
        {
            PrepareOldSession(OldSessionState.Ready);
            OldModule.Session.Teardown();
            PoolingBootModule fresh = NewSession();
            RegisterCoin(fresh, new CountingPoolHooks());
            PrewarmToReady(fresh);

            int warningsBefore = Sink.WarningCount;
            ExhaustOnce(fresh.Service, new List<PoolLease>());

            Assert.AreEqual(warningsBefore + 1, Sink.WarningCount);
            Assert.AreEqual(PoolDiagnosticCode.PoolExhausted, Sink.LastWarningCode);
            Assert.AreEqual(CoinId, Sink.LastWarningPoolId);
            Assert.AreEqual(1, Stats(fresh.Service).Exhausted);
        }

        [Test]
        public void AC31_Edge_OldSessionLeaseReleasedBeforePrewarm_IsIgnoredWithoutException()
        {
            PrepareOldSession(OldSessionState.Ready);
            PoolLease oldLease = OldLeases[0];
            OldModule.Session.Teardown();
            PoolingBootModule fresh = NewSession();
            CountingPoolHooks freshHooks = new CountingPoolHooks();
            RegisterCoin(fresh, freshHooks);

            Assert.DoesNotThrow(() => fresh.Service.Release(oldLease));

            PoolStats stats = Stats(fresh.Service);
            Assert.AreEqual(1, stats.ForeignRelease + stats.StaleRelease);
            Assert.AreEqual(0, stats.InUse);
            Assert.AreEqual(0, freshHooks.ReturnCount);
            Assert.AreEqual(0, OldHooks.ReturnCount);
        }

        [Test]
        public void AC31_Edge_OldSessionLeaseReleasedWhenReady_IsIgnoredWithoutException()
        {
            PrepareOldSession(OldSessionState.Ready);
            PoolLease oldLease = OldLeases[0];
            OldModule.Session.Teardown();
            PoolingBootModule fresh = NewSession();
            CountingPoolHooks freshHooks = new CountingPoolHooks();
            RegisterCoin(fresh, freshHooks);
            PrewarmToReady(fresh);

            Assert.DoesNotThrow(() => fresh.Service.Release(oldLease));

            PoolStats stats = Stats(fresh.Service);
            Assert.AreEqual(1, stats.ForeignRelease + stats.StaleRelease);
            Assert.AreEqual(0, stats.InUse);
            Assert.AreEqual(Capacity, stats.Created);
            Assert.AreEqual(0, freshHooks.ReturnCount);
        }

        [Test]
        public void AC31_Edge_RunningServiceStepTwice_TearsDownPreviousSession()
        {
            ConfigureCoin();
            CountingPoolHooks hooks = new CountingPoolHooks();
            PoolingBootModule module = NewSession();
            PoolSession first = module.Session;
            ObjectPoolService firstService = module.Service;
            RegisterCoin(module, hooks);
            PrewarmToReady(module);
            Assert.AreEqual(1, Root.childCount);

            module.ServiceStep.Run();

            Assert.IsTrue(first.IsTornDown);
            Assert.IsFalse(module.Session.IsTornDown);
            Assert.AreNotSame(first, module.Session);
            Assert.AreNotSame(firstService, module.Service);
            Assert.AreEqual(PoolServicePhase.Accepting, module.Service.Phase);
            Assert.AreEqual(0, Root.childCount);
            Assert.AreEqual(0, hooks.ReturnCount);
        }

        [Test]
        public void AC31_Edge_OldLeaseReleasedAgainstShutDownService_IsIgnoredWithoutException()
        {
            PrepareOldSession(OldSessionState.Ready);
            PoolLease oldLease = OldLeases[0];
            ObjectPoolService oldService = OldModule.Service;
            OldModule.Session.Teardown();

            Assert.DoesNotThrow(() => oldService.Release(oldLease));
            Assert.DoesNotThrow(() => oldService.Release(CoinId, oldLease));
            Assert.DoesNotThrow(() => oldService.Get(CoinId));
            Assert.IsFalse(oldService.Register(CoinId, Declared, false).Accepted);
            Assert.AreEqual(0, OldHooks.ReturnCount);
        }

        [Test]
        public void AC31_Edge_TeardownAndDispose_AreIdempotent()
        {
            ConfigureCoin();
            PoolingBootModule module = NewSession();
            PoolSession session = module.Session;

            Assert.DoesNotThrow(session.Teardown);
            Assert.DoesNotThrow(session.Teardown);
            Assert.DoesNotThrow(session.Dispose);
            Assert.IsTrue(session.IsTornDown);
            Assert.IsTrue(session.Service.IsShutDown);
        }
    }
}
