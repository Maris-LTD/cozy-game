using System;
using CozyGachapon.Contracts;

namespace CozyGachapon.Economy
{
    public sealed class SubmitIncomeHandler
    {
        private readonly IWallet _wallet;
        private readonly IIncomePipeline _pipeline;
        private readonly IEconomyDiagnosticsSink _sink;

        public event Action<IncomeCreditedEvent> IncomeCredited;

        public SubmitIncomeHandler(
            IWallet wallet,
            IIncomePipeline pipeline,
            IEconomyDiagnosticsSink sink = null)
        {
            _wallet = wallet ?? throw new ArgumentNullException(nameof(wallet));
            _pipeline = pipeline ?? throw new ArgumentNullException(nameof(pipeline));
            _sink = sink;
        }

        public bool Submit(IncomeChannel channel, string sourceKey, double baseAmount)
        {
            if (channel != IncomeChannel.Plinko
                || string.IsNullOrEmpty(sourceKey)
                || !IncomeInputGuard.IsValidSubmitAmount(baseAmount))
            {
                _sink?.ReportWarning(
                    EconomyDiagnosticCode.InvalidIncomeInput,
                    sourceKey ?? string.Empty,
                    baseAmount);
                return false;
            }

            double mSynergy = _pipeline.GetSynergyMultiplier(sourceKey);
            double mBoost = _pipeline.GetTierMultiplier(ModifierTier.Boost, IncomeChannel.Plinko);
            double mPrestige = _pipeline.GetTierMultiplier(ModifierTier.Prestige, IncomeChannel.Plinko);
            double mGlobal = _pipeline.GetTierMultiplier(ModifierTier.Global, IncomeChannel.Plinko);

            double credit = baseAmount * mSynergy * mBoost * mPrestige * mGlobal;
            if (double.IsNaN(credit) || double.IsInfinity(credit) || credit < 0d)
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
                IncomeCreditedEvent.NoCoinId,
                IncomeCreditedEvent.NoBucket,
                CoinOrigin.None,
                CoinResolveReason.None,
                sourceKey,
                baseAmount,
                delta);

            IncomeCredited?.Invoke(creditedEvt);
            return true;
        }
    }
}
