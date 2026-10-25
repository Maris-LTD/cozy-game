using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using CozyGachapon.Contracts;
using CozyGachapon.Pooling;
using NUnit.Framework;
using UnityEngine;

namespace CozyGachapon.Tests.Pooling
{
    public class PoolTimeIndependenceTests : PoolingTestFixture
    {
        private const int Capacity = 3;
        private const int RecycleRounds = 9;
        private const double FastForwardSeconds = 3600d;

        private static readonly Type[] ForbiddenTypes =
        {
            typeof(DateTime),
            typeof(DateTimeOffset),
            typeof(TimeSpan),
            typeof(System.Diagnostics.Stopwatch),
            typeof(System.Threading.Timer),
            typeof(System.Timers.Timer),
            typeof(ITimeProvider)
        };

        private sealed class FakeClock : ITimeProvider
        {
            public DateTimeOffset UtcNow { get; private set; } = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

            public void Advance(TimeSpan span)
            {
                UtcNow += span;
            }
        }

        [Test]
        public void AC23_FrozenVsFastForwardClock_SameSequence_IdenticalTrace()
        {
            float previousScale = Time.timeScale;
            List<int> frozen;
            try
            {
                Time.timeScale = 0f;
                frozen = RunSequence(TimeSpan.Zero);
            }
            finally
            {
                Time.timeScale = previousScale;
            }

            int operations = Capacity + RecycleRounds + 2 + Capacity;
            List<int> fast = RunSequence(TimeSpan.FromSeconds(FastForwardSeconds / operations));

            Assert.AreEqual(frozen.Count, fast.Count);
            for (int i = 0; i < frozen.Count; i++)
            {
                Assert.AreEqual(frozen[i], fast[i], "trace differs at " + i);
            }
        }

        [Test]
        public void AC23_PoolingTypes_HaveNoTimeDependency()
        {
            Type[] types = typeof(ObjectPoolService).Assembly.GetTypes();
            BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;
            List<string> violations = new List<string>();
            for (int t = 0; t < types.Length; t++)
            {
                Type type = types[t];
                if (type.Namespace == null || !type.Namespace.StartsWith("CozyGachapon.Pooling", StringComparison.Ordinal))
                {
                    continue;
                }

                CollectFieldViolations(type, type.GetFields(flags), violations);
                CollectMethodViolations(type, type.GetMethods(flags), violations);
                CollectConstructorViolations(type, type.GetConstructors(flags), violations);
            }

            Assert.IsEmpty(violations, string.Join(", ", violations));
        }

        [Test]
        public void AC23_PoolingSources_HaveNoTimeReference()
        {
            string folder = Path.Combine(Application.dataPath, "CozyGachapon/Scripts/Foundation/Pooling");
            Regex timeToken = new Regex(@"\b(Time|Stopwatch|DateTime|DateTimeOffset|TimeSpan|Timer|ITimeProvider)\b");
            string[] files = Directory.GetFiles(folder, "*.cs");
            List<string> violations = new List<string>();
            for (int i = 0; i < files.Length; i++)
            {
                if (timeToken.IsMatch(File.ReadAllText(files[i])))
                {
                    violations.Add(Path.GetFileName(files[i]));
                }
            }

            Assert.Greater(files.Length, 0);
            Assert.IsEmpty(violations, string.Join(", ", violations));
        }

        private List<int> RunSequence(TimeSpan step)
        {
            FakeClock clock = new FakeClock();
            RecordingPoolHooks hooks = new RecordingPoolHooks();
            ObjectPoolService service = CreateReadyPolicyService(PoolExhaustionPolicy.RecycleOldest, hooks, Capacity);
            string id = PoolId(0);
            List<GameObject> order = new List<GameObject>();
            service.CopyFreeObjects(id, order);
            List<int> trace = new List<int>();
            List<PoolLease> leases = new List<PoolLease>();
            int seen = 0;

            for (int i = 0; i < Capacity + RecycleRounds; i++)
            {
                PoolLease lease = service.Get(id);
                leases.Add(lease);
                seen = Record(trace, service, id, lease, order, hooks, seen);
                clock.Advance(step);
            }

            service.Release(leases[leases.Count - 1]);
            seen = Record(trace, service, id, default, order, hooks, seen);
            clock.Advance(step);

            PoolLease reacquired = service.Get(id);
            seen = Record(trace, service, id, reacquired, order, hooks, seen);
            clock.Advance(step);

            for (int i = 0; i < Capacity; i++)
            {
                PoolLease lease = service.Get(id);
                seen = Record(trace, service, id, lease, order, hooks, seen);
                clock.Advance(step);
            }

            return trace;
        }

        private static int Record(List<int> trace, ObjectPoolService service, string id, PoolLease lease, List<GameObject> order, RecordingPoolHooks hooks, int seen)
        {
            trace.Add(lease.IsValid ? order.IndexOf(lease.Instance) : -1);
            trace.Add((int)lease.Sequence);
            trace.Add(lease.IsValid ? 1 : 0);
            service.TryGetStats(id, out PoolStats stats);
            trace.Add(stats.Capacity);
            trace.Add(stats.Created);
            trace.Add(stats.InUse);
            trace.Add(stats.PeakInUse);
            trace.Add(stats.Exhausted);
            trace.Add(stats.Recycled);
            trace.Add(stats.DoubleRelease);
            trace.Add(stats.StaleRelease);
            trace.Add(stats.ForeignRelease);
            trace.Add(stats.Lost);
            int victim = -1;
            for (int i = seen; i < hooks.Calls.Count; i++)
            {
                if (hooks.Calls[i].IsReturn && hooks.Calls[i].Reason == PoolReturnReason.Recycled)
                {
                    victim = order.IndexOf(hooks.Calls[i].Instance);
                }
            }

            trace.Add(victim);
            return hooks.Calls.Count;
        }

        private static void CollectFieldViolations(Type owner, FieldInfo[] fields, List<string> violations)
        {
            for (int i = 0; i < fields.Length; i++)
            {
                if (IsForbidden(fields[i].FieldType))
                {
                    violations.Add(owner.Name + "." + fields[i].Name);
                }
            }
        }

        private static void CollectMethodViolations(Type owner, MethodInfo[] methods, List<string> violations)
        {
            for (int i = 0; i < methods.Length; i++)
            {
                if (IsForbidden(methods[i].ReturnType) || HasForbiddenParameter(methods[i].GetParameters()))
                {
                    violations.Add(owner.Name + "." + methods[i].Name);
                }
            }
        }

        private static void CollectConstructorViolations(Type owner, ConstructorInfo[] constructors, List<string> violations)
        {
            for (int i = 0; i < constructors.Length; i++)
            {
                if (HasForbiddenParameter(constructors[i].GetParameters()))
                {
                    violations.Add(owner.Name + ".ctor");
                }
            }
        }

        private static bool HasForbiddenParameter(ParameterInfo[] parameters)
        {
            for (int i = 0; i < parameters.Length; i++)
            {
                if (IsForbidden(parameters[i].ParameterType))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsForbidden(Type type)
        {
            Type resolved = type.IsByRef ? type.GetElementType() : type;
            for (int i = 0; i < ForbiddenTypes.Length; i++)
            {
                if (resolved == ForbiddenTypes[i])
                {
                    return true;
                }
            }

            return false;
        }
    }
}
