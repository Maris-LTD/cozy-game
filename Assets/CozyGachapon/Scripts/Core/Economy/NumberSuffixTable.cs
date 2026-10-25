using System;

namespace CozyGachapon.Economy
{
    public sealed class NumberSuffixTable
    {
        private const string SingleLetterSuffixes = "KMBT";
        private const int FirstTwoLetterGroup = 5;
        private const int LetterCount = 26;
        private const char FirstLetter = 'a';

        private readonly char[] _first;
        private readonly char[] _second;
        private readonly int[] _lengths;

        public NumberSuffixTable(int maxGroup)
        {
            _first = new char[maxGroup + 1];
            _second = new char[maxGroup + 1];
            _lengths = new int[maxGroup + 1];
            for (int k = 1; k <= maxGroup; k++)
            {
                if (k < FirstTwoLetterGroup)
                {
                    _first[k] = SingleLetterSuffixes[k - 1];
                    _lengths[k] = 1;
                    continue;
                }

                int index = k - FirstTwoLetterGroup;
                _first[k] = (char)(FirstLetter + index / LetterCount);
                _second[k] = (char)(FirstLetter + index % LetterCount);
                _lengths[k] = 2;
            }

            MaxSuffixLength = maxGroup >= FirstTwoLetterGroup ? 2 : 1;
        }

        public int MaxSuffixLength { get; private set; }

        public int Write(int group, Span<char> destination, int position)
        {
            int length = _lengths[group];
            if (length > 0)
            {
                destination[position] = _first[group];
            }

            if (length > 1)
            {
                destination[position + 1] = _second[group];
            }

            return position + length;
        }
    }
}
