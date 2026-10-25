using System;

namespace CozyGachapon.Audio
{
    public struct AudioFade
    {
        private float _from;
        private float _target;
        private double _startTime;
        private float _duration;

        public static float Factor(double now, double startTime, float duration)
        {
            if (duration <= 0f)
            {
                return 1f;
            }

            double progress = (now - startTime) / duration;
            return (float)Math.Min(1.0, Math.Max(0.0, progress));
        }

        public void StartFade(float from, float target, float duration, double now)
        {
            _from = from;
            _target = target;
            _startTime = now;
            _duration = duration;
        }

        public void Retarget(float target, float duration, double now)
        {
            StartFade(Evaluate(now), target, duration, now);
        }

        public void Snap(float value)
        {
            StartFade(value, value, 0f, 0.0);
        }

        public float Evaluate(double now)
        {
            if (_duration <= 0f)
            {
                return _target;
            }

            return _from + (_target - _from) * Factor(now, _startTime, _duration);
        }
    }
}
