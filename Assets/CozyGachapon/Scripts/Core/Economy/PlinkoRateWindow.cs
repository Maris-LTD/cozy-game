using System;

namespace CozyGachapon.Economy
{
    public sealed class PlinkoRateWindow
    {
        public const double RateBinSeconds = 1.0d;
        private const double Epsilon = 1e-9d;

        private readonly double[] _bins;
        private readonly double _windowSeconds;
        private int _currentIndex;
        private double _binElapsed;
        private double _runningTime;

        public PlinkoRateWindow(double windowSeconds)
        {
            double sanitized = ValueSanitizer.Sanitize(windowSeconds);
            _windowSeconds = sanitized > RateBinSeconds ? sanitized : RateBinSeconds;
            int binCount = (int)Math.Ceiling(_windowSeconds / RateBinSeconds);
            if (binCount < 1)
            {
                binCount = 1;
            }

            _bins = new double[binCount];
        }

        public double RunningTime => _runningTime;

        public double RateAvg
        {
            get
            {
                double sum = 0d;
                for (int i = 0; i < _bins.Length; i++)
                {
                    sum += _bins[i];
                }

                if (sum <= 0d)
                {
                    return 0d;
                }

                double denom = _runningTime;
                if (denom < RateBinSeconds)
                {
                    denom = RateBinSeconds;
                }
                else if (denom > _windowSeconds)
                {
                    denom = _windowSeconds;
                }

                return sum / denom;
            }
        }

        public void RecordCredit(double creditedAmount)
        {
            if (double.IsNaN(creditedAmount) || double.IsInfinity(creditedAmount) || creditedAmount <= 0d)
            {
                return;
            }

            RotateIfBinFull();
            _bins[_currentIndex] += creditedAmount;
        }

        public void Advance(double dt)
        {
            if (double.IsNaN(dt) || double.IsInfinity(dt) || dt <= 0d)
            {
                return;
            }

            _runningTime += dt;
            double remaining = dt;
            while (remaining > 0d)
            {
                RotateIfBinFull();
                double spaceInBin = RateBinSeconds - _binElapsed;
                if (remaining <= spaceInBin + Epsilon)
                {
                    _binElapsed += remaining;
                    break;
                }

                _binElapsed = RateBinSeconds;
                remaining -= spaceInBin;
            }
        }

        public void Reset()
        {
            for (int i = 0; i < _bins.Length; i++)
            {
                _bins[i] = 0d;
            }

            _currentIndex = 0;
            _binElapsed = 0d;
            _runningTime = 0d;
        }

        private void RotateIfBinFull()
        {
            if (_binElapsed >= RateBinSeconds - Epsilon)
            {
                _binElapsed -= RateBinSeconds;
                if (_binElapsed < 0d)
                {
                    _binElapsed = 0d;
                }

                _currentIndex = (_currentIndex + 1) % _bins.Length;
                _bins[_currentIndex] = 0d;
            }
        }
    }
}
