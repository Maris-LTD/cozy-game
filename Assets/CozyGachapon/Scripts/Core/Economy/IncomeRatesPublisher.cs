using System;
using CozyGachapon.Contracts;

namespace CozyGachapon.Economy
{
    public sealed class IncomeRatesPublisher
    {
        private const double Epsilon = 1e-9d;

        private readonly IIncomePipeline _pipeline;
        private readonly EconomyConfig _config;
        private double _elapsedSincePublish;
        private double _lastIdleCps;
        private double _lastPlinkoRateAvg;
        private bool _hasPendingChange;

        public event Action<IncomeRatesChangedEvent> IncomeRatesChanged;

        public IncomeRatesPublisher(IIncomePipeline pipeline, EconomyConfig config)
        {
            _pipeline = pipeline ?? throw new ArgumentNullException(nameof(pipeline));
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _elapsedSincePublish = ValueSanitizer.Sanitize(_config.RatesPublishInterval);
        }

        public void MarkChanged()
        {
            _hasPendingChange = true;
        }

        public void Tick(double clampedDt)
        {
            if (double.IsNaN(clampedDt) || double.IsInfinity(clampedDt) || clampedDt <= 0d)
            {
                return;
            }

            _elapsedSincePublish += clampedDt;
            double interval = ValueSanitizer.Sanitize(_config.RatesPublishInterval);
            if (_elapsedSincePublish + Epsilon < interval)
            {
                return;
            }

            double currentIdle = _pipeline.IdleCps;
            double currentPlinko = _pipeline.PlinkoRateAvg;
            bool changed = _hasPendingChange
                || Math.Abs(currentIdle - _lastIdleCps) > Epsilon
                || Math.Abs(currentPlinko - _lastPlinkoRateAvg) > Epsilon;

            if (!changed)
            {
                return;
            }

            _lastIdleCps = currentIdle;
            _lastPlinkoRateAvg = currentPlinko;
            _hasPendingChange = false;
            _elapsedSincePublish = 0d;
            IncomeRatesChanged?.Invoke(new IncomeRatesChangedEvent(currentIdle, currentPlinko));
        }
    }
}
