using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using Unity.Profiling;

namespace CozyGachapon.Tests.Pooling
{
    public static class PoolStaticFieldChecker
    {
        public const string PoolingNamespace = "CozyGachapon.Pooling";

        private const BindingFlags StaticFlags = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
        private const string BackingFieldSuffix = "k__BackingField";

        public static void CollectPoolingTypes(Assembly assembly, List<Type> result)
        {
            CollectTypes(assembly, PoolingNamespace, result);
        }

        public static void CollectTypes(Assembly assembly, string namespaceRoot, List<Type> result)
        {
            Type[] all = assembly.GetTypes();
            for (int i = 0; i < all.Length; i++)
            {
                Type type = all[i];
                if (IsInNamespace(type, namespaceRoot) && !IsCompilerGenerated(type))
                {
                    result.Add(type);
                }
            }
        }

        public static void CollectViolations(IReadOnlyList<Type> types, List<string> violations)
        {
            for (int i = 0; i < types.Count; i++)
            {
                FieldInfo[] fields = types[i].GetFields(StaticFlags);
                for (int f = 0; f < fields.Length; f++)
                {
                    if (IsChecked(types[i], fields[f]) && !IsAllowed(fields[f]))
                    {
                        violations.Add(types[i].FullName + "." + fields[f].Name);
                    }
                }
            }
        }

        private static bool IsInNamespace(Type type, string namespaceRoot)
        {
            string typeNamespace = type.Namespace;
            if (typeNamespace == null)
            {
                return false;
            }

            return typeNamespace == namespaceRoot || typeNamespace.StartsWith(namespaceRoot + ".", StringComparison.Ordinal);
        }

        private static bool IsCompilerGenerated(Type type)
        {
            for (Type current = type; current != null; current = current.DeclaringType)
            {
                if (current.IsDefined(typeof(CompilerGeneratedAttribute), false))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsChecked(Type owner, FieldInfo field)
        {
            if (!field.IsDefined(typeof(CompilerGeneratedAttribute), false))
            {
                return true;
            }

            return field.Name.EndsWith(BackingFieldSuffix, StringComparison.Ordinal) || owner.GetEvent(field.Name, StaticFlags) != null;
        }

        private static bool IsAllowed(FieldInfo field)
        {
            if (!field.IsLiteral && !field.IsInitOnly)
            {
                return false;
            }

            Type fieldType = field.FieldType;
            return fieldType.IsPrimitive || fieldType == typeof(string) || fieldType.IsEnum || fieldType == typeof(ProfilerMarker);
        }
    }
}
