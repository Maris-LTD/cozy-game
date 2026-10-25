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
        public event Action<SpendRejectedEvent> SpendRejected;

        public WalletService(EconomyConfig config, IEconomyDiagnosticsSink sink)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _sink = sink ?? throw new ArgumentNullException(nameof(sink));
        }

        internal bool RegisterCurrency(string currencyId)
        {
            return _ledger.TryRegister(currencyId);
        }

        internal bool IsRegistered(string currencyId)
        {
            return _ledger.TryGet(currencyId, out _);
        }

        internal void SetLoadedBalance(string currencyId, double amount)
        {
            if (_ledger.TryGet(currencyId, out _))
            {
                _ledger.Set(currencyId, amount);
            }
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

        public bool TrySpend(string currencyId, double amount, SpendReason reason, in PlayerActionContext context)
        {
            if (!PlayerActionGate.IsValid(context)
                || !IsValidAmount(amount)
                || !SpendReasonRules.TryToWalletChangeReason(reason, out WalletChangeReason walletReason)
                || !_ledger.TryGet(currencyId, out double previous))
            {
                _sink.ReportWarning(EconomyDiagnosticCode.RejectedSpend, currencyId, amount);
                return false;
            }

            if (!HasFunds(previous, amount))
            {
                SpendRejected?.Invoke(new SpendRejectedEvent(currencyId, amount, reason));
                return false;
            }

            double next = previous - amount;
            _ledger.Set(currencyId, next);
            WalletChanged?.Invoke(new WalletChangedEvent(currencyId, previous, next, -amount, walletReason));
            return true;
        }

        public bool CanAfford(string currencyId, double amount)
        {
            return IsValidAmount(amount)
                && _ledger.TryGet(currencyId, out double balance)
                && HasFunds(balance, amount);
        }

        public double GetBalance(string currencyId)
        {
            return _ledger.TryGet(currencyId, out double balance) ? balance : 0d;
        }

        private static bool HasFunds(double balance, double amount)
        {
            return balance >= amount;
        }

        private static bool IsValidAmount(double amount)
        {
            return amount > 0d && !double.IsInfinity(amount);
        }
    }
}
