using CozyGachapon.Contracts;
using CozyGachapon.Pooling;
using NUnit.Framework;

namespace CozyGachapon.Tests.Pooling
{
    public class PoolRegistrationRejectionTests : PoolingTestFixture
    {
        private const string PlinkoCoinId = "plinko.coin";
        private const string ExistingCapacityPoolId = "pool.valid";
        private const string RecyclePoolId = "pool.recycle";
        private const string NoPrefabPoolId = "pool.noprefab";

        private ObjectPoolService _service;
        private int _totalBefore;

        [SetUp]
        public void SetUpRejection()
        {
            Config.SetPoolsForTest(new[]
            {
                CreateDefinition(PlinkoCoinId, 0),
                CreateDefinition(ExistingCapacityPoolId, 0),
                CreateDefinition(RecyclePoolId, 0, PoolExhaustionPolicy.RecycleOldest),
                new PoolDefinition(NoPrefabPoolId, null, PoolExhaustionPolicy.Fixed, 0)
            });
            Config.SetLimitsForTest(64, 80);
            _service = CreateService();
            Assert.IsTrue(_service.Register(PlinkoCoinId, 24, true).Accepted);
            _totalBefore = _service.RegisteredCapacityTotal;
        }

        [Test]
        public void Register_UnknownId_RejectedWithUnknownPoolId()
        {
            AssertRejected(_service.Register("pool.unknown", 4, false), PoolRegistrationError.UnknownPoolId, PoolDiagnosticCode.RegistrationUnknownPoolId);
        }

        [Test]
        public void Register_NullId_RejectedWithUnknownPoolId()
        {
            AssertRejected(_service.Register(null, 4, false), PoolRegistrationError.UnknownPoolId, PoolDiagnosticCode.RegistrationUnknownPoolId);
        }

        [Test]
        public void Register_NullPrefab_RejectedWithMissingPrefab()
        {
            AssertRejected(_service.Register(NoPrefabPoolId, 4, false), PoolRegistrationError.MissingPrefab, PoolDiagnosticCode.RegistrationMissingPrefab);
        }

        [TestCase(0)]
        [TestCase(-1)]
        public void Register_DeclaredMaxBelowOne_RejectedWithInvalidDeclaredMax(int declared)
        {
            AssertRejected(_service.Register(ExistingCapacityPoolId, declared, false), PoolRegistrationError.InvalidDeclaredMax, PoolDiagnosticCode.RegistrationInvalidDeclaredMax);
        }

        [Test]
        public void Register_DeclaredIntMax_RejectedWithoutOverflow()
        {
            AssertRejected(_service.Register(ExistingCapacityPoolId, int.MaxValue, false), PoolRegistrationError.CapacityExceedsPerPoolLimit, PoolDiagnosticCode.RegistrationCapacityExceedsPerPoolLimit);
        }

        [Test]
        public void Register_CapacityOneAbovePerPoolLimit_RejectedBeforeTotalCheck()
        {
            AssertRejected(_service.Register(ExistingCapacityPoolId, 65, false), PoolRegistrationError.CapacityExceedsPerPoolLimit, PoolDiagnosticCode.RegistrationCapacityExceedsPerPoolLimit);
        }

        [Test]
        public void Register_CapacityExactlyAtPerPoolLimitButOverTotal_RejectedWithTotalExceeded()
        {
            AssertRejected(_service.Register(ExistingCapacityPoolId, 60, false), PoolRegistrationError.TotalCapacityExceeded, PoolDiagnosticCode.RegistrationTotalCapacityExceeded);
        }

        [Test]
        public void Register_TotalExactlyAtLimit_IsAccepted()
        {
            PoolRegistrationResult result = _service.Register(ExistingCapacityPoolId, 56, false);

            Assert.IsTrue(result.Accepted);
            Assert.AreEqual(80, _service.RegisteredCapacityTotal);
            Assert.AreEqual(0, Sink.ErrorCount);
        }

        [Test]
        public void Register_AlreadyRegisteredId_RejectedWithDuplicatePoolId()
        {
            AssertRejected(_service.Register(PlinkoCoinId, 24, true), PoolRegistrationError.DuplicatePoolId, PoolDiagnosticCode.RegistrationDuplicatePoolId);
        }

        [Test]
        public void Register_RecycleOldestWithForbidRecycle_RejectedWithRecycleForbidden()
        {
            AssertRejected(_service.Register(RecyclePoolId, 4, true), PoolRegistrationError.RecycleForbidden, PoolDiagnosticCode.RegistrationRecycleForbidden);
        }

        [Test]
        public void Register_RecycleOldestWithoutForbidRecycle_IsAccepted()
        {
            Assert.IsTrue(_service.Register(RecyclePoolId, 4, false).Accepted);
            Assert.AreEqual(0, Sink.ErrorCount);
        }

        private void AssertRejected(PoolRegistrationResult result, PoolRegistrationError expectedError, PoolDiagnosticCode expectedCode)
        {
            Assert.IsFalse(result.Accepted);
            Assert.AreEqual(expectedError, result.Error);
            Assert.AreEqual(1, Sink.ErrorCount);
            Assert.AreEqual(expectedCode, Sink.LastErrorCode);
            Assert.AreEqual(_totalBefore, _service.RegisteredCapacityTotal);
            Assert.IsTrue(_service.TryGetStats(PlinkoCoinId, out PoolStats plinkoStats));
            Assert.AreEqual(24, plinkoStats.Capacity);
            Assert.AreEqual(0, plinkoStats.Created);
            AssertNoObjectsCreated(ExistingCapacityPoolId);
            AssertNoObjectsCreated(RecyclePoolId);
            AssertNoObjectsCreated(NoPrefabPoolId);
            AssertNoObjectsCreated("pool.unknown");
        }

        private void AssertNoObjectsCreated(string id)
        {
            if (_service.TryGetStats(id, out PoolStats stats))
            {
                Assert.AreEqual(0, stats.Created);
            }
        }
    }
}
