using System;
using System.Collections.Generic;
using System.Reflection;
using CozyGachapon.Pooling;
using NUnit.Framework;

namespace CozyGachapon.Tests.Pooling
{
    public class PoolingApiBoundaryTests
    {
        [Test]
        public void PoolingApi_NeverExposesBootstrapFoundationTypes()
        {
            const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;
            List<string> violations = new List<string>();
            Type[] types = typeof(ObjectPoolService).Assembly.GetTypes();

            for (int t = 0; t < types.Length; t++)
            {
                Type type = types[t];
                if (type.Namespace == null || !type.Namespace.StartsWith("CozyGachapon.Pooling", StringComparison.Ordinal))
                {
                    continue;
                }

                foreach (FieldInfo field in type.GetFields(flags))
                {
                    CollectViolation(type, field.FieldType, violations);
                }

                foreach (PropertyInfo property in type.GetProperties(flags))
                {
                    CollectViolation(type, property.PropertyType, violations);
                }

                foreach (MethodInfo method in type.GetMethods(flags))
                {
                    CollectViolation(type, method.ReturnType, violations);
                    foreach (ParameterInfo parameter in method.GetParameters())
                    {
                        CollectViolation(type, parameter.ParameterType, violations);
                    }
                }

                foreach (ConstructorInfo constructor in type.GetConstructors(flags))
                {
                    foreach (ParameterInfo parameter in constructor.GetParameters())
                    {
                        CollectViolation(type, parameter.ParameterType, violations);
                    }
                }
            }

            Assert.AreEqual(0, violations.Count, string.Join(", ", violations));
        }

        private static void CollectViolation(Type owner, Type used, List<string> violations)
        {
            if (used.HasElementType)
            {
                CollectViolation(owner, used.GetElementType(), violations);
            }

            if (used.IsGenericType)
            {
                Type[] arguments = used.GetGenericArguments();
                for (int i = 0; i < arguments.Length; i++)
                {
                    CollectViolation(owner, arguments[i], violations);
                }
            }

            if (used.Namespace != null && used.Namespace.StartsWith("CozyGachapon.Foundation", StringComparison.Ordinal))
            {
                violations.Add(owner.Name + "->" + used.Name);
            }
        }
    }
}
