using UnityEngine;

namespace CozyGachapon.Plinko
{
    public static class PlinkoPhysicsConstants
    {
        public const string CoinLayerName = "Coin";
        public const string PegLayerName = "Peg";
        public const string WallLayerName = "Wall";
        public const string DividerLayerName = "Divider";

        public static void ConfigureLayerCollisionMatrix()
        {
            int coinLayer = LayerMask.NameToLayer(CoinLayerName);
            int pegLayer = LayerMask.NameToLayer(PegLayerName);
            int wallLayer = LayerMask.NameToLayer(WallLayerName);
            int dividerLayer = LayerMask.NameToLayer(DividerLayerName);

            if (coinLayer >= 0)
            {
                Physics2D.IgnoreLayerCollision(coinLayer, coinLayer, true);
                if (pegLayer >= 0)
                {
                    Physics2D.IgnoreLayerCollision(coinLayer, pegLayer, false);
                }
                if (wallLayer >= 0)
                {
                    Physics2D.IgnoreLayerCollision(coinLayer, wallLayer, false);
                }
                if (dividerLayer >= 0)
                {
                    Physics2D.IgnoreLayerCollision(coinLayer, dividerLayer, false);
                }
            }
        }
    }
}
