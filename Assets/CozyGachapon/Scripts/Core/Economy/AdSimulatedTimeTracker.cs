namespace CozyGachapon.Economy
{
    public sealed class AdSimulatedTimeTracker
    {
        public double SimulatedSeconds { get; private set; }

        public void RecordStep(double clampedDt, bool isAdOpen)
        {
            if (isAdOpen && !double.IsNaN(clampedDt) && !double.IsInfinity(clampedDt) && clampedDt > 0d)
            {
                SimulatedSeconds += clampedDt;
            }
        }

        public double ConsumeAndReset()
        {
            double value = SimulatedSeconds;
            SimulatedSeconds = 0d;
            return value;
        }
    }
}
