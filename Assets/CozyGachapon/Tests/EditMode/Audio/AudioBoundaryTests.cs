using System;
using System.Collections.Generic;
using System.IO;
using CozyGachapon.Audio;
using CozyGachapon.Tests.Audio.BoundaryFakes;
using CozyGachapon.Tests.Pooling;
using NUnit.Framework;
using UnityEngine;

namespace CozyGachapon.Tests.Audio
{
    public class AudioBoundaryTests
    {
        private const string AudioNamespace = "CozyGachapon.Audio";
        private const string FoundationNamespace = "CozyGachapon.Foundation";
        private const string FakesNamespace = "CozyGachapon.Tests.Audio.BoundaryFakes";
        private const string FoundationAsmdefPath = "CozyGachapon/Scripts/Foundation/CozyGachapon.Foundation.asmdef";

        private static readonly string[] GameplayRoots =
        {
            "CozyGachapon.Plinko",
            "CozyGachapon.Gacha",
            "CozyGachapon.Shelf",
            "CozyGachapon.Feedback",
            "CozyGachapon.Juice"
        };

        private static readonly string[] AudioRoots = { AudioNamespace };

        [Serializable]
        private sealed class AsmdefData
        {
            [SerializeField] private string[] references;

            public string[] References => references;
        }

        private static List<string> FindForbiddenAssemblies(IReadOnlyList<string> names)
        {
            List<string> found = new List<string>();
            for (int n = 0; n < names.Count; n++)
            {
                for (int r = 0; r < GameplayRoots.Length; r++)
                {
                    if (names[n] == GameplayRoots[r] || names[n].StartsWith(GameplayRoots[r] + ".", StringComparison.Ordinal))
                    {
                        found.Add(names[n]);
                    }
                }
            }

            return found;
        }

        private static List<string> ScanNamespace(string namespaceRoot, string[] forbiddenRoots)
        {
            List<Type> types = new List<Type>();
            PoolStaticFieldChecker.CollectTypes(typeof(AudioService).Assembly, namespaceRoot, types);
            List<string> violations = new List<string>();
            AudioTypeReferenceScanner.Collect(types, forbiddenRoots, violations);
            return violations;
        }

        private static List<string> ScanFakes(string[] forbiddenRoots)
        {
            List<Type> types = new List<Type>();
            PoolStaticFieldChecker.CollectTypes(typeof(AudioBoundaryTests).Assembly, FakesNamespace, types);
            List<string> violations = new List<string>();
            AudioTypeReferenceScanner.Collect(types, forbiddenRoots, violations);
            return violations;
        }

        [Test]
        public void AC31_FoundationAsmdef_HasNoGameplayReferences()
        {
            string path = Path.Combine(Application.dataPath, FoundationAsmdefPath);
            Assert.IsTrue(File.Exists(path), path);
            AsmdefData data = JsonUtility.FromJson<AsmdefData>(File.ReadAllText(path));

            Assert.IsNotNull(data.References);
            Assert.AreEqual(0, FindForbiddenAssemblies(data.References).Count);
        }

        [Test]
        public void AC31_FoundationAssembly_ReferencedAssemblies_HaveNoGameplay()
        {
            System.Reflection.AssemblyName[] referenced = typeof(AudioService).Assembly.GetReferencedAssemblies();
            List<string> names = new List<string>();
            for (int i = 0; i < referenced.Length; i++)
            {
                names.Add(referenced[i].Name);
            }

            Assert.Greater(names.Count, 0);
            Assert.AreEqual(0, FindForbiddenAssemblies(names).Count);
        }

        [Test]
        public void AC31_Control_GameplayAssemblyNames_AreDetected()
        {
            List<string> found = FindForbiddenAssemblies(
                new[] { "CozyGachapon.Contracts", "CozyGachapon.Plinko", "CozyGachapon.Gacha.Runtime", "CozyGachapon.Foundation" });

            Assert.AreEqual(2, found.Count);
        }

        [Test]
        public void AC31_AudioTypes_DoNotReferenceGameplayNamespaces()
        {
            List<string> violations = ScanNamespace(AudioNamespace, GameplayRoots);

            Assert.AreEqual(0, violations.Count, string.Join(", ", violations));
        }

        [Test]
        public void AC41_BootstrapFoundationTypes_DoNotReferenceAudioTypes()
        {
            List<Type> types = new List<Type>();
            PoolStaticFieldChecker.CollectTypes(typeof(AudioService).Assembly, FoundationNamespace, types);
            List<string> violations = new List<string>();
            AudioTypeReferenceScanner.Collect(types, AudioRoots, violations);

            Assert.Greater(types.Count, 0);
            Assert.AreEqual(0, violations.Count, string.Join(", ", violations));
        }

        [Test]
        public void Control_Scanner_DetectsForbiddenReferenceInSignaturesAndArrays()
        {
            List<string> violations = ScanFakes(AudioRoots);

            Assert.IsTrue(violations.Contains(nameof(ReferencesAudioServiceFake) + "->" + nameof(AudioService)));
            Assert.IsTrue(violations.Contains(nameof(ReferencesAudioArrayFake) + "->" + nameof(AudioBootModule)));
        }

        [Test]
        public void Control_Scanner_IgnoresUnrelatedForbiddenRoots()
        {
            Assert.AreEqual(0, ScanFakes(GameplayRoots).Count);
        }

        [Test]
        public void AudioTypes_HaveOnlyImmutableStaticFields()
        {
            List<Type> types = new List<Type>();
            PoolStaticFieldChecker.CollectTypes(typeof(AudioService).Assembly, AudioNamespace, types);
            List<string> violations = new List<string>();
            PoolStaticFieldChecker.CollectViolations(types, violations);

            Assert.IsTrue(types.Contains(typeof(AudioService)));
            Assert.IsTrue(types.Contains(typeof(AudioServiceBootStep)));
            Assert.IsTrue(types.Contains(typeof(AudioBootModule)));
            Assert.AreEqual(0, violations.Count, string.Join("\n", violations));
        }
    }
}
