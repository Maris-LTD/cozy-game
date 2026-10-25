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
            for (int i = 0; i < _createdObjects.Count; i++)
            {
                if (_createdObjects[i] != null)
                {
                    Object.DestroyImmediate(_createdObjects[i]);
                }
            }

            _createdObjects.Clear();
        }

        protected GameObject CreatePrefab(string name)
        {
            GameObject prefab = new GameObject(name);
            _createdObjects.Add(prefab);
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
    }
}
