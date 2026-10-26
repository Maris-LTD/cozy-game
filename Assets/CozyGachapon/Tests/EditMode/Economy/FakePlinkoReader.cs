using CozyGachapon.Contracts;

namespace CozyGachapon.Tests.Economy
{
    public sealed class FakePlinkoReader
    {
        private readonly IBaseCoinValueSource _source;

        public FakePlinkoReader(IBaseCoinValueSource source)
        {
            _source = source;
        }

        public double BaseCoinValue => _source != null ? _source.BaseCoinValue : 1d;
    }
}
