namespace CozyGachapon.Contracts
{
    public interface IBootStatusReader
    {
        bool IsBooting { get; }
        bool IsBootFailed { get; }
    }
}
