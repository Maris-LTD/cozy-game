using System;
using System.Collections.Generic;
using UnityEngine;

namespace CozyGachapon.Economy
{
    [Serializable]
    public class TierOwnerPrefixes
    {
        [SerializeField] private string[] _base = { "shelf." };
        [SerializeField] private string[] _synergy = { "shelf." };
        [SerializeField] private string[] _boost = { "boost.", "ad." };
        [SerializeField] private string[] _prestige = { "prestige." };
        [SerializeField] private string[] _global = { "event." };

        public IReadOnlyList<string> Base => _base;
        public IReadOnlyList<string> Synergy => _synergy;
        public IReadOnlyList<string> Boost => _boost;
        public IReadOnlyList<string> Prestige => _prestige;
        public IReadOnlyList<string> Global => _global;
    }
}
