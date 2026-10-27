using NUnit.Framework;
using CozyGachapon.Contracts.PetCatalog;
using CozyGachapon.Core.PetCatalog;

namespace CozyGachapon.Tests.EditMode.PetCatalog
{
    [TestFixture]
    public sealed class PetCardDescriptionFormatterTests
    {
        [Test]
        public void FormatMagnitude_SmallNumbers_ShowsUpTo3SignificantDigits()
        {
            Assert.That(PetCardDescriptionFormatter.FormatMagnitude(1.0), Is.EqualTo("1"));
            Assert.That(PetCardDescriptionFormatter.FormatMagnitude(2.5), Is.EqualTo("2.5"));
            Assert.That(PetCardDescriptionFormatter.FormatMagnitude(10.0), Is.EqualTo("10"));
            Assert.That(PetCardDescriptionFormatter.FormatMagnitude(0.15), Is.EqualTo("0.15"));
        }

        [Test]
        public void FormatMagnitude_LargeNumbers_UsesSuffixes()
        {
            Assert.That(PetCardDescriptionFormatter.FormatMagnitude(1000.0), Is.EqualTo("1K"));
            Assert.That(PetCardDescriptionFormatter.FormatMagnitude(1500.0), Is.EqualTo("1.5K"));
            Assert.That(PetCardDescriptionFormatter.FormatMagnitude(2_000_000.0), Is.EqualTo("2M"));
            Assert.That(PetCardDescriptionFormatter.FormatMagnitude(3_500_000_000.0), Is.EqualTo("3.5B"));
        }

        [Test]
        public void FormatEffect_FlatGoldPerDrop()
        {
            var effect = new PetEffect(PetEffectType.FlatGoldPerDrop, PetEffectScope.Self, 1.0);
            string desc = PetCardDescriptionFormatter.FormatEffect(effect);
            Assert.That(desc, Is.EqualTo("+1 Vàng mỗi lượt bóng rơi"));
        }

        [Test]
        public void FormatEffect_ScoreMultiplier()
        {
            var effect = new PetEffect(PetEffectType.ScoreMultiplier, PetEffectScope.Self, 1.2);
            string desc = PetCardDescriptionFormatter.FormatEffect(effect);
            Assert.That(desc, Is.EqualTo("x1.2 Điểm thưởng"));
        }

        [Test]
        public void FormatEffect_PercentGoldBoost_Scopes()
        {
            var effectSelf = new PetEffect(PetEffectType.PercentGoldBoost, PetEffectScope.Self, 0.10);
            Assert.That(PetCardDescriptionFormatter.FormatEffect(effectSelf), Is.EqualTo("+10% Vàng bản thân"));

            var effectGroup = new PetEffect(PetEffectType.PercentGoldBoost, PetEffectScope.Group, 0.15, PetGroup.Woodland);
            Assert.That(PetCardDescriptionFormatter.FormatEffect(effectGroup), Is.EqualTo("+15% Vàng nhóm Rừng Rậm"));

            var effectBoard = new PetEffect(PetEffectType.PercentGoldBoost, PetEffectScope.Board, 0.50);
            Assert.That(PetCardDescriptionFormatter.FormatEffect(effectBoard), Is.EqualTo("+50% Vàng toàn bàn"));
        }

        [Test]
        public void FormatEffect_SpawnRateBoost()
        {
            var effect = new PetEffect(PetEffectType.SpawnRateBoost, PetEffectScope.Board, 0.25);
            string desc = PetCardDescriptionFormatter.FormatEffect(effect);
            Assert.That(desc, Is.EqualTo("+25% Tỉ lệ xuất hiện"));
        }

        [Test]
        public void FormatEffect_SynergyCountBonus()
        {
            var effect = new PetEffect(PetEffectType.SynergyCountBonus, PetEffectScope.Both, 2.0, PetGroup.Savanna);
            string desc = PetCardDescriptionFormatter.FormatEffect(effect);
            Assert.That(desc, Is.EqualTo("+2 Kích hoạt liên kết"));
        }
    }
}
