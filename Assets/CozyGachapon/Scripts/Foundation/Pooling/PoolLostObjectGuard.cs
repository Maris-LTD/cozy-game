using CozyGachapon.Contracts;

namespace CozyGachapon.Pooling
{
    public sealed class PoolLostObjectGuard
    {
        private readonly IPoolDiagnosticsSink _sink;

        public PoolLostObjectGuard(IPoolDiagnosticsSink sink)
        {
            _sink = sink;
        }

        public bool TryPopLiveFree(PoolRecord record, out int slot)
        {
            while (record.TryPopFree(out slot))
            {
                if (record.GetInstance(slot) != null)
                {
                    return true;
                }

                record.Counters.AddLost();
                _sink.ReportError(PoolDiagnosticCode.PoolObjectLost, record.Id);
            }

            slot = -1;
            return false;
        }
    }
}
