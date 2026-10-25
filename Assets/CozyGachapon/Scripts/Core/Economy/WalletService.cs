using System;
using CozyGachapon.Contracts;

namespace CozyGachapon.Economy
{
    public sealed class WalletService : IWallet
    {
        private readonly EconomyConfig _config;
        private readonly IEconomyDiagnosticsSink _sink;
        private readonly CurrencyLedger _ledger = new CurrencyLedger();

        public event Action<WalletChangedEvent> WalletChanged;

        public WalletService(EconomyConfig config, IEconomyDiagnosticsSink sink)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _sink = sink ?? throw new ArgumentNullException(nameof(sink));
        }

        internal bool RegisterCurrency(string currencyId)
        {
            return _ledger.TryRegister(currencyId);
        }

        public bool Add(string currencyId, double amount, WalletChangeReason reason)
        {
            if (!IsValidAmount(amount)
                || !WalletChangeReasonRules.IsFaucet(reason)
                || !_ledger.TryGet(currencyId, out double previous))
            {
                _sink.ReportWarning(EconomyDiagnosticCode.RejectedAdd, currencyId, amount);
                return false;
            }

            double next = Math.Min(previous + amount, _config.WalletSafeCap);
            double delta = next - previous;
            if (delta > 0d)
            {
                _ledger.Set(currencyId, next);
                WalletChanged?.Invoke(new WalletChangedEvent(currencyId, previous, next, delta, reason));
            }

            return true;
        }

        public double GetBalance(string currencyId)
        {
            return _ledger.TryGet(currencyId, out double balance) ? balance : 0d;
        }

        private static bool IsValidAmount(double amount)
        {
            return amount > 0d && !double.IsInfinity(amount);
        }
    }
}
