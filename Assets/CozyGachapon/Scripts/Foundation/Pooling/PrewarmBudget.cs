namespace CozyGachapon.Pooling
{
    public static class PrewarmBudget
    {
        private const double Tolerance = 1e-6;

        public static int EffectivePerFrame(int perFrame)
        {
            return perFrame < 1 ? 1 : perFrame;
        }

        public static int Frames(int total, int perFrame)
        {
            if (total <= 0)
            {
                return 0;
            }

            int effective = EffectivePerFrame(perFrame);
            return (total + effective - 1) / effective;
        }

        public static float EstimatedSeconds(int frames, int targetFps)
        {
            if (targetFps < 1)
            {
                return 0f;
            }

            return (float)frames / targetFps;
        }

        public static bool ExceedsBudget(int total, int perFrame, int targetFps, float maxSeconds)
        {
            if (targetFps < 1)
            {
                return false;
            }

            double frames = Frames(total, perFrame);
            return frames > (double)maxSeconds * targetFps + Tolerance;
        }
    }
}
