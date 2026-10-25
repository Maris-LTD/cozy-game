using System;
using UnityEngine;

namespace CozyGachapon.Economy
{
    [Serializable]
    public struct DoubleRange
    {
        [SerializeField] private double _min;
        [SerializeField] private double _max;

        public double Min => _min;
        public double Max => _max;

        public DoubleRange(double min, double max)
        {
            _min = min;
            _max = max;
        }

        public bool Contains(double value)
        {
            return value >= _min && value <= _max;
        }
    }
}
