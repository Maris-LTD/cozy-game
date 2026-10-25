using System;
using System.Collections.Generic;
using System.Reflection;
using CozyGachapon.Pooling;
using CozyGachapon.Tests.Pooling.StaticFakes;
using NUnit.Framework;

namespace CozyGachapon.Tests.Pooling
{
    public class PoolNamespaceBoundaryTests
    {
        private const string FakesNamespace = "CozyGachapon.Tests.Pooling.StaticFakes";

        private static List<string> Check(params Type[] types)
        {
            List<string> violations = new List<string>();
            PoolStaticFieldChecker.CollectViolations(types, violations);
            return violations;
        }

        [Test]
        public void AC32_PoolingAssembly_HasOnlyImmutableStaticFields()
        {
            List<Type> types = new List<Type>();
            PoolStaticFieldChecker.CollectPoolingTypes(typeof(ObjectPoolService).Assembly, types);
            List<string> violations = new List<string>();
            PoolStaticFieldChecker.CollectViolations(types, violations);

            Assert.GreaterOrEqual(types.Count, 1);
            Assert.AreEqual(0, violations.Count, string.Join("\n", violations));
        }

        [Test]
        public void AC32_PoolingAssembly_TypeSetContainsServiceSessionAndBootStep()
        {
            List<Type> types = new List<Type>();
            PoolStaticFieldChecker.CollectPoolingTypes(typeof(ObjectPoolService).Assembly, types);

            Assert.IsTrue(types.Contains(typeof(ObjectPoolService)));
            Assert.IsTrue(types.Contains(typeof(PoolSession)));
            Assert.IsTrue(types.Contains(typeof(PoolingServiceBootStep)));
        }

        [Test]
        public void AC32_Control_MutableStaticField_IsReportedWithTypeAndFieldName()
        {
            List<string> violations = Check(typeof(MutableStaticFieldFake));

            Assert.AreEqual(1, violations.Count);
            StringAssert.Contains(nameof(MutableStaticFieldFake), violations[0]);
            StringAssert.Contains(nameof(MutableStaticFieldFake.Counter), violations[0]);
        }

        [Test]
        public void AC32_Control_StaticAutoProperty_IsReportedViaBackingField()
        {
            List<string> violations = Check(typeof(StaticAutoPropertyFake));

            Assert.AreEqual(1, violations.Count);
            StringAssert.Contains("k__BackingField", violations[0]);
        }

        [Test]
        public void AC32_Control_StaticEvent_IsReported()
        {
            List<string> violations = Check(typeof(StaticEventFake));

            Assert.AreEqual(1, violations.Count);
            StringAssert.Contains("Fired", violations[0]);
        }

        [Test]
        public void AC32_Control_StaticReadonlyReferenceType_IsReported()
        {
            List<string> violations = Check(typeof(StaticReadonlyReferenceFake));

            Assert.AreEqual(1, violations.Count);
            StringAssert.Contains("Table", violations[0]);
        }

        [Test]
        public void AC32_Control_GenericAndNestedTypes_AreReported()
        {
            Assert.AreEqual(1, Check(typeof(GenericMutableFake<>)).Count);
            Assert.AreEqual(1, Check(typeof(NestedMutableFake.Inner)).Count);
        }

        [Test]
        public void AC32_Control_ImmutableStaticFields_AreAccepted()
        {
            Assert.AreEqual(0, Check(typeof(ImmutableStaticFake)).Count);
        }

        [Test]
        public void AC32_Control_CompilerGeneratedLambdaCache_IsIgnored()
        {
            Type[] nested = typeof(LambdaCacheFake).GetNestedTypes(BindingFlags.NonPublic | BindingFlags.Public);
            Assert.GreaterOrEqual(nested.Length, 1);

            List<Type> collected = new List<Type>();
            PoolStaticFieldChecker.CollectTypes(typeof(PoolNamespaceBoundaryTests).Assembly, FakesNamespace, collected);
            for (int i = 0; i < nested.Length; i++)
            {
                Assert.IsFalse(collected.Contains(nested[i]));
            }

            Assert.IsTrue(collected.Contains(typeof(LambdaCacheFake)));
            Assert.AreEqual(0, Check(typeof(LambdaCacheFake)).Count);
        }

        [Test]
        public void AC32_Control_NamespaceFilter_CollectsGenericAndNestedFakes()
        {
            List<Type> collected = new List<Type>();
            PoolStaticFieldChecker.CollectTypes(typeof(PoolNamespaceBoundaryTests).Assembly, FakesNamespace, collected);
            List<string> violations = new List<string>();
            PoolStaticFieldChecker.CollectViolations(collected, violations);

            Assert.IsTrue(collected.Contains(typeof(GenericMutableFake<>)));
            Assert.IsTrue(collected.Contains(typeof(NestedMutableFake.Inner)));
            Assert.AreEqual(6, violations.Count, string.Join("\n", violations));
        }
    }
}
