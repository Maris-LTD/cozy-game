namespace CozyGachapon.Contracts
{
    public interface IServiceRegistry
    {
        bool Register<T>(T service) where T : class;
        bool TryGet<T>(out T service) where T : class;
    }
}
