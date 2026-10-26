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
    }
}
