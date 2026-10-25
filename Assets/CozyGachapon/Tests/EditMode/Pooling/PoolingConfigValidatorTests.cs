using System.Collections.Generic;
using CozyGachapon.Contracts;
using CozyGachapon.Pooling;
using NUnit.Framework;
using UnityEngine;

namespace CozyGachapon.Tests.Pooling
{
    public class PoolingConfigValidatorTests : PoolingTestFixture
    {
        private readonly List<PoolConfigIssue> _issues = new List<PoolConfigIssue>();

        [SetUp]
        public void ClearIssues()
        {
            _issues.Clear();
        }

        [Test]
        public void Validate_PrefabWithAudioSource_ReportsOneWarningNamingPoolId()
        {
            GameObject prefab = CreatePrefab("audio.prefab");
            prefab.AddComponent<AudioSource>();
            Config.SetPoolsForTest(new[] { new PoolDefinition("sfx.pop", prefab, PoolExhaustionPolicy.Fixed, 0) });

            PoolingConfigValidator.Validate(Config, _issues);

            Assert.AreEqual(1, _issues.Count);
            Assert.AreEqual(PoolDiagnosticCode.PrefabHasAudioSource, _issues[0].Code);
            Assert.AreEqual("sfx.pop", _issues[0].PoolId);
            Assert.IsFalse(_issues[0].IsError);
        }

        [Test]
        public void Validate_AudioSourceOnChild_ReportsOneWarning()
        {
            GameObject prefab = CreatePrefab("parent.prefab");
            GameObject child = new GameObject("child");
            child.transform.SetParent(prefab.transform);
            child.AddComponent<AudioSource>();
            Config.SetPoolsForTest(new[] { new PoolDefinition("sfx.child", prefab, PoolExhaustionPolicy.Fixed, 0) });

            PoolingConfigValidator.Validate(Config, _issues);

            Assert.AreEqual(1, _issues.Count);
            Assert.AreEqual("sfx.child", _issues[0].PoolId);
        }

        [Test]
        public void Validate_PrefabWithoutAudioSource_ReportsNothing()
        {
            Config.SetPoolsForTest(new[] { CreateDefinition("plain.pool", 0) });

            PoolingConfigValidator.Validate(Config, _issues);

            Assert.AreEqual(0, _issues.Count);
        }

        [Test]
        public void Register_PrefabWithAudioSource_StillAccepted()
        {
            GameObject prefab = CreatePrefab("audio.prefab");
            prefab.AddComponent<AudioSource>();
            Config.SetPoolsForTest(new[] { new PoolDefinition("sfx.pop", prefab, PoolExhaustionPolicy.Fixed, 0) });
            PoolingConfigValidator.Validate(Config, _issues);
            ObjectPoolService service = CreateService();

            PoolRegistrationResult result = service.Register("sfx.pop", 8, false);

            Assert.AreEqual(1, _issues.Count);
            Assert.IsTrue(result.Accepted);
            Assert.AreEqual(8, result.Capacity);
            Assert.AreEqual(0, Sink.ErrorCount);
        }
    }
}
