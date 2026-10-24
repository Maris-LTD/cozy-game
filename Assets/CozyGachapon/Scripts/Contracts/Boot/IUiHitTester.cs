namespace CozyGachapon.Contracts
{
    public interface IUiHitTester
    {
        bool HitsUi(float screenX, float screenY, int pointerId);
    }
}
