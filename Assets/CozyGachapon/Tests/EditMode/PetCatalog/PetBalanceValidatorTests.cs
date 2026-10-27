using NUnit.Framework;
using UnityEngine;
using CozyGachapon.Contracts.PetCatalog;
using CozyGachapon.Core.PetCatalog;

namespace CozyGachapon.Tests.EditMode.PetCatalog
{
    [TestFixture]
    public sealed class PetBalanceValidatorTests
    {
        private PetDefinition[] mvpPets;

        [SetUp]
        public void SetUp()
        {
            mvpPets = MvpPetFactory.CreateAllMvpPets();
        }

        [TearDown]
        public void TearDown()
        {
            if (mvpPets != null)
            {
                for (int i = 0; i < mvpPets.Length; i++)
                {
                    if (mvpPets[i] != null)
                    {
                        UnityEngine.Object.DestroyImmediate(mvpPets[i]);
                    }
                }
            }
        }

        [Test]
        public void BasePower_Is10()
        {
            Assert.That(PetBalanceTargets.DefaultBasePower, Is.EqualTo(10.0));
        }

        [Test]
        public void TargetPowers_MatchFormulas()
        {
            Assert.That(PetBalanceTargets.GetTargetPower(PetRarity.Common, PetRole.GoldBooster), Is.EqualTo(10.0).Within(0.001));
            Assert.That(PetBalanceTargets.GetTargetPower(PetRarity.Rare, PetRole.Spawner), Is.EqualTo(27.5).Within(0.001));
            Assert.That(PetBalanceTargets.GetTargetPower(PetRarity.Epic, PetRole.Multiplier), Is.EqualTo(72.0).Within(0.001));
            Assert.That(PetBalanceTargets.GetTargetPower(PetRarity.Legendary, PetRole.SynergyEnabler), Is.EqualTo(195.0).Within(0.001));
        }

        [Test]
        public void ValidateBudgets_WithSufficientTolerance_ValidatesMvpPets()
        {
            var errors = PetBalanceValidator.ValidateBudgets(mvpPets, null, 0.45);
            Assert.That(errors, Is.Empty);
        }

        [Test]
        public void OutOfBudgetPet_IsDetected()
        {
            var overBudgetPet = ScriptableObject.CreateInstance<PetDefinition>();
            overBudgetPet.SetData("pet.overpowered", "key", PetRarity.Common, PetGroup.Woodland, PetRole.GoldBooster, PetReleaseTier.Mvp,
                new[] { new PetEffect(PetEffectType.FlatGoldPerDrop, PetEffectScope.Board, 1000.0) });

            var errors = PetBalanceValidator.ValidateBudgets(new[] { overBudgetPet });
            UnityEngine.Object.DestroyImmediate(overBudgetPet);

            Assert.That(errors.Count, Is.GreaterThan(0));
            Assert.That(errors[0], Does.Contain("out of balance budget"));
        }
    }
}
