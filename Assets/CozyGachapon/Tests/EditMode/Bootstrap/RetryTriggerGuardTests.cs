using NUnit.Framework;

namespace CozyGachapon.Tests.Bootstrap
{
    public class RetryTriggerGuardTests
    {
        [Test]
        public void TryTrigger_FirstCall_ReturnsTrue()
        {
            RetryTriggerGuard guard = new RetryTriggerGuard();

            Assert.IsTrue(guard.TryTrigger());
            Assert.IsTrue(guard.IsTriggered);
        }

        [Test]
        public void TryTrigger_SecondCall_ReturnsFalse()
        {
            RetryTriggerGuard guard = new RetryTriggerGuard();
            guard.TryTrigger();

            Assert.IsFalse(guard.TryTrigger());
            Assert.IsFalse(guard.TryTrigger());
        }
    }
}
