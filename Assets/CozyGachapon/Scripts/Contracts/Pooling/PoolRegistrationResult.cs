namespace CozyGachapon.Contracts
{
    public readonly struct PoolRegistrationResult
    {
        public bool Accepted { get; }
        public int Capacity { get; }
        public PoolRegistrationError Error { get; }

        private PoolRegistrationResult(bool accepted, int capacity, PoolRegistrationError error)
        {
            Accepted = accepted;
            Capacity = capacity;
            Error = error;
        }

        public static PoolRegistrationResult Accept(int capacity)
        {
            return new PoolRegistrationResult(true, capacity, PoolRegistrationError.None);
        }

        public static PoolRegistrationResult Reject(PoolRegistrationError error)
        {
            return new PoolRegistrationResult(false, 0, error);
        }
    }
}
