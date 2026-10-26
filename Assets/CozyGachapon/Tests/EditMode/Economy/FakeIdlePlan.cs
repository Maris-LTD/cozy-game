using System.Collections.Generic;
using CozyGachapon.Contracts;

namespace CozyGachapon.Tests.Economy
{
    public sealed class FakeIdlePlan : IIdlePlan
    {
        private readonly List<IdlePlanSource> _sources = new List<IdlePlanSource>();
        private readonly List<IncomeModifier> _modifiers = new List<IncomeModifier>();

        public int SourceCount => _sources.Count;
        public int ModifierCount => _modifiers.Count;

        public void AddSource(string sourceKey, double baseRate, double flatUnits = 0d)
        {
            _sources.Add(new IdlePlanSource(sourceKey, baseRate, flatUnits));
        }

        public void AddModifier(in IncomeModifier modifier)
        {
            _modifiers.Add(modifier);
        }

        public IdlePlanSource GetSource(int index) => _sources[index];

        public IncomeModifier GetModifier(int index) => _modifiers[index];
    }
}
