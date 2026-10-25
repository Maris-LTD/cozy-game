using CozyGachapon.Contracts;
using CozyGachapon.Pooling;
using NUnit.Framework;
using UnityEngine;

namespace CozyGachapon.Tests.Pooling
{
    public class PoolPrewarmTests : PoolingTestFixture
    {
        private const int StepGuard = 1000;

        private int RunToReady(ObjectPoolService service, int poolCount, int perFrameLimit, out int maxCreatedInOneStep)
        {
            int steps = 0;
            int previous = TotalCreated(service, poolCount);
            maxCreatedInOneStep = 0;
            while (service.Phase != PoolServicePhase.Ready && steps < StepGuard)
            {
                service.StepPrewarm();
                steps++;
                int total = TotalCreated(service, poolCount);
                maxCreatedInOneStep = Mathf.Max(maxCreatedInOneStep, total - previous);
                previous = total;
                Assert.LessOrEqual(maxCreatedInOneStep, perFrameLimit);
            }

            return steps;
        }

        [TestCase(40, 10, 4)]
        [TestCase(41, 10, 5)]
        [TestCase(40, 40, 1)]
        [TestCase(40, 4, 10)]
        public void AC6_StepCountMatchesCeil_AndEveryPoolFullyCreated(int total, int perFrame, int expectedSteps)
        {
            Config.SetPrewarmForTest(perFrame, 0.25f);
            ObjectPoolService service = CreateRegisteredService(total);
            service.BeginPrewarm(CreateRoot());

            int steps = RunToReady(service, 1, perFrame, out int maxPerStep);

            Assert.AreEqual(expectedSteps, steps);
            Assert.LessOrEqual(maxPerStep, perFrame);
            Assert.AreEqual(PoolServicePhase.Ready, service.Phase);
            service.TryGetStats(PoolId(0), out PoolStats stats);
            Assert.AreEqual(stats.Capacity, stats.Created);
            Assert.AreEqual(total, stats.Created);
        }

        [Test]
        public void AC6_TwoPoolsSummingToForty_StillFourSteps_AndEachPoolFull()
        {
            Config.SetPrewarmForTest(10, 0.25f);
            ObjectPoolService service = CreateRegisteredService(25, 15);
            service.BeginPrewarm(CreateRoot());

            int steps = RunToReady(service, 2, 10, out int maxPerStep);

            Assert.AreEqual(4, steps);
            Assert.AreEqual(10, maxPerStep);
            for (int i = 0; i < 2; i++)
            {
                service.TryGetStats(PoolId(i), out PoolStats stats);
                Assert.AreEqual(stats.Capacity, stats.Created);
            }
        }

        [Test]
        public void AC6_LastStepOfFortyOneByTen_CreatesOnlyOne()
        {
            Config.SetPrewarmForTest(10, 0.25f);
            ObjectPoolService service = CreateRegisteredService(41);
            service.BeginPrewarm(CreateRoot());

            for (int i = 0; i < 4; i++)
            {
                service.StepPrewarm();
            }

            Assert.AreEqual(40, TotalCreated(service, 1));
            Assert.AreEqual(PoolServicePhase.Prewarming, service.Phase);
            service.StepPrewarm();
            Assert.AreEqual(41, TotalCreated(service, 1));
            Assert.AreEqual(PoolServicePhase.Ready, service.Phase);
        }

        [Test]
        public void AC6_CreationFollowsRegistrationOrder()
        {
            Config.SetPrewarmForTest(4, 0.25f);
            ObjectPoolService service = CreateRegisteredService(6, 6);
            service.BeginPrewarm(CreateRoot());

            service.StepPrewarm();
            service.TryGetStats(PoolId(0), out PoolStats a1);
            service.TryGetStats(PoolId(1), out PoolStats b1);
            Assert.AreEqual(4, a1.Created);
            Assert.AreEqual(0, b1.Created);

            service.StepPrewarm();
            service.TryGetStats(PoolId(0), out PoolStats a2);
            service.TryGetStats(PoolId(1), out PoolStats b2);
            Assert.AreEqual(6, a2.Created);
            Assert.AreEqual(2, b2.Created);
        }

        [Test]
        public void AC6_PhaseStaysPrewarmingUntilLastStep()
        {
            Config.SetPrewarmForTest(10, 0.25f);
            ObjectPoolService service = CreateRegisteredService(40);
            service.BeginPrewarm(CreateRoot());

            Assert.AreEqual(PoolServicePhase.Prewarming, service.Phase);
            for (int i = 0; i < 3; i++)
            {
                service.StepPrewarm();
                Assert.AreEqual(PoolServicePhase.Prewarming, service.Phase);
            }

            service.StepPrewarm();
            Assert.AreEqual(PoolServicePhase.Ready, service.Phase);
        }

        [TestCase(0)]
        [TestCase(-3)]
        public void AC7_PerFrameBelowOne_UsesOne_AndFinishesInNSteps(int perFrame)
        {
            Config.SetPrewarmForTest(perFrame, 0.25f);
            ObjectPoolService service = CreateRegisteredService(5);
            service.BeginPrewarm(CreateRoot());

            int steps = 0;
            int maxPerStep = 0;
            Assert.DoesNotThrow(() => steps = RunToReady(service, 1, 1, out maxPerStep));

            Assert.AreEqual(5, steps);
            Assert.AreEqual(1, maxPerStep);
            Assert.AreEqual(0, Sink.ErrorCount);
        }

        [Test]
        public void BeginPrewarm_NoPools_ReadyImmediately_CreatesNothing()
        {
            ObjectPoolService service = CreateService();
            Transform root = CreateRoot();

            service.BeginPrewarm(root);

            Assert.AreEqual(PoolServicePhase.Ready, service.Phase);
            Assert.AreEqual(0, root.childCount);
        }

        [Test]
        public void BeginPrewarm_WhenNotAccepting_DoesNothing()
        {
            ObjectPoolService service = CreateRegisteredService(3);
            Transform root = CreateRoot();
            service.SetPhaseForTest(PoolServicePhase.Ready);

            service.BeginPrewarm(root);

            Assert.AreEqual(PoolServicePhase.Ready, service.Phase);
            Assert.AreEqual(0, root.childCount);
            Assert.AreEqual(0, TotalCreated(service, 1));
        }

        [Test]
        public void Register_AfterBeginPrewarm_RejectedWithWrongPhase()
        {
            ObjectPoolService service = CreateRegisteredService(3);
            service.BeginPrewarm(CreateRoot());

            PoolRegistrationResult result = service.Register(PoolId(0), 2, false);

            Assert.IsFalse(result.Accepted);
            Assert.AreEqual(PoolRegistrationError.WrongPhase, result.Error);
        }

        [Test]
        public void BeginPrewarm_NullRoot_FailsThroughSink_WithoutException()
        {
            ObjectPoolService service = CreateRegisteredService(3);

            Assert.DoesNotThrow(() => service.BeginPrewarm(null));
            Assert.DoesNotThrow(() => service.StepPrewarm());

            Assert.IsTrue(service.HasPrewarmFailed);
            Assert.AreEqual(PoolDiagnosticCode.PrewarmMissingContainer, Sink.LastErrorCode);
            Assert.AreEqual(0, TotalCreated(service, 1));
            Assert.AreNotEqual(PoolServicePhase.Ready, service.Phase);
        }
    }
}
