using System.Collections.Generic;
using CozyGachapon.Contracts;
using CozyGachapon.Pooling;
using NUnit.Framework;

namespace CozyGachapon.Tests.Pooling
{
    public class PoolPrewarmValidatorTests : PoolingTestFixture
    {
        private const int TargetFps = 60;

        private readonly List<PoolConfigIssue> _issues = new List<PoolConfigIssue>();

        [SetUp]
        public void ClearIssues()
        {
            _issues.Clear();
        }

        private int CountCode(PoolDiagnosticCode code)
        {
            int count = 0;
            for (int i = 0; i < _issues.Count; i++)
            {
                if (_issues[i].Code == code)
                {
                    count++;
                }
            }

            return count;
        }

        [TestCase(0)]
        [TestCase(-2)]
        public void AC7_PerFrameBelowOne_ReportsOneError(int perFrame)
        {
            Config.SetPrewarmForTest(perFrame, 0.25f);

            PoolingConfigValidator.Validate(Config, _issues);

            Assert.AreEqual(1, _issues.Count);
            Assert.AreEqual(PoolDiagnosticCode.PrewarmPerFrameInvalid, _issues[0].Code);
            Assert.IsTrue(_issues[0].IsError);
        }

        [Test]
        public void AC7_PerFrameTen_ReportsNoPerFrameError()
        {
            Config.SetPrewarmForTest(10, 0.25f);

            PoolingConfigValidator.Validate(Config, _issues);

            Assert.AreEqual(0, CountCode(PoolDiagnosticCode.PrewarmPerFrameInvalid));
        }

        [Test]
        public void AC8_OverBudget_ReportsWarning()
        {
            Config.SetPrewarmForTest(4, 0.25f);

            PoolingConfigValidator.ValidatePrewarmBudget(Config, 128, TargetFps, _issues);

            Assert.AreEqual(1, _issues.Count);
            Assert.AreEqual(PoolDiagnosticCode.PrewarmBudgetExceeded, _issues[0].Code);
            Assert.IsFalse(_issues[0].IsError);
        }

        [TestCase(40, 10)]
        [TestCase(41, 10)]
        public void AC8_MvpShapedConfig_ReportsNothing(int total, int perFrame)
        {
            Config.SetPrewarmForTest(perFrame, 0.25f);

            PoolingConfigValidator.ValidatePrewarmBudget(Config, total, TargetFps, _issues);

            Assert.AreEqual(0, _issues.Count);
        }

        [Test]
        public void AC8_ExactlyAtThreshold_ReportsNothing()
        {
            Config.SetPrewarmForTest(1, 0.25f);

            PoolingConfigValidator.ValidatePrewarmBudget(Config, 15, TargetFps, _issues);

            Assert.AreEqual(0, _issues.Count);
        }

        [Test]
        public void AC8_OneFrameOverThreshold_ReportsWarning()
        {
            Config.SetPrewarmForTest(1, 0.25f);

            PoolingConfigValidator.ValidatePrewarmBudget(Config, 16, TargetFps, _issues);

            Assert.AreEqual(1, _issues.Count);
        }

        [Test]
        public void AC8_PerFrameZeroWithLargeTotal_BudgetUsesOne_NoException()
        {
            Config.SetPrewarmForTest(0, 0.25f);

            Assert.DoesNotThrow(() => PoolingConfigValidator.ValidatePrewarmBudget(Config, 200, TargetFps, _issues));

            Assert.AreEqual(1, CountCode(PoolDiagnosticCode.PrewarmBudgetExceeded));
        }

        [Test]
        public void PrewarmBudget_Frames_RoundUp()
        {
            Assert.AreEqual(4, PrewarmBudget.Frames(40, 10));
            Assert.AreEqual(5, PrewarmBudget.Frames(41, 10));
            Assert.AreEqual(0, PrewarmBudget.Frames(0, 10));
            Assert.AreEqual(40, PrewarmBudget.Frames(40, 0));
        }

        [Test]
        public void PrewarmBudget_EffectivePerFrame_ClampsToOne()
        {
            Assert.AreEqual(1, PrewarmBudget.EffectivePerFrame(0));
            Assert.AreEqual(1, PrewarmBudget.EffectivePerFrame(-5));
            Assert.AreEqual(7, PrewarmBudget.EffectivePerFrame(7));
        }

        [Test]
        public void PrewarmBudget_InvalidFps_NeverExceeds()
        {
            Assert.IsFalse(PrewarmBudget.ExceedsBudget(1000, 1, 0, 0.25f));
            Assert.AreEqual(0f, PrewarmBudget.EstimatedSeconds(10, 0));
        }
    }
}
