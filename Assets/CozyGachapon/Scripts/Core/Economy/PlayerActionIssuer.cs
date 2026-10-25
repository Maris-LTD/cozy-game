using CozyGachapon.Contracts;

namespace CozyGachapon.Economy
{
    public sealed class PlayerActionIssuer : IPlayerActionIssuer
    {
        private long _lastTicket;

        public PlayerActionContext Issue(PlayerActionKind kind)
        {
            if (!IsIssuable(kind))
            {
                return default;
            }

            _lastTicket++;
            return new PlayerActionContext(kind, _lastTicket);
        }

        private static bool IsIssuable(PlayerActionKind kind)
        {
            switch (kind)
            {
                case PlayerActionKind.GachaSpin:
                case PlayerActionKind.ShopPurchase:
                case PlayerActionKind.PrestigeUpgrade:
                case PlayerActionKind.NeighborCandy:
                case PlayerActionKind.StartNewShop:
                    return true;
                default:
                    return false;
            }
        }
    }
}
