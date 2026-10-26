using System;
using System.Collections.Generic;
using CozyGachapon.Contracts;

namespace CozyGachapon.Economy
{
    internal sealed class ShopResetter
    {
        private readonly EconomyConfig _config;
        private readonly WalletService _wallet;
        private readonly SpinPriceState _spinPriceState;
        private readonly CurrencyDefinition[] _resetCurrencies;
        private readonly string[] _resetCurrencyIds;

        public ShopResetter(
            EconomyConfig config,
            WalletService wallet,
            SpinPriceState spinPriceState,
            IReadOnlyList<CurrencyDefinition> currencies,
            ReleaseTier buildTier)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _wallet = wallet ?? throw new ArgumentNullException(nameof(wallet));
            _spinPriceState = spinPriceState ?? throw new ArgumentNullException(nameof(spinPriceState));

            int count = 0;
            if (currencies != null)
            {
                for (int i = 0; i < currencies.Count; i++)
                {
                    CurrencyDefinition currency = currencies[i];
                    if (currency != null && currency.ResetsOnNewShop && currency.ReleaseTier <= buildTier)
                    {
                        count++;
                    }
                }
            }

            _resetCurrencies = new CurrencyDefinition[count];
            _resetCurrencyIds = new string[count];

            int index = 0;
            if (currencies != null)
            {
                for (int i = 0; i < currencies.Count; i++)
                {
                    CurrencyDefinition currency = currencies[i];
                    if (currency != null && currency.ResetsOnNewShop && currency.ReleaseTier <= buildTier)
                    {
                        _resetCurrencies[index] = currency;
                        _resetCurrencyIds[index] = currency.Id;
                        index++;
                    }
                }
            }
        }

        public bool ResetForNewShop(
            in PlayerActionContext context,
            Action<WalletChangedEvent> raiseWalletChanged,
            Action<SpinPriceChangedEvent> raiseSpinPriceChanged,
            Action<ShopEconomyResetEvent> raiseShopReset)
        {
            if (!PlayerActionGate.IsValid(in context))
            {
                return false;
            }

            for (int i = 0; i < _resetCurrencies.Length; i++)
            {
                CurrencyDefinition currency = _resetCurrencies[i];
                double target = string.Equals(currency.Id, EconomyIds.Coin, StringComparison.Ordinal)
                    ? _config.StartingCoins
                    : 0d;

                if (_wallet.TryResetBalance(currency.Id, target, out WalletChangedEvent changeEvent))
                {
                    raiseWalletChanged?.Invoke(changeEvent);
                }
            }

            _spinPriceState.Reset();
            raiseSpinPriceChanged?.Invoke(new SpinPriceChangedEvent(_spinPriceState.SpinsThisShop, _spinPriceState.SpinPrice, _spinPriceState.BaseCoinValue));
            raiseShopReset?.Invoke(new ShopEconomyResetEvent(_resetCurrencyIds, 0));
            return true;
        }
    }
}
