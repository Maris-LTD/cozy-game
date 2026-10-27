using System;
using System.Globalization;
using CozyGachapon.Contracts.PetCatalog;

namespace CozyGachapon.Core.PetCatalog
{
    public static class PetCardDescriptionFormatter
    {
        public static string FormatEffect(PetEffect effect)
        {
            string magnitudeStr = FormatMagnitude(effect.Magnitude);

            switch (effect.Type)
            {
                case PetEffectType.FlatGoldPerDrop:
                    return $"+{magnitudeStr} Vàng mỗi lượt bóng rơi";

                case PetEffectType.PercentGoldBoost:
                    string percentStr = FormatMagnitude(effect.Magnitude * 100.0);
                    switch (effect.Scope)
                    {
                        case PetEffectScope.Self:
                            return $"+{percentStr}% Vàng bản thân";
                        case PetEffectScope.Group:
                            return $"+{percentStr}% Vàng nhóm {GetGroupName(effect.TargetGroup)}";
                        case PetEffectScope.Board:
                        case PetEffectScope.Both:
                        default:
                            return $"+{percentStr}% Vàng toàn bàn";
                    }

                case PetEffectType.ScoreMultiplier:
                    return $"x{magnitudeStr} Điểm thưởng";

                case PetEffectType.SpawnRateBoost:
                    string spawnPercent = FormatMagnitude(effect.Magnitude * 100.0);
                    return $"+{spawnPercent}% Tỉ lệ xuất hiện";

                case PetEffectType.SynergyCountBonus:
                    return $"+{magnitudeStr} Kích hoạt liên kết";

                default:
                    return string.Empty;
            }
        }

        public static string FormatMagnitude(double value)
        {
            double absValue = Math.Abs(value);

            if (absValue >= 1_000_000_000.0)
            {
                return (value / 1_000_000_000.0).ToString("0.##", CultureInfo.InvariantCulture) + "B";
            }
            if (absValue >= 1_000_000.0)
            {
                return (value / 1_000_000.0).ToString("0.##", CultureInfo.InvariantCulture) + "M";
            }
            if (absValue >= 1000.0)
            {
                return (value / 1000.0).ToString("0.##", CultureInfo.InvariantCulture) + "K";
            }

            if (Math.Abs(value - Math.Round(value)) < 0.0001)
            {
                return ((long)Math.Round(value)).ToString(CultureInfo.InvariantCulture);
            }

            return value.ToString("G3", CultureInfo.InvariantCulture);
        }

        public static string GetGroupName(PetGroup group)
        {
            switch (group)
            {
                case PetGroup.Woodland:
                    return "Rừng Rậm";
                case PetGroup.Aquatic:
                    return "Thuỷ Tộc";
                case PetGroup.Savanna:
                    return "Thảo Nguyên";
                case PetGroup.Sweets:
                    return "Đồ Ngọt";
                case PetGroup.Sky:
                    return "Bầu Trời";
                case PetGroup.Mythic:
                    return "Thần Thoại";
                default:
                    return string.Empty;
            }
        }
    }
}
