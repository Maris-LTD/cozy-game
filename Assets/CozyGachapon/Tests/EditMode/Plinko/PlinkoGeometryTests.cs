using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using CozyGachapon.Contracts;
using CozyGachapon.Plinko;

namespace CozyGachapon.Tests.Plinko
{
    public class PlinkoGeometryTests
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
        private TestValidationContext _context;

        [SetUp]
        public void SetUp()
        {
            _config = Track(ScriptableObject.CreateInstance<PlinkoConfig>());
            _targets = Track(ScriptableObject.CreateInstance<PlinkoBalanceTargets>());
            _context = new TestValidationContext();
        }

        [TearDown]
        public void TearDown()
        {
            for (int i = 0; i < _created.Count; i++)
            {
                Object.DestroyImmediate(_created[i]);
            }

            _created.Clear();
        }

        private T Track<T>(T obj) where T : Object
        {
            _created.Add(obj);
            return obj;
        }

        [Test]
        public void GeneratePegs_DefaultConfig_SatisfiesGeometryFormulasAndCounts()
        {
            float tol = _targets.GeometryTolerance;
            float leftWall = PlinkoBoardLayout.CalculateLeftWallX(_config);
            float rightWall = PlinkoBoardLayout.CalculateRightWallX(_config);
            float expectedInnerWidth = _config.PegsPerRow * _config.PegSpacing;

            Assert.AreEqual(-expectedInnerWidth * 0.5f, leftWall, tol);
            Assert.AreEqual(expectedInnerWidth * 0.5f, rightWall, tol);

            float rowSpacing = PlinkoBoardLayout.CalculateRowSpacing(_config);
            Assert.AreEqual(_config.RowSpacingRatio * _config.PegSpacing, rowSpacing, tol);

            List<PegLayoutInfo> pegs = PlinkoBoardLayout.GeneratePegs(_config);
            Assert.AreEqual(_config.PegRows * 10 - 6, pegs.Count);

            int n = _config.PegsPerRow;
            float s = _config.PegSpacing;

            for (int r = 0; r < _config.PegRows; r++)
            {
                List<PegLayoutInfo> rowPegs = pegs.FindAll(p => p.RowIndex == r);
                float expectedY = -r * rowSpacing;

                if (r % 2 == 0)
                {
                    Assert.AreEqual(n, rowPegs.Count);
                    List<PegLayoutInfo> freePegs = rowPegs.FindAll(p => !p.IsWallBump);
                    List<PegLayoutInfo> bumps = rowPegs.FindAll(p => p.IsWallBump);

                    Assert.AreEqual(n - 2, freePegs.Count);
                    Assert.AreEqual(2, bumps.Count);

                    for (int k = 1; k <= n - 2; k++)
                    {
                        float expectedX = (k - (n - 1) * 0.5f) * s;
                        PegLayoutInfo peg = freePegs[k - 1];
                        Assert.AreEqual(expectedX, peg.Position.x, tol);
                        Assert.AreEqual(expectedY, peg.Position.y, tol);
                    }

                    Assert.AreEqual(leftWall, bumps[0].Position.x, tol);
                    Assert.AreEqual(rightWall, bumps[1].Position.x, tol);
                }
                else
                {
                    Assert.AreEqual(n - 1, rowPegs.Count);
                    for (int k = 0; k <= n - 2; k++)
                    {
                        float expectedX = (k - (n - 2) * 0.5f) * s;
                        PegLayoutInfo peg = rowPegs[k];
                        Assert.AreEqual(expectedX, peg.Position.x, tol);
                        Assert.AreEqual(expectedY, peg.Position.y, tol);
                        Assert.IsFalse(peg.IsWallBump);
                    }
                }
            }
        }

        [Test]
        public void GenerateBuckets_DefaultConfig_SatisfiesMultipliersAndTotalWidth()
        {
            float tol = _targets.GeometryTolerance;
            List<BucketLayoutInfo> buckets = PlinkoBoardLayout.GenerateBuckets(_config);

            Assert.AreEqual(5, buckets.Count);
            float totalWidth = 0f;
            float expectedInnerWidth = PlinkoBoardLayout.CalculateInnerWidth(_config);

            for (int i = 0; i < buckets.Count; i++)
            {
                BucketLayoutInfo bucket = buckets[i];
                Assert.AreEqual(i, bucket.BucketIndex);
                Assert.AreEqual(_config.BucketMultipliers[i], bucket.Multiplier);
                Assert.Greater(bucket.Width, 0f);
                Assert.AreEqual((bucket.MinX + bucket.MaxX) * 0.5f, bucket.CenterX, tol);
                totalWidth += bucket.Width;

                if (i == 0 || i == 4)
                {
                    Assert.IsTrue(bucket.IsJackpot);
                }
                else
                {
                    Assert.IsFalse(bucket.IsJackpot);
                }
            }

            Assert.AreEqual(expectedInnerWidth, totalWidth, tol);
        }

        [Test]
        public void Validate_DefaultConfig_HasNoErrors()
        {
            _config.Validate(_context);
            Assert.IsFalse(_context.HasErrors);
        }

        [Test]
        public void Validate_CoinRadiusTooLarge_ReportsPassageGapViolation()
        {
            _config.SetCoinRadiusForTest(0.20f);
            _config.Validate(_context);

            Assert.IsTrue(_context.HasErrors);
            Assert.IsTrue(_context.Errors.Exists(e => e.Contains("passage gap")));
        }

        [Test]
        public void Validate_OmittedWallBumps_ReportsEvenRowGapViolation()
        {
            _config.SetUseWallBumpsForTest(false);
            _config.Validate(_context);

            Assert.IsTrue(_context.HasErrors);
            Assert.IsTrue(_context.Errors.Exists(e => e.Contains("Even row peg to wall passage gap without wall bump")));
        }

        [Test]
        public void Validate_LowRowSpacingRatio_ReportsDiagonalGapViolation()
        {
            _config.SetRowSpacingRatioForTest(0.75f);
            _config.Validate(_context);

            Assert.IsTrue(_context.HasErrors);
            Assert.IsTrue(_context.Errors.Exists(e => e.Contains("Diagonal passage gap")));
        }

        [Test]
        public void Validate_ExcessiveSpeed_ReportsAntiTunnelingViolation()
        {
            _config.SetMaxCoinSpeedForTest(20f);
            _config.Validate(_context);

            Assert.IsTrue(_context.HasErrors);
            Assert.IsTrue(_context.Errors.Exists(e => e.Contains("Anti-tunneling")));
        }

        [Test]
        public void Validate_BouncinessCapTooHigh_ReportsError()
        {
            _config.SetBouncinessCapForTest(1.05f);
            _config.Validate(_context);

            Assert.IsTrue(_context.HasErrors);
            Assert.IsTrue(_context.Errors.Exists(e => e.Contains("Bounciness cap")));
        }
    }
}

