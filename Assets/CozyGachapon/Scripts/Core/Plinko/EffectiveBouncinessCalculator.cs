using UnityEngine;

namespace CozyGachapon.Plinko
{
    public static class EffectiveBouncinessCalculator
    {
        public static float SanitizeBouncinessModifier(float modifier)
        {
            if (float.IsNaN(modifier) || float.IsInfinity(modifier) || modifier <= 0f)
            {
                return 1f;
            }

            if (modifier < 1f)
            {
                return 1f;
            }

            return modifier;
        }

        public static float SanitizeAutoDropRate(float rate)
        {
            if (float.IsNaN(rate) || float.IsInfinity(rate) || rate <= 0f)
            {
                return 0f;
            }

            return rate;
        }

        public static float Calculate(float basePegBounciness, float bouncinessModifier, float bouncinessCap)
        {
            float sanitizedMod = SanitizeBouncinessModifier(bouncinessModifier);
            float uncapped = basePegBounciness * sanitizedMod;
            return Mathf.Min(uncapped, bouncinessCap);
        }

        public static PhysicsMaterial2D CreatePegMaterial(float effectiveBounciness)
        {
            PhysicsMaterial2D material = new PhysicsMaterial2D("PegPhysicsMaterial")
            {
                bounciness = effectiveBounciness,
                friction = 0f
            };
            return material;
        }

        private static PhysicsMaterial2D _cachedCoinMaterial;

        public static PhysicsMaterial2D GetOrCreateCoinMaterial()
        {
            if (_cachedCoinMaterial == null)
            {
                _cachedCoinMaterial = new PhysicsMaterial2D("CoinPhysicsMaterial")
                {
                    bounciness = 0f,
                    friction = 0f
                };
            }
            return _cachedCoinMaterial;
        }

        public static PhysicsMaterial2D CreateCoinMaterial()
        {
            return GetOrCreateCoinMaterial();
        }
    }
}
