using System;
using System.Collections.Generic;
using System.Reflection;
using CozyGachapon.Contracts;

namespace CozyGachapon.Tests.Economy
{
    public static class EconomyApiInvoker
    {
        private static readonly double[] DoubleBoundaries =
        {
            0d,
            -1d,
            double.NaN,
            double.PositiveInfinity,
            double.NegativeInfinity,
            double.MaxValue
        };

        private static readonly int[] IntBoundaries =
        {
            0,
            -1,
            int.MaxValue
        };

        private static readonly string[] StringBoundaries =
        {
            string.Empty,
            null,
            "currency.unknown",
            EconomyIds.Coin,
            "unknown.source.key"
        };

        public static List<object[]> BuildParameterCombinations(MethodInfo method)
        {
            ParameterInfo[] parameters = method.GetParameters();
            List<object[]> combinations = new List<object[]>();
            if (parameters.Length == 0)
            {
                combinations.Add(Array.Empty<object>());
                return combinations;
            }

            object[] defaults = new object[parameters.Length];
            List<object>[] perParamBoundaries = new List<object>[parameters.Length];

            for (int i = 0; i < parameters.Length; i++)
            {
                Type rawType = parameters[i].ParameterType;
                Type elemType = rawType.IsByRef ? rawType.GetElementType() : rawType;
                defaults[i] = GetDefaultValidValue(elemType);
                perParamBoundaries[i] = GetBoundaryValues(elemType);
            }

            for (int p = 0; p < parameters.Length; p++)
            {
                List<object> boundaries = perParamBoundaries[p];
                for (int b = 0; b < boundaries.Count; b++)
                {
                    object[] args = new object[parameters.Length];
                    for (int k = 0; k < parameters.Length; k++)
                    {
                        args[k] = k == p ? boundaries[b] : defaults[k];
                    }

                    combinations.Add(args);
                }
            }

            int maxBoundaryCount = 0;
            for (int p = 0; p < parameters.Length; p++)
            {
                if (perParamBoundaries[p].Count > maxBoundaryCount)
                {
                    maxBoundaryCount = perParamBoundaries[p].Count;
                }
            }

            for (int b = 0; b < maxBoundaryCount; b++)
            {
                object[] allBoundaryArgs = new object[parameters.Length];
                for (int p = 0; p < parameters.Length; p++)
                {
                    List<object> list = perParamBoundaries[p];
                    allBoundaryArgs[p] = list[b % list.Count];
                }

                combinations.Add(allBoundaryArgs);
            }

            return combinations;
        }

        private static object GetDefaultValidValue(Type type)
        {
            if (type == typeof(double)) return 1d;
            if (type == typeof(int)) return 1;
            if (type == typeof(bool)) return true;
            if (type == typeof(string)) return EconomyIds.Coin;
            if (type.IsEnum)
            {
                Array values = Enum.GetValues(type);
                return values.Length > 0 ? values.GetValue(0) : Activator.CreateInstance(type);
            }

            if (type == typeof(IncomeModifier))
            {
                return IncomeModifier.ForChannel("shelf.valid", IncomeChannel.Idle, ModifierTier.Synergy, 0.5d);
            }

            if (type == typeof(CoinResolvedEvent))
            {
                return new CoinResolvedEvent("coin.valid", CoinOrigin.Manual, 0, 1d, 5d, 1, CoinResolveReason.Bucket);
            }

            if (type == typeof(InteractiveEvent)) return default(InteractiveEvent);
            if (type == typeof(PausedEvent)) return new PausedEvent(default);
            if (type == typeof(ResumedEvent)) return new ResumedEvent(default, 1d);
            if (type == typeof(AdClosedEvent)) return new AdClosedEvent(5d);
            if (type == typeof(IIdlePlan)) return new FakeIdlePlan();
            if (type == typeof(ISimulationState)) return new FakeSimulationState(true, false);

            return type.IsValueType ? Activator.CreateInstance(type) : null;
        }

        private static List<object> GetBoundaryValues(Type type)
        {
            List<object> list = new List<object>();
            if (type == typeof(double))
            {
                for (int i = 0; i < DoubleBoundaries.Length; i++) list.Add(DoubleBoundaries[i]);
                return list;
            }

            if (type == typeof(int))
            {
                for (int i = 0; i < IntBoundaries.Length; i++) list.Add(IntBoundaries[i]);
                return list;
            }

            if (type == typeof(string))
            {
                for (int i = 0; i < StringBoundaries.Length; i++) list.Add(StringBoundaries[i]);
                return list;
            }

            if (type == typeof(bool))
            {
                list.Add(false);
                list.Add(true);
                return list;
            }

            if (type.IsEnum)
            {
                Array values = Enum.GetValues(type);
                for (int i = 0; i < values.Length; i++)
                {
                    list.Add(values.GetValue(i));
                }

                list.Add(Enum.ToObject(type, -999));
                return list;
            }

            if (type == typeof(IncomeModifier))
            {
                for (int i = 0; i < DoubleBoundaries.Length; i++)
                {
                    list.Add(new IncomeModifier(
                        StringBoundaries[i % StringBoundaries.Length],
                        IncomeChannel.Both,
                        ModifierTier.Synergy,
                        DoubleBoundaries[i],
                        ModifierTarget.Source,
                        StringBoundaries[(i + 1) % StringBoundaries.Length],
                        true));
                }

                return list;
            }

            if (type == typeof(CoinResolvedEvent))
            {
                for (int i = 0; i < DoubleBoundaries.Length; i++)
                {
                    list.Add(new CoinResolvedEvent(
                        StringBoundaries[i % StringBoundaries.Length],
                        CoinOrigin.Manual,
                        IntBoundaries[i % IntBoundaries.Length],
                        DoubleBoundaries[i],
                        DoubleBoundaries[i],
                        IntBoundaries[i % IntBoundaries.Length],
                        CoinResolveReason.Bucket));
                }

                return list;
            }

            if (type == typeof(ResumedEvent))
            {
                for (int i = 0; i < DoubleBoundaries.Length; i++)
                {
                    list.Add(new ResumedEvent(default, DoubleBoundaries[i]));
                }

                return list;
            }

            if (type == typeof(AdClosedEvent))
            {
                for (int i = 0; i < DoubleBoundaries.Length; i++)
                {
                    list.Add(new AdClosedEvent(DoubleBoundaries[i]));
                }

                return list;
            }

            if (type == typeof(IIdlePlan))
            {
                list.Add(null);
                FakeIdlePlan boundaryPlan = new FakeIdlePlan();
                boundaryPlan.AddSource(null, -10d);
                boundaryPlan.AddSource("shelf.slot1.idle", double.NaN);
                list.Add(boundaryPlan);
                return list;
            }

            if (type == typeof(ISimulationState))
            {
                list.Add(null);
                list.Add(new FakeSimulationState(true, false));
                return list;
            }

            list.Add(type.IsValueType ? Activator.CreateInstance(type) : null);
            return list;
        }
    }
}
