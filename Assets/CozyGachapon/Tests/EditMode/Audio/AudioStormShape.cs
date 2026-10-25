using CozyGachapon.Audio;

namespace CozyGachapon.Tests.Audio
{
    public readonly struct AudioStormShape
    {
        public double PegHitRate { get; }
        public double Seconds { get; }
        public double CoinPeriodSeconds { get; }
        public double UnscoredRatio { get; }
        public double JackpotIntervalSeconds { get; }

        public AudioStormShape(
            double pegHitRate,
            double seconds,
            double coinPeriodSeconds,
            double unscoredRatio,
            double jackpotIntervalSeconds)
        {
            PegHitRate = pegHitRate;
            Seconds = seconds;
            CoinPeriodSeconds = coinPeriodSeconds;
            UnscoredRatio = unscoredRatio;
            JackpotIntervalSeconds = jackpotIntervalSeconds;
        }

        public static AudioStormShape FromTargets(AudioBalanceTargets targets, double coinPeriodSeconds)
        {
            return new AudioStormShape(
                targets.StormPegHitRate,
                targets.StormTestSeconds,
                coinPeriodSeconds,
                targets.StormUnscoredPegHitRatio,
                targets.StormJackpotIntervalSeconds);
        }
    }
}
