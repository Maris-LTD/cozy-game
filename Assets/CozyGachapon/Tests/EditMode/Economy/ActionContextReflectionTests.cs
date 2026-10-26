using System;
using System.Collections.Generic;
using System.Reflection;
using CozyGachapon.Contracts;
using CozyGachapon.Economy;
using NUnit.Framework;
using UnityEngine;

namespace CozyGachapon.Tests.Economy
{
    [TestFixture]
    public sealed class ActionContextReflectionTests
    {
        public static readonly Type ActionContextType = typeof(PlayerActionContext);

        private static readonly string[] ExpectedActionContextMethods =
        {
            "TrySpend",
            "TryPaySpin",
            "ResetForNewShop"
        };

        [Test]
        public void AC7a_PublicMethodsWithPlayerActionContext_ExactlyMatchExpectedSet()
        {
            List<Type> inspectedTypes = CollectInspectedTypes();
            List<string> foundMethodNames = new List<string>();

            for (int t = 0; t < inspectedTypes.Count; t++)
            {
                Type type = inspectedTypes[t];
                if (IsEventDataStruct(type))
                {
                    continue;
                }

                MethodInfo[] methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly);
                for (int m = 0; m < methods.Length; m++)
                {
                    MethodInfo method = methods[m];
                    if (HasActionContextParameter(method) && !ContainsString(foundMethodNames, method.Name))
                    {
                        foundMethodNames.Add(method.Name);
                    }
                }
            }

            Assert.That(foundMethodNames.Count, Is.EqualTo(ExpectedActionContextMethods.Length));
            for (int i = 0; i < ExpectedActionContextMethods.Length; i++)
            {
                Assert.That(ContainsString(foundMethodNames, ExpectedActionContextMethods[i]), Is.True);
            }
        }

        [Test]
        public void AC7a_NoPublicBalanceSetter_NoMutablePublicFields_NoRefReturnToBalance()
        {
            List<Type> inspectedTypes = CollectInspectedTypes();

            for (int t = 0; t < inspectedTypes.Count; t++)
            {
                Type type = inspectedTypes[t];
                if (typeof(ScriptableObject).IsAssignableFrom(type) || type.IsEnum)
                {
                    continue;
                }

                PropertyInfo[] properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly);
                for (int p = 0; p < properties.Length; p++)
                {
                    PropertyInfo prop = properties[p];
                    MethodInfo setter = prop.GetSetMethod(false);
                    Assert.That(setter, Is.Null, type.Name + "." + prop.Name + " must not have a public setter.");
                    Assert.That(prop.PropertyType.IsByRef, Is.False, type.Name + "." + prop.Name + " must not return by ref.");
                }

                FieldInfo[] fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly);
                for (int f = 0; f < fields.Length; f++)
                {
                    FieldInfo field = fields[f];
                    bool isImmutable = field.IsInitOnly || field.IsLiteral;
                    Assert.That(isImmutable, Is.True, type.Name + "." + field.Name + " must be readonly or const.");
                }

                MethodInfo[] methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly);
                for (int m = 0; m < methods.Length; m++)
                {
                    MethodInfo method = methods[m];
                    bool isWritableRef = method.ReturnType.IsByRef &&
                        method.ReturnParameter.GetCustomAttributes(typeof(System.Runtime.CompilerServices.IsReadOnlyAttribute), false).Length == 0;
                    Assert.That(isWritableRef, Is.False, type.Name + "." + method.Name + " must not return a writable ref.");
                }
            }
        }

        private static List<Type> CollectInspectedTypes()
        {
            List<Type> result = new List<Type>();
            Assembly coreAssembly = typeof(EconomyCore).Assembly;
            Type[] coreTypes = coreAssembly.GetExportedTypes();
            for (int i = 0; i < coreTypes.Length; i++)
            {
                result.Add(coreTypes[i]);
            }

            result.Add(typeof(IWallet));
            result.Add(typeof(ISpinPriceService));
            result.Add(typeof(IIncomePipeline));
            result.Add(typeof(IBaseCoinValueSource));
            return result;
        }

        private static bool IsEventDataStruct(Type type)
        {
            return type.IsValueType && !type.IsEnum && type.Name.EndsWith("Event", StringComparison.Ordinal);
        }

        private static bool HasActionContextParameter(MethodInfo method)
        {
            ParameterInfo[] parameters = method.GetParameters();
            for (int i = 0; i < parameters.Length; i++)
            {
                Type paramType = parameters[i].ParameterType;
                if (paramType.IsByRef)
                {
                    paramType = paramType.GetElementType();
                }

                if (paramType == ActionContextType)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool ContainsString(List<string> list, string target)
        {
            for (int i = 0; i < list.Count; i++)
            {
                if (string.Equals(list[i], target, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
