using System;
using CozyGachapon.Contracts;

namespace CozyGachapon.Economy
{
    public sealed class PlinkoCreditHandler
    {
        private readonly IWallet _wallet;
        private readonly IIncomePipeline _pipeline;
        private readonly IBaseCoinValueSource _baseCoinValueSource;
        private readonly EconomyConfig _config;
        private readonly IEconomyDiagnosticsSink _sink;

        public event Action<IncomeCreditedEvent> IncomeCredited;

        public PlinkoCreditHandler(
            IWallet wallet,
            IIncomePipeline pipeline,
            IBaseCoinValueSource baseCoinValueSource,
            EconomyConfig config,
            IEconomyDiagnosticsSink sink = null)
        {
            _wallet = wallet ?? throw new ArgumentNullException(nameof(wallet));
            _pipeline = pipeline ?? throw new ArgumentNullException(nameof(pipeline));
            _baseCoinValueSource = baseCoinValueSource ?? throw new ArgumentNullException(nameof(baseCoinValueSource));
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _sink = sink;
        }

        public bool Handle(in CoinResolvedEvent evt)
        {
            if (!IncomeInputGuard.IsValidRawValue(evt.RawValue))
            {
                _sink?.ReportWarning(EconomyDiagnosticCode.InvalidIncomeInput, evt.CoinId, evt.RawValue);
                return false;
            }

            double flatUnits = _pipeline.GetFlatUnits(EconomyIds.PlinkoCoinSource);
            double baseCoinValue = _baseCoinValueSource.BaseCoinValue;
            double mSynergy = _pipeline.GetSynergyMultiplier(EconomyIds.PlinkoCoinSource);
            double mBoost = _pipeline.GetTierMultiplier(ModifierTier.Boost, IncomeChannel.Plinko);
            double mPrestige = _pipeline.GetTierMultiplier(ModifierTier.Prestige, IncomeChannel.Plinko);
            double mGlobal = _pipeline.GetTierMultiplier(ModifierTier.Global, IncomeChannel.Plinko);

            double credit = PlinkoCreditCalculator.Calculate(
                evt.RawValue,
                baseCoinValue,
                flatUnits,
                _config.PlinkoFlatPerCoinCap,
                mSynergy,
                mBoost,
                mPrestige,
                mGlobal);

            if (credit < 0d)
            {
                return false;
            }

            double delta = 0d;
            if (credit > 0d)
            {
                double previous = _wallet.GetBalance(EconomyIds.Coin);
                _wallet.Add(EconomyIds.Coin, credit, WalletChangeReason.PlinkoIncome);
                double current = _wallet.GetBalance(EconomyIds.Coin);
                delta = current - previous;
            }

            IncomeCreditedEvent creditedEvt = new IncomeCreditedEvent(
                IncomeChannel.Plinko,
                evt.CoinId,
                evt.BucketIndex,
                evt.Origin,
                evt.Reason,
                EconomyIds.PlinkoCoinSource,
                evt.RawValue,
                delta);

            IncomeCredited?.Invoke(creditedEvt);
            return true;
        }
    }
}
