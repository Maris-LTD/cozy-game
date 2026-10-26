using System;
using CozyGachapon.Contracts;

namespace CozyGachapon.Economy
{
    public sealed class ModifierBook
    {
        private const int DefaultCapacity = 16;

        private IncomeModifier[] _modifiers = new IncomeModifier[DefaultCapacity];

        public int Count { get; private set; }
        public int Capacity => _modifiers.Length;

        public ref readonly IncomeModifier At(int index)
        {
            return ref _modifiers[index];
        }

        public void Add(in IncomeModifier modifier)
        {
            int existingIndex = FindMatchingIndex(in modifier);
            if (existingIndex >= 0)
            {
                _modifiers[existingIndex] = modifier;
                return;
            }

            if (Count == _modifiers.Length)
            {
                Array.Resize(ref _modifiers, _modifiers.Length * 2);
            }

            _modifiers[Count] = modifier;
            Count++;
        }

        public int RemoveBySourceId(string sourceId)
        {
            if (string.IsNullOrEmpty(sourceId))
            {
                return 0;
            }

            int write = 0;
            for (int read = 0; read < Count; read++)
            {
                if (string.Equals(_modifiers[read].SourceId, sourceId, StringComparison.Ordinal))
                {
                    continue;
                }

                if (write != read)
                {
                    _modifiers[write] = _modifiers[read];
                }

                write++;
            }

            int removed = Count - write;
            for (int i = write; i < Count; i++)
            {
                _modifiers[i] = default;
            }

            Count = write;
            return removed;
        }

        private int FindMatchingIndex(in IncomeModifier modifier)
        {
            for (int i = 0; i < Count; i++)
            {
                ref readonly IncomeModifier existing = ref _modifiers[i];
                if (IsSameTarget(in existing, in modifier))
                {
                    return i;
                }
            }

            return -1;
        }

        private static bool IsSameTarget(in IncomeModifier a, in IncomeModifier b)
        {
            if (!string.Equals(a.SourceId, b.SourceId, StringComparison.Ordinal))
            {
                return false;
            }

            if (a.Tier != b.Tier || a.Channel != b.Channel || a.Target != b.Target)
            {
                return false;
            }

            string keyA = a.TargetKey ?? string.Empty;
            string keyB = b.TargetKey ?? string.Empty;
            return string.Equals(keyA, keyB, StringComparison.Ordinal);
        }
    }
}
