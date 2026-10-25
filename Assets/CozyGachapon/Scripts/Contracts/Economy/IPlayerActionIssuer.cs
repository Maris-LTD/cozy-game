namespace CozyGachapon.Contracts
{
    public interface IPlayerActionIssuer
    {
        PlayerActionContext Issue(PlayerActionKind kind);
    }
}
