using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using CozyGachapon.Contracts.PetCatalog;
using CozyGachapon.Core.PetCatalog;

namespace CozyGachapon.Tests.EditMode.PetCatalog
{
    [TestFixture]
    public sealed class PetCatalogValidatorTests
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
        public void Validate_AllMvpPets_HasZeroErrors()
        {
            var errors = PetCatalogValidator.Validate(mvpPets);
            Assert.That(errors, Is.Empty);
        }

        [Test]
        public void Validate_CatchesInvalidIdFormat()
        {
            var pet = ScriptableObject.CreateInstance<PetDefinition>();
            pet.SetData("Invalid_ID", "key", PetRarity.Common, PetGroup.Woodland, PetRole.GoldBooster, PetReleaseTier.Mvp,
                new[] { new PetEffect(PetEffectType.FlatGoldPerDrop, PetEffectScope.Self, 1.0) });

            var errors = PetCatalogValidator.Validate(new[] { pet });
            UnityEngine.Object.DestroyImmediate(pet);

            Assert.That(errors.Count, Is.GreaterThan(0));
            Assert.That(errors[0], Does.Contain("invalid ID format"));
        }

        [Test]
        public void Validate_CatchesDuplicateId()
        {
            var p1 = MvpPetFactory.CreateWoodyDuck();
            var p2 = MvpPetFactory.CreateWoodyDuck();

            var errors = PetCatalogValidator.Validate(new[] { p1, p2 });
            UnityEngine.Object.DestroyImmediate(p1);
            UnityEngine.Object.DestroyImmediate(p2);

            Assert.That(errors.Count, Is.GreaterThan(0));
            Assert.That(errors[0], Does.Contain("Duplicate pet ID"));
        }

        [Test]
        public void Validate_CatchesRoleMismatch()
        {
            var pet = ScriptableObject.CreateInstance<PetDefinition>();
            pet.SetData("pet.mismatch", "key", PetRarity.Common, PetGroup.Woodland, PetRole.GoldBooster, PetReleaseTier.Mvp,
                new[] { new PetEffect(PetEffectType.ScoreMultiplier, PetEffectScope.Self, 1.0) });

            var errors = PetCatalogValidator.Validate(new[] { pet });
            UnityEngine.Object.DestroyImmediate(pet);

            Assert.That(errors.Count, Is.GreaterThan(0));
            Assert.That(errors[0], Does.Contain("does not match its Role"));
        }

        [Test]
        public void Validate_CatchesScopeBothOnCommonOrRare()
        {
            var pet = ScriptableObject.CreateInstance<PetDefinition>();
            pet.SetData("pet.scope_both", "key", PetRarity.Common, PetGroup.Woodland, PetRole.GoldBooster, PetReleaseTier.Mvp,
                new[] { new PetEffect(PetEffectType.FlatGoldPerDrop, PetEffectScope.Both, 1.0) });

            var errors = PetCatalogValidator.Validate(new[] { pet });
            UnityEngine.Object.DestroyImmediate(pet);

            Assert.That(errors.Count, Is.GreaterThan(0));
            Assert.That(errors[0], Does.Contain("Scope.Both"));
        }
    }
}
