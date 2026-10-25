using System;
using System.Collections.Generic;
using CozyGachapon.Contracts;

namespace CozyGachapon.Foundation
{
    public static class WelcomeStepOrder
    {
        private static readonly OrdinalIdComparer Comparer = new OrdinalIdComparer();

        public static void Build(IReadOnlyList<string> order, List<IWelcomeStep> registered, List<IWelcomeStep> result)
        {
            result.Clear();
            if (order != null)
            {
                for (int i = 0; i < order.Count; i++)
                {
                    IWelcomeStep named = Find(registered, order[i]);
                    if (named != null && !result.Contains(named))
                    {
                        result.Add(named);
                    }
                }
            }

            int namedCount = result.Count;
            for (int i = 0; i < registered.Count; i++)
            {
                if (!result.Contains(registered[i]))
                {
                    result.Add(registered[i]);
                }
            }

            result.Sort(namedCount, result.Count - namedCount, Comparer);
        }

        public static void CollectUnnamed(IReadOnlyList<string> order, List<IWelcomeStep> registered, List<string> result)
        {
            result.Clear();
            for (int i = 0; i < registered.Count; i++)
            {
                if (!Contains(order, registered[i].Id))
                {
                    result.Add(registered[i].Id);
                }
            }

            result.Sort(StringComparer.Ordinal);
        }

        private static IWelcomeStep Find(List<IWelcomeStep> registered, string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return null;
            }

            for (int i = 0; i < registered.Count; i++)
            {
                if (string.Equals(registered[i].Id, id, StringComparison.Ordinal))
                {
                    return registered[i];
                }
            }

            return null;
        }

        private static bool Contains(IReadOnlyList<string> order, string id)
        {
            if (order == null)
            {
                return false;
            }

            for (int i = 0; i < order.Count; i++)
            {
                if (string.Equals(order[i], id, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private sealed class OrdinalIdComparer : IComparer<IWelcomeStep>
        {
            public int Compare(IWelcomeStep x, IWelcomeStep y)
            {
                return string.CompareOrdinal(x.Id, y.Id);
            }
        }
    }
}
