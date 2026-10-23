namespace CozyGachapon.Contracts
{
    public interface IInputBlockGate
    {
        int AcquireInputBlock(string owner);
        void Release(int token);
        bool IsInputBlocked { get; }
        InputGateStatus Status { get; }
    }
}
