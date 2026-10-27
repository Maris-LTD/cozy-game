using System;
using System.Collections.Generic;
using CozyGachapon.Contracts.PetCatalog;
using CozyGachapon.Core.PetCatalog;

namespace CozyGachapon.Core.Inventory
{
    public sealed class PetDisplayComparer : IComparer<string>
    {
        private readonly IPetDatabase catalog;
        private readonly bool rarityOrderDescending;

        public PetDisplayComparer(IPetDatabase catalog, bool rarityOrderDescending)
        {
            this.catalog = catalog;
            this.rarityOrderDescending = rarityOrderDescending;
        }

        public int Compare(string x, string y)
        {
            if (ReferenceEquals(x, y))
            {
                return 0;
            }
            if (x == null)
            {
                return -1;
            }
            if (y == null)
            {
                return 1;
            }

            bool hasX = catalog != null && catalog.TryGet(x, out var petX) && petX != null;
            bool hasY = catalog != null && catalog.TryGet(y, out var petY) && petY != null;

            if (hasX && hasY)
            {
                catalog.TryGet(x, out var px);
                catalog.TryGet(y, out var py);

                int rarityCmp = rarityOrderDescending
                    ? ((int)py.Rarity).CompareTo((int)px.Rarity)
                    : ((int)px.Rarity).CompareTo((int)py.Rarity);

                if (rarityCmp != 0)
                {
                    return rarityCmp;
                }

                int groupCmp = ((int)px.Group).CompareTo((int)py.Group);
                if (groupCmp != 0)
                {
                    return groupCmp;
                }
            }

            return string.CompareOrdinal(x, y);
        }
    }
}
