using System;
using System.Collections.Generic;
using CozyGachapon.Contracts;

namespace CozyGachapon.Economy
{
    internal sealed class EconomyStateRestorer
    {
        private readonly EconomyConfig _config;
        private readonly IEconomyDiagnosticsSink _sink;

        internal EconomyStateRestorer(EconomyConfig config, IEconomyDiagnosticsSink sink)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _sink = sink ?? throw new ArgumentNullException(nameof(sink));
        }

        internal double ClampBalance(string currencyId, double raw)
        {
            if (!(raw >= 0d))
            {
                _sink.ReportWarning(EconomyDiagnosticCode.LoadedBalanceClamped, currencyId, raw);
                return 0d;
            }

            double cap = _config.WalletSafeCap;
            if (raw > cap)
            {
                _sink.ReportWarning(EconomyDiagnosticCode.LoadedBalanceClamped, currencyId, raw);
                return cap;
            }

            return raw;
        }

        internal int ClampSpins(int raw)
        {
            if (raw < 0)
            {
                _sink.ReportWarning(EconomyDiagnosticCode.LoadedSpinsClamped, null, raw);
                return 0;
            }

            return raw;
        }

        internal void Restore(EconomyStateData loaded, WalletService wallet, PreservedBalanceList preserved)
        {
            IReadOnlyList<CurrencyBalanceEntry> balances = loaded.Balances;
            if (balances == null)
            {
                return;
            }

            for (int i = 0; i < balances.Count; i++)
            {
                CurrencyBalanceEntry entry = balances[i];
                if (string.IsNullOrEmpty(entry.CurrencyId))
                {
                    continue;
                }

                double amount = ClampBalance(entry.CurrencyId, entry.Amount);
                if (wallet.IsRegistered(entry.CurrencyId))
                {
                    wallet.SetLoadedBalance(entry.CurrencyId, amount);
                }
                else
                {
                    preserved.Set(entry.CurrencyId, amount);
                }
            }
        }
    }
}
