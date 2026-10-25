namespace CozyGachapon.Contracts
{
    public readonly struct PoolConfigIssue
    {
        public PoolDiagnosticCode Code { get; }
        public string PoolId { get; }
        public bool IsError { get; }

        public PoolConfigIssue(PoolDiagnosticCode code, string poolId, bool isError)
        {
            Code = code;
            PoolId = poolId;
            IsError = isError;
        }
    }
}
