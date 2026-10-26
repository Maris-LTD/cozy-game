using System;
using CozyGachapon.Contracts;

namespace CozyGachapon.Economy
{
    public sealed class IncomeSourceTable
    {
        private const int DefaultCapacity = 8;

        private string[] _keys = new string[DefaultCapacity];
        private IncomeChannel[] _channels = new IncomeChannel[DefaultCapacity];
        private double[] _baseRates = new double[DefaultCapacity];
        private double[] _flatUnits = new double[DefaultCapacity];
        private double[] _synergy = new double[DefaultCapacity];

        public int Count { get; private set; }

        public int IndexOf(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                return -1;
            }

            for (int i = 0; i < Count; i++)
            {
                if (string.Equals(_keys[i], key, StringComparison.Ordinal))
                {
                    return i;
                }
            }

            return -1;
        }

        public bool Upsert(string key, IncomeChannel channel, double baseRate)
        {
            if (string.IsNullOrEmpty(key) || !IsConcrete(channel) || !IsValidRate(baseRate))
            {
                return false;
            }

            int index = IndexOf(key);
            if (index < 0)
            {
                index = Append(key);
            }

            _channels[index] = channel;
            _baseRates[index] = baseRate;
            return true;
        }

        public string KeyAt(int index)
        {
            return _keys[index];
        }

        public IncomeChannel ChannelAt(int index)
        {
            return _channels[index];
        }

        public double BaseRateAt(int index)
        {
            return _baseRates[index];
        }

        public double FlatUnitsAt(int index)
        {
            return _flatUnits[index];
        }

        public double SynergyAt(int index)
        {
            return _synergy[index];
        }

        public void SetDerived(int index, double flatUnits, double synergy)
        {
            _flatUnits[index] = flatUnits;
            _synergy[index] = synergy;
        }

        private static bool IsConcrete(IncomeChannel channel)
        {
            return channel == IncomeChannel.Idle || channel == IncomeChannel.Plinko;
        }

        private static bool IsValidRate(double rate)
        {
            return rate >= 0d && !double.IsInfinity(rate);
        }

        private int Append(string key)
        {
            if (Count == _keys.Length)
            {
                int capacity = _keys.Length * 2;
                Array.Resize(ref _keys, capacity);
                Array.Resize(ref _channels, capacity);
                Array.Resize(ref _baseRates, capacity);
                Array.Resize(ref _flatUnits, capacity);
                Array.Resize(ref _synergy, capacity);
            }

            int index = Count;
            _keys[index] = key;
            _flatUnits[index] = 0d;
            _synergy[index] = 1d;
            Count++;
            return index;
        }
    }
}
