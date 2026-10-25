using System;

namespace CozyGachapon.Economy
{
    public sealed class DisplayStringCache
    {
        private readonly char[] _last;
        private int _lastLength;
        private string _cached;

        public DisplayStringCache(int capacity)
        {
            _last = new char[capacity];
        }

        public string Get(ReadOnlySpan<char> current)
        {
            if (_cached != null && current.SequenceEqual(new ReadOnlySpan<char>(_last, 0, _lastLength)))
            {
                return _cached;
            }

            current.CopyTo(_last);
            _lastLength = current.Length;
            _cached = new string(current);
            return _cached;
        }
    }
}
