using System.Collections.Generic;
using CozyGachapon.Contracts;

namespace CozyGachapon.Tests.Economy
{
    public sealed class RecordingWalletEvents
    {
        public List<WalletChangedEvent> WalletChanges { get; } = new List<WalletChangedEvent>();
        public List<IncomeCreditedEvent> IncomeCredits { get; } = new List<IncomeCreditedEvent>();
        public List<SpendRejectedEvent> SpendRejections { get; } = new List<SpendRejectedEvent>();

        public void OnWalletChanged(WalletChangedEvent evt) => WalletChanges.Add(evt);
        public void OnIncomeCredited(IncomeCreditedEvent evt) => IncomeCredits.Add(evt);
        public void OnSpendRejected(SpendRejectedEvent evt) => SpendRejections.Add(evt);

        public void Clear()
        {
            WalletChanges.Clear();
            IncomeCredits.Clear();
            SpendRejections.Clear();
        }
    }
}
