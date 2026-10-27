using System;
using UnityEngine;

namespace CozyGachapon.Contracts.PetCatalog
{
    [Serializable]
    public struct PetEffect : IEquatable<PetEffect>
    {
        [SerializeField] private PetEffectType type;
        [SerializeField] private PetEffectScope scope;
        [SerializeField] private double magnitude;
        [SerializeField] private PetGroup targetGroup;

        public PetEffect(PetEffectType type, PetEffectScope scope, double magnitude, PetGroup targetGroup = PetGroup.None)
        {
            this.type = type;
            this.scope = scope;
            this.magnitude = magnitude;
            this.targetGroup = targetGroup;
        }

        public PetEffectType Type => type;
        public PetEffectScope Scope => scope;
        public double Magnitude => magnitude;
        public PetGroup TargetGroup => targetGroup;

        public bool Equals(PetEffect other)
        {
            return type == other.type &&
                   scope == other.scope &&
                   magnitude.Equals(other.magnitude) &&
                   targetGroup == other.targetGroup;
        }

        public override bool Equals(object obj)
        {
            return obj is PetEffect other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = (int)type;
                hash = (hash * 397) ^ (int)scope;
                hash = (hash * 397) ^ magnitude.GetHashCode();
                hash = (hash * 397) ^ (int)targetGroup;
                return hash;
            }
        }
    }
}
