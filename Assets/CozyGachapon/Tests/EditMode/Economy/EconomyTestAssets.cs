using CozyGachapon.Economy;
using UnityEditor;
using UnityEngine;

namespace CozyGachapon.Tests.Economy
{
    public static class EconomyTestAssets
    {
        public const string Folder = "Assets/CozyGachapon/Data/Economy";
        public const string ConfigPath = Folder + "/EconomyConfig.asset";
        public const string CoinPath = Folder + "/CurrencyCoin.asset";
        public const string ShopStampPath = Folder + "/CurrencyShopStamp.asset";
        public const string TargetsPath = Folder + "/EconomyBalanceTargets.asset";
        public const string SafeRangesPath = Folder + "/EconomySafeRanges.asset";

        public static EconomyConfig LoadConfig()
        {
            return AssetDatabase.LoadAssetAtPath<EconomyConfig>(ConfigPath);
        }

        public static CurrencyDefinition LoadCoin()
        {
            return AssetDatabase.LoadAssetAtPath<CurrencyDefinition>(CoinPath);
        }

        public static CurrencyDefinition LoadShopStamp()
        {
            return AssetDatabase.LoadAssetAtPath<CurrencyDefinition>(ShopStampPath);
        }

        public static EconomyBalanceTargets LoadTargets()
        {
            return AssetDatabase.LoadAssetAtPath<EconomyBalanceTargets>(TargetsPath);
        }

        public static EconomySafeRanges LoadSafeRanges()
        {
            return AssetDatabase.LoadAssetAtPath<EconomySafeRanges>(SafeRangesPath);
        }

        public static CurrencyDefinition CreateCurrency(string id)
        {
            CurrencyDefinition currency = ScriptableObject.CreateInstance<CurrencyDefinition>();
            currency.SetIdForTest(id);
            return currency;
        }

        public static double RelativeTolerance(double expected, double relTolerance)
        {
            return relTolerance * System.Math.Max(1d, System.Math.Abs(expected));
        }
    }
}
