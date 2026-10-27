using System;

namespace CozyGachapon.Contracts.Inventory
{
    public readonly struct AddPetResult : IEquatable<AddPetResult>
    {
        public bool IsAdded { get; }
        public bool IsNew { get; }
        public int Copies { get; }
        public AddPetRejectReason RejectReason { get; }

        private AddPetResult(bool isAdded, bool isNew, int copies, AddPetRejectReason rejectReason)
        {
            IsAdded = isAdded;
            IsNew = isNew;
            Copies = copies;
            RejectReason = rejectReason;
        }

        public static AddPetResult Added(bool isNew, int copies)
        {
            return new AddPetResult(true, isNew, copies, AddPetRejectReason.None);
        }

        public static AddPetResult Rejected(AddPetRejectReason reason)
        {
            return new AddPetResult(false, false, 0, reason);
        }

        public bool Equals(AddPetResult other)
        {
            return IsAdded == other.IsAdded &&
                   IsNew == other.IsNew &&
                   Copies == other.Copies &&
                   RejectReason == other.RejectReason;
        }

        public override bool Equals(object obj)
        {
            return obj is AddPetResult other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = IsAdded.GetHashCode();
                hash = (hash * 397) ^ IsNew.GetHashCode();
                hash = (hash * 397) ^ Copies;
                hash = (hash * 397) ^ (int)RejectReason;
                return hash;
            }
        }
    }
}
