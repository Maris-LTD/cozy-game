using NUnit.Framework;
using CozyGachapon.Core.Inventory;

namespace CozyGachapon.Tests.EditMode.Inventory
{
    [TestFixture]
    public sealed class InventoryMathTests
    {
        [Test]
        public void AddCopiesClamped_ZeroBefore_ReturnsAmountAndIsNewTrue()
        {
            int after = InventoryMath.AddCopiesClamped(0, 1, out bool isNew);
            Assert.That(after, Is.EqualTo(1));
            Assert.That(isNew, Is.True);
        }

        [Test]
        public void AddCopiesClamped_ExistingCopies_ReturnsSumAndIsNewFalse()
        {
            int after = InventoryMath.AddCopiesClamped(4, 1, out bool isNew);
            Assert.That(after, Is.EqualTo(5));
            Assert.That(isNew, Is.False);
        }

        [Test]
        public void AddCopiesClamped_OverIntMax_ClampsToIntMax()
        {
            int after = InventoryMath.AddCopiesClamped(int.MaxValue - 1, 5, out bool isNew);
            Assert.That(after, Is.EqualTo(int.MaxValue));
            Assert.That(isNew, Is.False);

            int maxPlusMax = InventoryMath.AddCopiesClamped(int.MaxValue, int.MaxValue, out bool isNew2);
            Assert.That(maxPlusMax, Is.EqualTo(int.MaxValue));
            Assert.That(isNew2, Is.False);
        }

        [Test]
        public void FormatCopiesBadge_RespectsDisplayMax()
        {
            Assert.That(InventoryMath.FormatCopiesBadge(7, 99), Is.EqualTo("x7"));
            Assert.That(InventoryMath.FormatCopiesBadge(99, 99), Is.EqualTo("x99"));
            Assert.That(InventoryMath.FormatCopiesBadge(100, 99), Is.EqualTo("x99+"));
            Assert.That(InventoryMath.FormatCopiesBadge(int.MaxValue, 99), Is.EqualTo("x99+"));
        }

        [Test]
        public void CalculateCollectionProgress_HandlesZeroAndNormalCounts()
        {
            Assert.That(InventoryMath.CalculateCollectionProgress(0, 0), Is.EqualTo(0.0));
            Assert.That(InventoryMath.CalculateCollectionProgress(3, 7), Is.EqualTo(3.0 / 7.0).Within(1e-6));
            Assert.That(InventoryMath.CalculateCollectionProgress(7, 7), Is.EqualTo(1.0));
        }
    }
}
