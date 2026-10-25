using System.Collections.Generic;
using CozyGachapon.Contracts;

namespace CozyGachapon.Tests.Economy
{
    public sealed class EconomyEventRecorder
    {
        private readonly List<WalletChangedEvent> _events;
        private readonly List<SpendRejectedEvent> _rejected;

        public EconomyEventRecorder(int capacity)
        {
            _events = new List<WalletChangedEvent>(capacity);
            _rejected = new List<SpendRejectedEvent>(capacity);
        }

        public int Count => _events.Count;
        public WalletChangedEvent Last => _events[_events.Count - 1];
        public WalletChangedEvent this[int index] => _events[index];
        public int RejectedCount => _rejected.Count;
        public SpendRejectedEvent LastRejected => _rejected[_rejected.Count - 1];

        public void OnWalletChanged(WalletChangedEvent change)
        {
            _events.Add(change);
        }

        public void OnSpendRejected(SpendRejectedEvent rejection)
        {
            _rejected.Add(rejection);
        }
    }
}
