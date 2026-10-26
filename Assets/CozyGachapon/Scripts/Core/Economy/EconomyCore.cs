using System;
using System.Collections.Generic;
using CozyGachapon.Contracts;

namespace CozyGachapon.Economy
{
    public sealed class EconomyCore : IWallet, ISpinPriceService, IBaseCoinValueSource
    {
        private readonly IEconomyDiagnosticsSink _sink;
        private readonly List<string> _registeredIds = new List<string>();
        private readonly PreservedBalanceList _preserved = new PreservedBalanceList();
        private readonly EconomyLifecycle _lifecycle = new EconomyLifecycle();
        private WalletService _wallet;
        private SpinPriceCurve _curve;
        private BaseCoinValueCalculator _baseCoinValueCalculator;
        private SpinPriceState _spinPriceState;
        private BaseCoinValueSource _baseCoinValueSource;
        private ShopResetter _shopResetter;
        private IncomePipeline _incomePipeline;
        private PlinkoCreditHandler _plinkoCreditHandler;
        private SubmitIncomeHandler _submitIncomeHandler;
        private IdleAccrual _idleAccrual;
        private AdIdleCompensator _adCompensator;
        private IncomeRatesPublisher _ratesPublisher;

        public event Action<WalletChangedEvent> WalletChanged;
        public event Action<SpendRejectedEvent> SpendRejected;
        public event Action<SpinPriceChangedEvent> SpinPriceChanged;
        public event Action<ShopEconomyResetEvent> ShopEconomyReset;
        public event Action<IncomeCreditedEvent> IncomeCredited;
        public event Action<IncomeRatesChangedEvent> IncomeRatesChanged;

        public EconomyCore(IEconomyDiagnosticsSink sink, ISimulationState simulationState = null)
        {
            _sink = sink ?? throw new ArgumentNullException(nameof(sink));
            if (simulationState != null)
            {
                _lifecycle.SetSimulationState(simulationState);
            }
        }

        public bool IsInitialized { get; private set; }
        public EconomyRunState RunState => _lifecycle.State;
        public int SpinsThisShop => _spinPriceState != null ? _spinPriceState.SpinsThisShop : 0;
        public double BaseCoinValue => _baseCoinValueSource != null ? _baseCoinValueSource.BaseCoinValue : 1d;
        public double SpinPrice => _spinPriceState != null ? _spinPriceState.SpinPrice : 0d;
        public double PlinkoRateAvg => _incomePipeline != null ? _incomePipeline.PlinkoRateAvg : 0d;
        public IIncomePipeline IncomePipeline => _incomePipeline;
        internal PlinkoCreditHandler PlinkoCreditHandler => _plinkoCreditHandler;
        internal SubmitIncomeHandler SubmitIncomeHandler => _submitIncomeHandler;

        public void SetSimulationState(ISimulationState simulationState) => _lifecycle.SetSimulationState(simulationState);

        public bool Initialize(EconomyConfig config, ReleaseTier buildTier, EconomyStateData loaded, ISimulationState simulationState = null)
        {
            if (config == null || IsInitialized)
            {
                return false;
            }

            _wallet = new WalletService(config, _sink);
            _curve = new SpinPriceCurve(config);
            _baseCoinValueCalculator = new BaseCoinValueCalculator(config, _curve);
            _spinPriceState = new SpinPriceState(_curve, _baseCoinValueCalculator);
            _baseCoinValueSource = new BaseCoinValueSource(_spinPriceState);
            _shopResetter = new ShopResetter(config, _wallet, _spinPriceState, config.Currencies, buildTier);
            RegisterCurrencies(config, buildTier);
            _incomePipeline = new IncomePipeline(config, _sink);
            _incomePipeline.RegisterIncomeSource(EconomyIds.PlinkoCoinSource, IncomeChannel.Plinko, 0d);
            _plinkoCreditHandler = new PlinkoCreditHandler(_wallet, _incomePipeline, _baseCoinValueSource, config, _sink);
            _plinkoCreditHandler.IncomeCredited += RaiseIncomeCredited;
            _submitIncomeHandler = new SubmitIncomeHandler(_wallet, _incomePipeline, _sink);
            _submitIncomeHandler.IncomeCredited += RaiseIncomeCredited;
            _incomePipeline.SetSubmitHandler(_submitIncomeHandler);
            _idleAccrual = new IdleAccrual(_wallet, _incomePipeline, config, _lifecycle);
            _adCompensator = new AdIdleCompensator(_wallet, _incomePipeline, config, _lifecycle);
            _ratesPublisher = new IncomeRatesPublisher(_incomePipeline, config);
            _ratesPublisher.IncomeRatesChanged += RaiseIncomeRatesChanged;
            _incomePipeline.StateMutated += _ratesPublisher.MarkChanged;

            EconomyStateRestorer restorer = new EconomyStateRestorer(config, _sink);
            if (IsNewGame(loaded))
            {
                double starting = restorer.ClampBalance(EconomyIds.Coin, config.StartingCoins);
                _wallet.SetLoadedBalance(EconomyIds.Coin, starting);
                _spinPriceState.SetSpins(0);
            }
            else
            {
                restorer.Restore(loaded, _wallet, _preserved);
                int spins = restorer.ClampSpins(loaded.SpinsThisShop);
                _spinPriceState.SetSpins(spins);
            }

            _wallet.WalletChanged += RaiseWalletChanged;
            _wallet.SpendRejected += RaiseSpendRejected;
            _lifecycle.OnInitialized(simulationState);
            IsInitialized = true;
            return true;
        }

