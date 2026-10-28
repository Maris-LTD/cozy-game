namespace CozyGachapon.Contracts
{
    public interface IValidationContext
    {
        void AddError(string message, UnityEngine.Object contextObj = null);
        void AddWarning(string message, UnityEngine.Object contextObj = null);
        bool HasErrors { get; }
    }

    public interface IValidatableAsset
    {
        void Validate(IValidationContext context);
    }
}

