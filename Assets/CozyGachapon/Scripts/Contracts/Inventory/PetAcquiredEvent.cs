using System;

namespace CozyGachapon.Contracts.Inventory
{
    public readonly struct PetAcquiredEvent : IEquatable<PetAcquiredEvent>
    {
        public string Id { get; }
        public bool IsNew { get; }
        public int Copies { get; }

        public PetAcquiredEvent(string id, bool isNew, int copies)
        {
            Id = id;
            IsNew = isNew;
            Copies = copies;
        }

        public bool Equals(PetAcquiredEvent other)
        {
            return string.Equals(Id, other.Id, StringComparison.Ordinal) &&
                   IsNew == other.IsNew &&
                   Copies == other.Copies;
        }

        public override bool Equals(object obj)
        {
            return obj is PetAcquiredEvent other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = Id != null ? StringComparer.Ordinal.GetHashCode(Id) : 0;
                hash = (hash * 397) ^ IsNew.GetHashCode();
                hash = (hash * 397) ^ Copies;
                return hash;
            }
        }
    }
}
