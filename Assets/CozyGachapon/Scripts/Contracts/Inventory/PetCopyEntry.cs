using System;
using UnityEngine;

namespace CozyGachapon.Contracts.Inventory
{
    [Serializable]
    public struct PetCopyEntry : IEquatable<PetCopyEntry>
    {
        [SerializeField] private string id;
        [SerializeField] private int copies;

        public PetCopyEntry(string id, int copies)
        {
            this.id = id;
            this.copies = copies;
        }

        public string Id => id;
        public int Copies => copies;

        public bool Equals(PetCopyEntry other)
        {
            return string.Equals(id, other.id, StringComparison.Ordinal) &&
                   copies == other.copies;
        }

        public override bool Equals(object obj)
        {
            return obj is PetCopyEntry other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((id != null ? StringComparer.Ordinal.GetHashCode(id) : 0) * 397) ^ copies;
            }
        }
    }
}
