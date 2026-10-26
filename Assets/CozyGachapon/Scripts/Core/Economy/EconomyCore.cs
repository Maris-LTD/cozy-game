using System;
using System.Collections.Generic;
using CozyGachapon.Contracts;

namespace CozyGachapon.Economy
{
    public sealed class EconomyCore : IWallet
    {
        private readonly IEconomyDiagnosticsSink _sink;
        private readonly List<string> _registeredIds = new List<string>();
        private readonly PreservedBalanceList _preserved = new PreservedBalanceList();
        private WalletService _wallet;
        private SpinPriceCurve _curve;
        private BaseCoinValueCalculator _baseCoinValueCalculator;
        private IncomePipeline _incomePipeline;

        public event Action<WalletChangedEvent> WalletChanged;
        public event Action<SpendRejectedEvent> SpendRejected;

        public EconomyCore(IEconomyDiagnosticsSink sink)
        {
            _sink = sink ?? throw new ArgumentNullException(nameof(sink));
            BaseCoinValue = 1d;
        }

        public bool IsInitialized { get; private set; }
        public int SpinsThisShop { get; private set; }
        public double BaseCoinValue { get; private set; }
        public double SpinPrice { get; private set; }
        public IIncomePipeline IncomePipeline => _incomePipeline;

        public bool Initialize(EconomyConfig config, ReleaseTier buildTier, EconomyStateData loaded)
        {
            if (config == null || IsInitialized)
            {
                return false;
            }

            _wallet = new WalletService(config, _sink);
            _curve = new SpinPriceCurve(config);
            _baseCoinValueCalculator = new BaseCoinValueCalculator(config, _curve);
            RegisterCurrencies(config, buildTier);
            _incomePipeline = new IncomePipeline(config);
            _incomePipeline.RegisterIncomeSource(EconomyIds.PlinkoCoinSource, IncomeChannel.Plinko, 0d);

            EconomyStateRestorer restorer = new EconomyStateRestorer(config, _sink);
            if (IsNewGame(loaded))
            {
                double starting = restorer.ClampBalance(EconomyIds.Coin, config.StartingCoins);
                _wallet.SetLoadedBalance(EconomyIds.Coin, starting);
                SpinsThisShop = 0;
            }
            else
            {
                restorer.Restore(loaded, _wallet, _preserved);
                SpinsThisShop = restorer.ClampSpins(loaded.SpinsThisShop);
            }

            SpinPrice = _curve.Price(SpinsThisShop);
            BaseCoinValue = _baseCoinValueCalculator.Compute(SpinsThisShop);
            _wallet.WalletChanged += RaiseWalletChanged;
            _wallet.SpendRejected += RaiseSpendRejected;
            IsInitialized = true;
            return true;
        }

        public bool IsCurrencyRegistered(string currencyId)
        {
            return IsInitialized && _wallet.IsRegistered(currencyId);
        }

        public EconomyStateData ExportState()
        {
            if (!IsInitialized)
            {
                return null;
            }

            return EconomyStateExporter.Export(_registeredIds, _wallet, _preserved, SpinsThisShop);
        }

        public bool Add(string currencyId, double amount, WalletChangeReason reason)
        {
            return IsInitialized && _wallet.Add(currencyId, amount, reason);
        }

        public bool TrySpend(string currencyId, double amount, SpendReason reason, in PlayerActionContext context)
        {
            return IsInitialized && _wallet.TrySpend(currencyId, amount, reason, in context);
        }

        public bool CanAfford(string currencyId, double amount)
        {
            return IsInitialized && _wallet.CanAfford(currencyId, amount);
        }

        public double GetBalance(string currencyId)
        {
            return IsInitialized ? _wallet.GetBalance(currencyId) : 0d;
        }

        private static bool IsNewGame(EconomyStateData loaded)
        {
            return loaded == null || loaded.Balances == null || loaded.Balances.Count == 0;
        }

        private void RegisterCurrencies(EconomyConfig config, ReleaseTier buildTier)
        {
            IReadOnlyList<CurrencyDefinition> currencies = config.Currencies;
            for (int i = 0; i < currencies.Count; i++)
            {
                CurrencyDefinition currency = currencies[i];
                if (currency == null || currency.ReleaseTier > buildTier)
                {
                    continue;
                }

                if (_wallet.RegisterCurrency(currency.Id))
                {
                    _registeredIds.Add(currency.Id);
                }
            }
        }

        private void RaiseWalletChanged(WalletChangedEvent change)
        {
            WalletChanged?.Invoke(change);
        }

        private void RaiseSpendRejected(SpendRejectedEvent rejection)
        {
            SpendRejected?.Invoke(rejection);
        }
    }
}
