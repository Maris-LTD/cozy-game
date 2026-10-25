using CozyGachapon.Contracts;
using NUnit.Framework;

namespace CozyGachapon.Tests.Audio
{
    public class PlayResultTests
    {
        [Test]
        public void Success_IsPlayed_WithReasonNone()
        {
            PlayResult result = PlayResult.Success();

            Assert.IsTrue(result.Played);
            Assert.AreEqual(DropReason.None, result.Reason);
        }

        [Test]
        public void Dropped_IsNotPlayed_WithGivenReason()
        {
            PlayResult result = PlayResult.Dropped(DropReason.RateLimited);

            Assert.IsFalse(result.Played);
            Assert.AreEqual(DropReason.RateLimited, result.Reason);
        }
    }
}
