using System;
using System.Collections.Generic;
using System.Reflection;

namespace CozyGachapon.Tests.Audio
{
    public static class AudioTypeReferenceScanner
    {
        private const BindingFlags MemberFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance
            | BindingFlags.Static | BindingFlags.DeclaredOnly;

        public static void Collect(IReadOnlyList<Type> types, IReadOnlyList<string> forbiddenRoots, List<string> violations)
        {
            for (int t = 0; t < types.Count; t++)
            {
                Type type = types[t];
                Check(type, type.BaseType, forbiddenRoots, violations);
                Type[] interfaces = type.GetInterfaces();
                for (int i = 0; i < interfaces.Length; i++)
                {
                    Check(type, interfaces[i], forbiddenRoots, violations);
                }

                CollectMembers(type, forbiddenRoots, violations);
            }
        }

        private static void CollectMembers(Type type, IReadOnlyList<string> forbiddenRoots, List<string> violations)
        {
            FieldInfo[] fields = type.GetFields(MemberFlags);
            for (int i = 0; i < fields.Length; i++)
            {
                Check(type, fields[i].FieldType, forbiddenRoots, violations);
            }

            PropertyInfo[] properties = type.GetProperties(MemberFlags);
            for (int i = 0; i < properties.Length; i++)
            {
                Check(type, properties[i].PropertyType, forbiddenRoots, violations);
            }

            MethodInfo[] methods = type.GetMethods(MemberFlags);
            for (int i = 0; i < methods.Length; i++)
            {
                Check(type, methods[i].ReturnType, forbiddenRoots, violations);
                CheckParameters(type, methods[i].GetParameters(), forbiddenRoots, violations);
            }

            ConstructorInfo[] constructors = type.GetConstructors(MemberFlags);
            for (int i = 0; i < constructors.Length; i++)
            {
                CheckParameters(type, constructors[i].GetParameters(), forbiddenRoots, violations);
            }
        }

        private static void CheckParameters(
            Type owner, ParameterInfo[] parameters, IReadOnlyList<string> forbiddenRoots, List<string> violations)
        {
            for (int i = 0; i < parameters.Length; i++)
            {
                Check(owner, parameters[i].ParameterType, forbiddenRoots, violations);
            }
        }

        private static void Check(Type owner, Type used, IReadOnlyList<string> forbiddenRoots, List<string> violations)
        {
            if (used == null)
            {
                return;
            }

            if (used.HasElementType)
            {
                Check(owner, used.GetElementType(), forbiddenRoots, violations);
            }

            if (used.IsGenericType)
            {
                Type[] arguments = used.GetGenericArguments();
                for (int i = 0; i < arguments.Length; i++)
                {
                    Check(owner, arguments[i], forbiddenRoots, violations);
                }
            }

            if (IsForbidden(used.Namespace, forbiddenRoots))
            {
                violations.Add(owner.Name + "->" + used.Name);
            }
        }

        private static bool IsForbidden(string typeNamespace, IReadOnlyList<string> forbiddenRoots)
        {
            if (typeNamespace == null)
            {
                return false;
            }

            for (int i = 0; i < forbiddenRoots.Count; i++)
            {
                string root = forbiddenRoots[i];
                if (typeNamespace == root || typeNamespace.StartsWith(root + ".", StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
