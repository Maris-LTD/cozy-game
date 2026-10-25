using System.Reflection;
using CozyGachapon.Contracts;
using CozyGachapon.Economy;
using NUnit.Framework;

namespace CozyGachapon.Tests.Economy
{
    public class PlayerActionContextTests
    {
        private const int UndefinedKindValue = 999;
        private PlayerActionIssuer _issuer;

        [SetUp]
        public void SetUp()
        {
            _issuer = new PlayerActionIssuer();
        }

        [Test]
        public void Default_IsNotValidAndGateRejectsIt()
        {
            PlayerActionContext context = default;

            Assert.IsFalse(context.IsValid);
            Assert.IsFalse(PlayerActionGate.IsValid(context));
            Assert.AreEqual(PlayerActionKind.None, context.Kind);
        }

        [Test]
        public void Issue_GachaSpin_IsValidWithKindAndPositiveTicket()
        {
            PlayerActionContext context = _issuer.Issue(PlayerActionKind.GachaSpin);

            Assert.IsTrue(context.IsValid);
            Assert.IsTrue(PlayerActionGate.IsValid(context));
            Assert.AreEqual(PlayerActionKind.GachaSpin, context.Kind);
            Assert.Greater(context.Ticket, 0L);
        }

        [TestCase(PlayerActionKind.GachaSpin)]
        [TestCase(PlayerActionKind.ShopPurchase)]
        [TestCase(PlayerActionKind.PrestigeUpgrade)]
        [TestCase(PlayerActionKind.NeighborCandy)]
        [TestCase(PlayerActionKind.StartNewShop)]
        public void Issue_EveryDefinedKind_IsValid(PlayerActionKind kind)
        {
            PlayerActionContext context = _issuer.Issue(kind);

            Assert.IsTrue(context.IsValid);
            Assert.AreEqual(kind, context.Kind);
        }

        [Test]
        public void Issue_Twice_TicketsDifferAndIncrease()
        {
            PlayerActionContext first = _issuer.Issue(PlayerActionKind.GachaSpin);
            PlayerActionContext second = _issuer.Issue(PlayerActionKind.GachaSpin);

            Assert.AreNotEqual(first.Ticket, second.Ticket);
            Assert.Greater(second.Ticket, first.Ticket);
        }

        [Test]
        public void Issue_NoneOrUndefinedKind_IsNotValid()
        {
            Assert.IsFalse(_issuer.Issue(PlayerActionKind.None).IsValid);
            Assert.IsFalse(_issuer.Issue((PlayerActionKind)UndefinedKindValue).IsValid);
        }

        [Test]
        public void Issue_RejectedKind_DoesNotConsumeTicket()
        {
            _issuer.Issue(PlayerActionKind.None);

            PlayerActionContext context = _issuer.Issue(PlayerActionKind.GachaSpin);

            Assert.AreEqual(1L, context.Ticket);
        }

        [Test]
        public void Type_HasNoPublicConstructor()
        {
            ConstructorInfo[] constructors = typeof(PlayerActionContext).GetConstructors(BindingFlags.Public | BindingFlags.Instance);

            Assert.AreEqual(0, constructors.Length);
        }
    }
}
