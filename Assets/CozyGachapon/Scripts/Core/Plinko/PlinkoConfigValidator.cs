using UnityEngine;
using CozyGachapon.Contracts;

namespace CozyGachapon.Plinko
{
    public static class PlinkoConfigValidator
    {
        public const float DefaultFixedDeltaTime = 0.02f;

        public static void Validate(PlinkoConfig config, IValidationContext context, float fixedDeltaTime = DefaultFixedDeltaTime)
        {
            if (config == null)
            {
                context.AddError("PlinkoConfig is null");
                return;
            }

            ValidateAntiTunneling(config, context, fixedDeltaTime);
            ValidatePassageGaps(config, context);
            ValidatePhysicsParameters(config, context);
            ValidateBuckets(config, context);
            ValidateEconomyAndSlots(config, context);
        }

        private static void ValidateAntiTunneling(PlinkoConfig config, IValidationContext context, float fixedDeltaTime)
        {
            float maxDisplacement = config.MaxCoinSpeed * fixedDeltaTime;
            float contactThreshold = config.PegRadius + config.CoinRadius;
            if (maxDisplacement >= contactThreshold)
            {
                context.AddError($"Anti-tunneling violated: MaxCoinSpeed ({config.MaxCoinSpeed}) * fixedDeltaTime ({fixedDeltaTime}) = {maxDisplacement} >= PegRadius + CoinRadius ({contactThreshold})");
            }
        }

        private static void ValidatePassageGaps(PlinkoConfig config, IValidationContext context)
        {
            float minRequiredGap = 2f * config.CoinRadius + config.MinGapMargin;

            float pegToPegGap = config.PegSpacing - 2f * config.PegRadius;
            if (pegToPegGap < minRequiredGap)
            {
                context.AddError($"Peg to peg passage gap ({pegToPegGap}) is less than required ({minRequiredGap})");
            }

            float oddRowPegToWallGap = config.PegSpacing - config.PegRadius;
            if (oddRowPegToWallGap < minRequiredGap)
            {
                context.AddError($"Odd row peg to wall passage gap ({oddRowPegToWallGap}) is less than required ({minRequiredGap})");
            }

            if (config.UseWallBumps)
            {
                float evenRowBumpGap = 1.5f * config.PegSpacing - config.PegRadius - config.WallBumpRadius;
                if (evenRowBumpGap < minRequiredGap)
                {
                    context.AddError($"Even row peg to wall-bump passage gap ({evenRowBumpGap}) is less than required ({minRequiredGap})");
                }
            }
            else
            {
                float evenRowNoBumpGap = 0.5f * config.PegSpacing - config.PegRadius;
                if (evenRowNoBumpGap < minRequiredGap)
                {
                    context.AddError($"Even row peg to wall passage gap without wall bump ({evenRowNoBumpGap}) is less than required ({minRequiredGap})");
                }
            }

            float dx = 0.5f * config.PegSpacing;
            float dy = config.RowSpacingRatio * config.PegSpacing;
            float diagonalDistance = Mathf.Sqrt(dx * dx + dy * dy);
            float diagonalGap = diagonalDistance - 2f * config.PegRadius;
            if (diagonalGap < minRequiredGap)
            {
                context.AddError($"Diagonal passage gap between adjacent rows ({diagonalGap}) is less than required ({minRequiredGap})");
            }
        }

        private static void ValidatePhysicsParameters(PlinkoConfig config, IValidationContext context)
        {
            if (config.BouncinessCap >= 1.0f)
            {
                context.AddError($"Bounciness cap ({config.BouncinessCap}) must be strictly less than 1.0");
            }

            if (config.BasePegBounciness > config.BouncinessCap)
            {
                context.AddError($"Base peg bounciness ({config.BasePegBounciness}) cannot exceed bounciness cap ({config.BouncinessCap})");
            }
        }

        private static void ValidateBuckets(PlinkoConfig config, IValidationContext context)
        {
            if (config.BucketMultipliers == null || config.BucketMultipliers.Length != 5)
            {
                context.AddError("Bucket multipliers must contain exactly 5 elements");
            }

            if (config.BucketWidthWeights == null || config.BucketWidthWeights.Length != 5)
            {
                context.AddError("Bucket width weights must contain exactly 5 elements");
            }
        }

        private static void ValidateEconomyAndSlots(PlinkoConfig config, IValidationContext context)
        {
            if (config.BaseCoinValue < 1.0d || double.IsNaN(config.BaseCoinValue) || double.IsInfinity(config.BaseCoinValue))
            {
                context.AddError($"Base coin value fallback ({config.BaseCoinValue}) must be >= 1.0 and finite");
            }

            if (config.ManualReservedSlots >= config.MaxActiveCoins)
            {
                context.AddError($"Manual reserved slots ({config.ManualReservedSlots}) must be less than max active coins ({config.MaxActiveCoins})");
            }
        }
    }
}

