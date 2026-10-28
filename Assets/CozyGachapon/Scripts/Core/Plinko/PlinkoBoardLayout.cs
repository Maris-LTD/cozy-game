using System.Collections.Generic;
using UnityEngine;

namespace CozyGachapon.Plinko
{
    public readonly struct PegLayoutInfo
    {
        public string PegId { get; }
        public int RowIndex { get; }
        public Vector2 Position { get; }
        public bool IsWallBump { get; }
        public float Radius { get; }

        public PegLayoutInfo(string pegId, int rowIndex, Vector2 position, bool isWallBump, float radius)
        {
            PegId = pegId;
            RowIndex = rowIndex;
            Position = position;
            IsWallBump = isWallBump;
            Radius = radius;
        }
    }

    public readonly struct BucketLayoutInfo
    {
        public int BucketIndex { get; }
        public float Multiplier { get; }
        public float MinX { get; }
        public float MaxX { get; }
        public float CenterX { get; }
        public float Width { get; }
        public bool IsJackpot { get; }

        public BucketLayoutInfo(int bucketIndex, float multiplier, float minX, float maxX, float centerX, float width, bool isJackpot)
        {
            BucketIndex = bucketIndex;
            Multiplier = multiplier;
            MinX = minX;
            MaxX = maxX;
            CenterX = centerX;
            Width = width;
            IsJackpot = isJackpot;
        }
    }

    public static class PlinkoBoardLayout
    {
        public static float CalculateInnerWidth(PlinkoConfig config)
        {
            return config.PegsPerRow * config.PegSpacing;
        }

        public static float CalculateLeftWallX(PlinkoConfig config)
        {
            return -CalculateInnerWidth(config) * 0.5f;
        }

        public static float CalculateRightWallX(PlinkoConfig config)
        {
            return CalculateInnerWidth(config) * 0.5f;
        }

        public static float CalculateRowSpacing(PlinkoConfig config)
        {
            return config.RowSpacingRatio * config.PegSpacing;
        }

        public static float CalculateRowY(int rowIndex, PlinkoConfig config)
        {
            return -rowIndex * CalculateRowSpacing(config);
        }

        public static List<PegLayoutInfo> GeneratePegs(PlinkoConfig config)
        {
            var pegs = new List<PegLayoutInfo>();
            float leftWall = CalculateLeftWallX(config);
            float rightWall = CalculateRightWallX(config);
            int n = config.PegsPerRow;
            float s = config.PegSpacing;

            for (int r = 0; r < config.PegRows; r++)
            {
                float y = CalculateRowY(r, config);
                if (r % 2 == 0)
                {
                    for (int k = 1; k <= n - 2; k++)
                    {
                        float x = (k - (n - 1) * 0.5f) * s;
                        pegs.Add(new PegLayoutInfo($"peg_{r}_{k}", r, new Vector2(x, y), false, config.PegRadius));
                    }

                    if (config.UseWallBumps)
                    {
                        pegs.Add(new PegLayoutInfo($"bump_{r}_left", r, new Vector2(leftWall, y), true, config.WallBumpRadius));
                        pegs.Add(new PegLayoutInfo($"bump_{r}_right", r, new Vector2(rightWall, y), true, config.WallBumpRadius));
                    }
                }
                else
                {
                    for (int k = 0; k <= n - 2; k++)
                    {
                        float x = (k - (n - 2) * 0.5f) * s;
                        pegs.Add(new PegLayoutInfo($"peg_{r}_{k}", r, new Vector2(x, y), false, config.PegRadius));
                    }
                }
            }

            return pegs;
        }

        public static float ComputeMinPassageGap(PlinkoConfig config)
        {
            float pegToPegGap = config.PegSpacing - 2f * config.PegRadius;
            float oddRowPegToWallGap = config.PegSpacing - config.PegRadius;
            float evenRowBumpGap = config.UseWallBumps
                ? 1.5f * config.PegSpacing - config.PegRadius - config.WallBumpRadius
                : 0.5f * config.PegSpacing - config.PegRadius;

            float dx = 0.5f * config.PegSpacing;
            float dy = config.RowSpacingRatio * config.PegSpacing;
            float diagonalGap = Mathf.Sqrt(dx * dx + dy * dy) - 2f * config.PegRadius;

            return Mathf.Min(Mathf.Min(pegToPegGap, oddRowPegToWallGap), Mathf.Min(evenRowBumpGap, diagonalGap));
        }

        public static List<BucketLayoutInfo> GenerateBuckets(PlinkoConfig config)
        {
            var buckets = new List<BucketLayoutInfo>();
            float[] multipliers = config.BucketMultipliers;
            float[] weights = config.BucketWidthWeights;
            if (multipliers == null || weights == null || multipliers.Length == 0 || multipliers.Length != weights.Length)
            {
                return buckets;
            }

            float totalWeight = 0f;
            for (int i = 0; i < weights.Length; i++)
            {
                totalWeight += weights[i];
            }

            if (totalWeight <= 0f)
            {
                return buckets;
            }

            float innerWidth = CalculateInnerWidth(config);
            float currentX = CalculateLeftWallX(config);

            for (int i = 0; i < multipliers.Length; i++)
            {
                float width = innerWidth * (weights[i] / totalWeight);
                float minX = currentX;
                float maxX = currentX + width;
                float centerX = (minX + maxX) * 0.5f;
                bool isJackpot = (i == 0 || i == multipliers.Length - 1);

                buckets.Add(new BucketLayoutInfo(i, multipliers[i], minX, maxX, centerX, width, isJackpot));
                currentX = maxX;
            }

            return buckets;
        }
    }
}

