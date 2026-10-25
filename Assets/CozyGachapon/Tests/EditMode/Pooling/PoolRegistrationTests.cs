using CozyGachapon.Contracts;
using CozyGachapon.Pooling;
using NUnit.Framework;

namespace CozyGachapon.Tests.Pooling
{
    public class PoolRegistrationTests : PoolingTestFixture
    {
        private const string PlinkoCoinId = "plinko.coin";

        [TestCase(0, 24)]
        [TestCase(4, 28)]
        public void Register_PlinkoCoinFixed_CapacityIsDeclaredPlusHeadroom(int headroom, int expectedCapacity)
        {
            Config.SetPoolsForTest(new[] { CreateDefinition(PlinkoCoinId, headroom) });
            ObjectPoolService service = CreateService();

            PoolRegistrationResult result = service.Register(PlinkoCoinId, 24, true);

            Assert.IsTrue(result.Accepted);
            Assert.AreEqual(PoolRegistrationError.None, result.Error);
            Assert.AreEqual(expectedCapacity, result.Capacity);
            Assert.IsTrue(service.TryGetStats(PlinkoCoinId, out PoolStats stats));
            Assert.AreEqual(expectedCapacity, stats.Capacity);
            Assert.AreEqual(0, stats.Created);
            Assert.AreEqual(0, Sink.ErrorCount);
        }

        [TestCase(1, 0, 1)]
        [TestCase(24, 4, 28)]
        [TestCase(40, 8, 48)]
        [TestCase(10, 2, 12)]
        public void Register_DeclaredAndHeadroom_CapacityMatchesFormula(int declared, int headroom, int expected)
        {
            Config.SetPoolsForTest(new[] { CreateDefinition("pool.single", headroom) });
            ObjectPoolService service = CreateService();

            PoolRegistrationResult result = service.Register("pool.single", declared, false);

            Assert.IsTrue(result.Accepted);
            Assert.AreEqual(expected, result.Capacity);
            Assert.IsTrue(service.TryGetStats("pool.single", out PoolStats stats));
            Assert.AreEqual(expected, stats.Capacity);
        }

        [Test]
        public void Register_AllFourTablePoolsOnOneService_AllAcceptedAndTotalIsSum()
        {
            Config.SetPoolsForTest(new[]
            {
                CreateDefinition("pool.a", 0),
                CreateDefinition("pool.b", 4),
                CreateDefinition("pool.c", 8),
                CreateDefinition("pool.d", 2)
            });
            ObjectPoolService service = CreateService();

            Assert.AreEqual(1, service.Register("pool.a", 1, false).Capacity);
            Assert.AreEqual(28, service.Register("pool.b", 24, false).Capacity);
            Assert.AreEqual(48, service.Register("pool.c", 40, false).Capacity);
            Assert.AreEqual(12, service.Register("pool.d", 10, false).Capacity);
            Assert.AreEqual(89, service.RegisteredCapacityTotal);
            Assert.AreEqual(0, Sink.ErrorCount);
        }

        [TestCase(0, 0, 0)]
        [TestCase(24, 4, 28)]
        public void PoolCapacity_Compute_IsDeclaredPlusHeadroom(int declared, int headroom, int expected)
        {
            Assert.AreEqual(expected, PoolCapacity.Compute(declared, headroom));
        }

        [TestCase(PoolServicePhase.Prewarming)]
        [TestCase(PoolServicePhase.Ready)]
        public void Register_ServiceNotAccepting_RejectedWithOneErrorAndNothingCreated(PoolServicePhase phase)
        {
            Config.SetPoolsForTest(new[] { CreateDefinition(PlinkoCoinId, 0) });
            ObjectPoolService service = CreateService();
            service.SetPhaseForTest(phase);

            PoolRegistrationResult result = service.Register(PlinkoCoinId, 24, true);

            Assert.IsFalse(result.Accepted);
            Assert.AreEqual(PoolRegistrationError.WrongPhase, result.Error);
            Assert.AreEqual(1, Sink.ErrorCount);
            Assert.AreEqual(PoolDiagnosticCode.RegistrationWrongPhase, Sink.LastErrorCode);
            Assert.AreEqual(0, service.RegisteredCapacityTotal);
            Assert.IsFalse(service.TryGetStats(PlinkoCoinId, out _));
        }

        [Test]
        public void Register_SameIdWhileAccepting_IsAcceptedAsControl()
        {
            Config.SetPoolsForTest(new[] { CreateDefinition(PlinkoCoinId, 0) });
            ObjectPoolService service = CreateService();

            PoolRegistrationResult result = service.Register(PlinkoCoinId, 24, true);

            Assert.IsTrue(result.Accepted);
            Assert.AreEqual(PoolServicePhase.Accepting, service.Phase);
            Assert.AreEqual(0, Sink.ErrorCount);
        }
    }
}