        public double Tick(double dt)
        {
            if (!IsInitialized || _idleAccrual == null)
            {
                return 0d;
            }

            double clampedDt = _idleAccrual.Tick(dt);
            _adCompensator?.RecordStep(clampedDt);
            _incomePipeline?.PlinkoRateWindow.Advance(clampedDt);
            _ratesPublisher?.Tick(clampedDt);
            return clampedDt;
        }

        public void HandleInteractive(in InteractiveEvent evt) { if (IsInitialized) _lifecycle.HandleInteractive(in evt); }
        public void HandlePaused(in PausedEvent evt) { if (IsInitialized) _lifecycle.HandlePaused(in evt); }
        public void HandleResumed(in ResumedEvent evt) { if (IsInitialized) _lifecycle.HandleResumed(in evt); }
        public double HandleAdClosed(in AdClosedEvent evt) =>
            IsInitialized && _adCompensator != null ? _adCompensator.HandleAdClosed(in evt) : 0d;

        public bool HandleCoinResolved(in CoinResolvedEvent evt) =>
            IsInitialized && _plinkoCreditHandler != null && _plinkoCreditHandler.Handle(in evt);

        public bool SubmitIncome(IncomeChannel channel, string sourceKey, double baseAmount) =>
            IsInitialized && _incomePipeline != null && _incomePipeline.SubmitIncome(channel, sourceKey, baseAmount);

        public double GetSourceIdleCps(string sourceKey) =>
            IsInitialized && _incomePipeline != null ? _incomePipeline.GetSourceIdleCps(sourceKey) : 0d;

        public double PreviewIdleCps(IIdlePlan plan) =>
            IsInitialized && _incomePipeline != null ? _incomePipeline.PreviewIdleCps(plan) : 0d;

        public double GetSpinPrice() => SpinPrice;

        public double GetDerivedPrice(double priceCoef) =>
            TryGetDerivedPrice(priceCoef, out double price) ? price : 0d;

        public bool TryGetDerivedPrice(double priceCoef, out double price)
        {
            price = 0d;
            return IsInitialized && DerivedPriceCalculator.TryCompute(_curve, priceCoef, SpinsThisShop, out price);
        }

        public bool CanAffordSpin() => CanAfford(EconomyIds.Coin, SpinPrice);

        public bool TryPaySpin(in PlayerActionContext context)
        {
            if (!IsInitialized)
            {
                return false;
            }

            double currentPrice = SpinPrice;
            if (!_wallet.TrySpend(EconomyIds.Coin, currentPrice, SpendReason.GachaSpin, in context))
            {
                return false;
            }

            _spinPriceState.AdvanceSpin();
            SpinPriceChanged?.Invoke(new SpinPriceChangedEvent(SpinsThisShop, SpinPrice, BaseCoinValue));
            return true;
        }

        public bool ResetForNewShop(in PlayerActionContext context)
        {
            if (!IsInitialized || _shopResetter == null)
            {
                return false;
            }

            return _shopResetter.ResetForNewShop(
                in context,
                RaiseWalletChanged,
                RaiseSpinPriceChanged,
                RaiseShopEconomyReset);
        }

        public bool IsCurrencyRegistered(string currencyId) => IsInitialized && _wallet.IsRegistered(currencyId);

        public EconomyStateData ExportState()
        {
            if (!IsInitialized)
            {
                return null;
            }

            return EconomyStateExporter.Export(_registeredIds, _wallet, _preserved, SpinsThisShop);
        }

        public bool Add(string currencyId, double amount, WalletChangeReason reason) =>
            IsInitialized && _wallet.Add(currencyId, amount, reason);

        public bool TrySpend(string currencyId, double amount, SpendReason reason, in PlayerActionContext context) =>
            IsInitialized && _wallet.TrySpend(currencyId, amount, reason, in context);

        public bool CanAfford(string currencyId, double amount) =>
            IsInitialized && _wallet.CanAfford(currencyId, amount);

        public double GetBalance(string currencyId) => IsInitialized ? _wallet.GetBalance(currencyId) : 0d;

        private static bool IsNewGame(EconomyStateData loaded) =>
            loaded == null || loaded.Balances == null || loaded.Balances.Count == 0;

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

        private void RaiseWalletChanged(WalletChangedEvent change) => WalletChanged?.Invoke(change);
        private void RaiseSpendRejected(SpendRejectedEvent rejection) => SpendRejected?.Invoke(rejection);
        private void RaiseSpinPriceChanged(SpinPriceChangedEvent change) => SpinPriceChanged?.Invoke(change);
        private void RaiseShopEconomyReset(ShopEconomyResetEvent reset) => ShopEconomyReset?.Invoke(reset);
        private void RaiseIncomeRatesChanged(IncomeRatesChangedEvent rates) => IncomeRatesChanged?.Invoke(rates);

        private void RaiseIncomeCredited(IncomeCreditedEvent credited)
        {
            _incomePipeline?.PlinkoRateWindow.RecordCredit(credited.CreditedAmount);
            _ratesPublisher?.MarkChanged();
            IncomeCredited?.Invoke(credited);
        }
    }
}
