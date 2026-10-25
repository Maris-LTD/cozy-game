using System.Collections.Generic;
using CozyGachapon.Contracts;

namespace CozyGachapon.Tests.Economy
{
    public sealed class EconomyEventRecorder
    {
        private readonly List<WalletChangedEvent> _events;

        public EconomyEventRecorder(int capacity)
        {
            _events = new List<WalletChangedEvent>(capacity);
        }

        public int Count => _events.Count;
        public WalletChangedEvent Last => _events[_events.Count - 1];
        public WalletChangedEvent this[int index] => _events[index];

        public void OnWalletChanged(WalletChangedEvent change)
        {
            _events.Add(change);
        }
    }
}
