using System;
using System.Collections.Generic;
using CozyGachapon.Contracts;

namespace CozyGachapon.Economy
{
    public sealed class EconomyBootStep : IBootStep
    {
        public const string StepId = "economy.core";

        private readonly EconomyCore _core;
        private readonly EconomyConfig _config;
        private readonly ReleaseTier _buildTier;
        private readonly EconomyStateData _loaded;
        private readonly IServiceRegistry _serviceRegistry;
        private readonly ISimulationState _simulationState;

        public string Id => StepId;
        public BootPhase Phase => BootPhase.GameplayInit;
        public IReadOnlyList<string> RunsAfter => Array.Empty<string>();
        public bool IsCritical => true;
        public bool IsDone { get; private set; }
        public bool IsFailed { get; private set; }

        public EconomyBootStep(
            EconomyCore core,
            EconomyConfig config,
            IServiceRegistry serviceRegistry,
            ISimulationState simulationState = null,
            ReleaseTier buildTier = ReleaseTier.Mvp,
            EconomyStateData loaded = null)
        {
            _core = core ?? throw new ArgumentNullException(nameof(core));
            _config = config;
            _serviceRegistry = serviceRegistry;
            _simulationState = simulationState;
            _buildTier = buildTier;
            _loaded = loaded;
        }

        public void Run()
        {
            if (IsDone || IsFailed)
            {
                return;
            }

            try
            {
                if (!HasCoinCurrency(_config, _buildTier))
                {
                    IsFailed = true;
                    return;
                }

                if (!_core.Initialize(_config, _buildTier, _loaded, _simulationState)
                    || !_core.IsCurrencyRegistered(EconomyIds.Coin))
                {
                    IsFailed = true;
                    return;
                }

                if (_serviceRegistry != null)
                {
                    _serviceRegistry.Register<IBaseCoinValueSource>(_core);
                }

                IsDone = true;
            }
            catch (Exception)
            {
                IsFailed = true;
            }
        }

        private static bool HasCoinCurrency(EconomyConfig config, ReleaseTier buildTier)
        {
            if (config == null || config.Currencies == null)
            {
                return false;
            }

            IReadOnlyList<CurrencyDefinition> currencies = config.Currencies;
            for (int i = 0; i < currencies.Count; i++)
            {
                CurrencyDefinition currency = currencies[i];
                if (currency != null
                    && currency.ReleaseTier <= buildTier
                    && string.Equals(currency.Id, EconomyIds.Coin, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
