using System.Collections.Generic;
using CozyGachapon.Contracts;
using CozyGachapon.Pooling;
using NUnit.Framework;
using UnityEngine;

namespace CozyGachapon.Tests.Pooling
{
    public enum OldSessionState
    {
        Accepting = 0,
        Prewarming = 1,
        Ready = 2
    }

    public abstract class PoolSessionTestBase : PoolingTestFixture
    {
        protected const string CoinId = "plinko.coin";
        protected const int Declared = 4;
        protected const int Headroom = 2;
        protected const int TargetFps = 60;
        protected const int PerFrame = 2;
        protected const int TickGuard = 100;

        private readonly List<GameObject> _buffer = new List<GameObject>();

        protected Transform Root { get; private set; }
        protected PoolingBootModule OldModule { get; private set; }
        protected CountingPoolHooks OldHooks { get; private set; }
        protected Transform OldContainer { get; private set; }
        protected int Capacity { get; private set; }
        protected List<GameObject> OldObjects { get; } = new List<GameObject>();
        protected List<PoolLease> OldLeases { get; } = new List<PoolLease>();

        protected void ConfigureCoin()
        {
            GameObject prefab = CreateVisualPrefab(CoinId, 1);
            Config.SetPoolsForTest(new[] { new PoolDefinition(CoinId, prefab, PoolExhaustionPolicy.Fixed, Headroom) });
            Config.SetLimitsForTest(1000, 10000);
            Config.SetPrewarmForTest(PerFrame, 0.25f);
            Root = CreateRoot();
        }

        protected PoolingBootModule NewSession()
        {
            PoolingBootModule module = new PoolingBootModule(Config, Root, Sink, TargetFps);
            module.ServiceStep.Run();
            return module;
        }

        protected PoolRegistrationResult RegisterCoin(PoolingBootModule module, CountingPoolHooks hooks)
        {
            return module.Service.Register(CoinId, Declared, false, hooks);
        }

        protected void PrewarmToReady(PoolingBootModule module)
        {
            module.PrewarmStep.Run();
            for (int i = 0; i < TickGuard && module.Service.Phase != PoolServicePhase.Ready; i++)
            {
                module.Tick();
            }

            Assert.AreEqual(PoolServicePhase.Ready, module.Service.Phase);
        }

        protected void ExhaustOnce(ObjectPoolService service, List<PoolLease> leases)
        {
            for (int i = 0; i < Capacity; i++)
            {
                PoolLease lease = service.Get(CoinId);
                Assert.IsTrue(lease.IsValid);
                leases.Add(lease);
            }

            Assert.IsFalse(service.Get(CoinId).IsValid);
        }

        protected PoolStats Stats(ObjectPoolService service)
        {
            Assert.IsTrue(service.TryGetStats(CoinId, out PoolStats stats));
            return stats;
        }

        protected void PrepareOldSession(OldSessionState state)
        {
            ConfigureCoin();
            OldHooks = new CountingPoolHooks();
            OldModule = NewSession();
            PoolRegistrationResult result = RegisterCoin(OldModule, OldHooks);
            Assert.IsTrue(result.Accepted);
            Capacity = result.Capacity;

            if (state == OldSessionState.Prewarming)
            {
                OldModule.PrewarmStep.Run();
                OldModule.Tick();
                Assert.AreEqual(PoolServicePhase.Prewarming, OldModule.Service.Phase);
                PoolStats stats = Stats(OldModule.Service);
                Assert.Greater(stats.Created, 0);
                Assert.Less(stats.Created, Capacity);
            }

            if (state == OldSessionState.Ready)
            {
                PrewarmToReady(OldModule);
                ExhaustOnce(OldModule.Service, OldLeases);
                Assert.Greater(Stats(OldModule.Service).Exhausted, 0);
            }

            RecordOldObjects();
        }

        private void RecordOldObjects()
        {
            OldContainer = Root.childCount > 0 ? Root.GetChild(0) : null;
            OldModule.Service.CopyFreeObjects(CoinId, _buffer);
            OldObjects.AddRange(_buffer);
            for (int i = 0; i < OldLeases.Count; i++)
            {
                OldObjects.Add(OldLeases[i].Instance);
            }
        }
    }
}
