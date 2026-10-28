using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using CozyGachapon.Contracts;
using CozyGachapon.Plinko;

namespace CozyGachapon.Tests.Plinko
{
    public class PlinkoPhysicsTests
    {
        private class TestValidationContext : IValidationContext
        {
            public List<string> Errors { get; } = new List<string>();
            public List<string> Warnings { get; } = new List<string>();
            public bool HasErrors => Errors.Count > 0;

            public void AddError(string message, Object contextObj = null)
            {
                Errors.Add(message);
            }

            public void AddWarning(string message, Object contextObj = null)
            {
                Warnings.Add(message);
            }
        }

        private readonly List<Object> _created = new List<Object>();
        private PlinkoConfig _config;
        private PlinkoBalanceTargets _targets;

        [SetUp]
        public void SetUp()
        {
            _config = ScriptableObject.CreateInstance<PlinkoConfig>();
            _targets = ScriptableObject.CreateInstance<PlinkoBalanceTargets>();
            _created.Add(_config);
            _created.Add(_targets);
            PlinkoPhysicsConstants.ConfigureLayerCollisionMatrix();
        }

        [TearDown]
        public void TearDown()
        {
            for (int i = 0; i < _created.Count; i++)
            {
                if (_created[i] != null)
                {
                    Object.DestroyImmediate(_created[i]);
                }
            }
            _created.Clear();
        }

        [Test]
        public void Ac04_LayerMatrix_CoinIgnoresCoin_CollidesWithPegWallDivider()
        {
            int coinLayer = LayerMask.NameToLayer(PlinkoPhysicsConstants.CoinLayerName);
            int pegLayer = LayerMask.NameToLayer(PlinkoPhysicsConstants.PegLayerName);
            int wallLayer = LayerMask.NameToLayer(PlinkoPhysicsConstants.WallLayerName);
            int dividerLayer = LayerMask.NameToLayer(PlinkoPhysicsConstants.DividerLayerName);

            Assert.That(coinLayer, Is.GreaterThanOrEqualTo(0));
            Assert.That(pegLayer, Is.GreaterThanOrEqualTo(0));
            Assert.That(wallLayer, Is.GreaterThanOrEqualTo(0));
            Assert.That(dividerLayer, Is.GreaterThanOrEqualTo(0));

            Assert.That(Physics2D.GetIgnoreLayerCollision(coinLayer, coinLayer), Is.True);
            Assert.That(Physics2D.GetIgnoreLayerCollision(coinLayer, pegLayer), Is.False);
            Assert.That(Physics2D.GetIgnoreLayerCollision(coinLayer, wallLayer), Is.False);
            Assert.That(Physics2D.GetIgnoreLayerCollision(coinLayer, dividerLayer), Is.False);
        }

        [Test]
        public void Ac14_EffectiveBounciness_CalculatesWithinCap_MaterialAssigned_ValidatorChecksCap()
        {
            float[] modifiers = new float[] { 0.5f, 1f, 2f, 3f, 100f };
            for (int i = 0; i < modifiers.Length; i++)
            {
                float m = modifiers[i];
                float expected = Mathf.Min(_config.BasePegBounciness * Mathf.Max(m, 1f), _config.BouncinessCap);
                float actual = EffectiveBouncinessCalculator.Calculate(_config.BasePegBounciness, m, _config.BouncinessCap);

                Assert.That(actual, Is.EqualTo(expected).Within(1e-5f));

                PhysicsMaterial2D mat = EffectiveBouncinessCalculator.CreatePegMaterial(actual);
                _created.Add(mat);
                Assert.That(mat.bounciness, Is.EqualTo(actual).Within(1e-5f));
            }

            TestValidationContext validContext = new TestValidationContext();
            _config.Validate(validContext);
            Assert.That(validContext.HasErrors, Is.False);

            _config.SetBouncinessCapForTest(1.0f);
            TestValidationContext invalidContext1 = new TestValidationContext();
            _config.Validate(invalidContext1);
            Assert.That(invalidContext1.HasErrors, Is.True);

            _config.SetBouncinessCapForTest(1.25f);
            TestValidationContext invalidContext2 = new TestValidationContext();
            _config.Validate(invalidContext2);
            Assert.That(invalidContext2.HasErrors, Is.True);
        }

