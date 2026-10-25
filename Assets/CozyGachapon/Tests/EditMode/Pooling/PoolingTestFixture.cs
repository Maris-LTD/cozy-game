using System.Collections.Generic;
using CozyGachapon.Contracts;
using CozyGachapon.Pooling;
using NUnit.Framework;
using UnityEngine;

namespace CozyGachapon.Tests.Pooling
{
    public abstract class PoolingTestFixture
    {
        private readonly List<Object> _createdObjects = new List<Object>();

        protected PoolingConfig Config { get; private set; }
        protected CountingPoolDiagnosticsSink Sink { get; private set; }

        [SetUp]
        public void SetUpFixture()
        {
            Config = ScriptableObject.CreateInstance<PoolingConfig>();
            _createdObjects.Add(Config);
            Sink = new CountingPoolDiagnosticsSink();
        }

        [TearDown]
        public void TearDownFixture()
        {
            for (int i = _createdObjects.Count - 1; i >= 0; i--)
            {
                if (_createdObjects[i] != null)
                {
                    Object.DestroyImmediate(_createdObjects[i]);
                }
            }

            _createdObjects.Clear();
        }

        protected T Track<T>(T instance) where T : Object
        {
            _createdObjects.Add(instance);
            return instance;
        }

        protected GameObject CreatePrefab(string name)
        {
            GameObject prefab = new GameObject(name);
            _createdObjects.Add(prefab);
            return prefab;
        }

        protected Transform CreateRoot()
        {
            return Track(new GameObject("Pools")).transform;
        }

        protected GameObject CreateVisualPrefab(string name, int childRenderers)
        {
            GameObject prefab = CreatePrefab(name);
            prefab.AddComponent<SpriteRenderer>();
            prefab.AddComponent<Rigidbody2D>();
            for (int i = 0; i < childRenderers; i++)
            {
                GameObject child = new GameObject("child" + i);
                child.transform.SetParent(prefab.transform, false);
                child.AddComponent<SpriteRenderer>();
            }

            return prefab;
        }

        protected PoolDefinition CreateDefinition(string id, int headroom, PoolExhaustionPolicy policy = PoolExhaustionPolicy.Fixed)
        {
            return new PoolDefinition(id, CreatePrefab(id), policy, headroom);
        }

        protected ObjectPoolService CreateService()
        {
            return new ObjectPoolService(Config, Sink);
        }

        protected static string PoolId(int index)
        {
            return "pool" + index;
        }

        protected ObjectPoolService CreateRegisteredService(params int[] capacities)
        {
            List<PoolDefinition> definitions = new List<PoolDefinition>();
            for (int i = 0; i < capacities.Length; i++)
            {
                definitions.Add(new PoolDefinition(PoolId(i), CreateVisualPrefab(PoolId(i), 2), PoolExhaustionPolicy.Fixed, 0));
            }

            Config.SetPoolsForTest(definitions);
            Config.SetLimitsForTest(1000, 10000);
            ObjectPoolService service = CreateService();
            for (int i = 0; i < capacities.Length; i++)
            {
                Assert.IsTrue(service.Register(PoolId(i), capacities[i], false).Accepted);
            }

            return service;
        }

        protected static int TotalCreated(ObjectPoolService service, int poolCount)
        {
            int total = 0;
            for (int i = 0; i < poolCount; i++)
            {
                service.TryGetStats(PoolId(i), out PoolStats stats);
                total += stats.Created;
            }

            return total;
        }
    }
}