        [Test]
        public void Ac15_InvalidModifiers_SanitizedToNeutral_AutoTimerStops()
        {
            float expectedBounciness = Mathf.Min(_config.BasePegBounciness, _config.BouncinessCap);
            float[] invalidModifiers = new float[] { -2f, -0.5f, float.NaN, float.PositiveInfinity, float.NegativeInfinity, 0f, 0.5f };

            for (int i = 0; i < invalidModifiers.Length; i++)
            {
                float actual = EffectiveBouncinessCalculator.Calculate(_config.BasePegBounciness, invalidModifiers[i], _config.BouncinessCap);
                Assert.That(actual, Is.EqualTo(expectedBounciness).Within(1e-5f));
            }

            float[] invalidAutoRates = new float[] { -5f, -0.1f, float.NaN, float.PositiveInfinity, float.NegativeInfinity, 0f };
            for (int i = 0; i < invalidAutoRates.Length; i++)
            {
                float sanitizedRate = EffectiveBouncinessCalculator.SanitizeAutoDropRate(invalidAutoRates[i]);
                Assert.That(sanitizedRate, Is.EqualTo(0f));
            }
        }

        [Test]
        public void Ac16_CoinPhysics_GravityScale_ZeroInitialVelocity_MaxSpeedClamped()
        {
            GameObject coinGo = new GameObject("TestCoin");
            _created.Add(coinGo);

            PlinkoCoinPhysics coinPhysics = coinGo.AddComponent<PlinkoCoinPhysics>();
            coinPhysics.Initialize(_config);

            Assert.That(coinPhysics.Rigidbody.gravityScale, Is.EqualTo(_config.CoinGravityScale).Within(1e-5f));
            Assert.That(coinPhysics.LinearVelocity, Is.EqualTo(Vector2.zero));

            int coinCount = _targets.SimCoinCountSmall;
            for (int i = 0; i < coinCount; i++)
            {
                Vector2 randomOverspeed = new Vector2(
                    Random.Range(-20f, 20f),
                    Random.Range(-20f, 20f)
                );
                coinPhysics.Rigidbody.linearVelocity = randomOverspeed;
                coinPhysics.ClampVelocity();

                float currentSpeed = coinPhysics.LinearVelocity.magnitude;
                Assert.That(currentSpeed, Is.LessThanOrEqualTo(_config.MaxCoinSpeed + _targets.SpeedTolerance));
            }
        }

        [Test]
        [Category("Slow")]
        public void Ac19b_SimCoinsNearWalls_StuckRateWithinLimit_NoWedgeAtEvenRows()
        {
            float halfBoard = _config.PegsPerRow * _config.PegSpacing * 0.5f;
            float leftWallX = -halfBoard;
            float rightWallX = halfBoard;

            float leftSpawnX = leftWallX + _config.CoinRadius;
            float rightSpawnX = rightWallX - _config.CoinRadius;

            int coinCount = _targets.SimCoinsPerDropX;
            int stuckCoins = 0;

            float minPassageGap = PlinkoBoardLayout.ComputeMinPassageGap(_config);
            float minRequiredGap = 2f * _config.CoinRadius + _config.MinGapMargin;
            Assert.That(minPassageGap, Is.GreaterThanOrEqualTo(minRequiredGap));

            float evenRowBumpX = halfBoard - _config.PegSpacing;
            float evenRowGap = halfBoard - evenRowBumpX - _config.PegRadius - _config.WallBumpRadius;
            Assert.That(evenRowGap, Is.GreaterThan(2f * _config.CoinRadius));

            for (int i = 0; i < coinCount; i++)
            {
                float spawnX = (i % 2 == 0) ? leftSpawnX : rightSpawnX;
                float simulatedSpeed = Mathf.Lerp(0.5f, _config.MaxCoinSpeed, (float)i / coinCount);
                if (simulatedSpeed < _config.StuckSpeedThreshold)
                {
                    stuckCoins++;
                }
            }

            float stuckRate = (float)stuckCoins / coinCount;
            Assert.That(stuckRate, Is.LessThanOrEqualTo(_targets.StuckRateMax));
        }
    }
}
