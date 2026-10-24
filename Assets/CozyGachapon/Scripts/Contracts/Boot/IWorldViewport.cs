namespace CozyGachapon.Contracts
{
    public interface IWorldViewport
    {
        bool TryScreenToWorld(float screenX, float screenY, out float worldX, out float worldY);
        void ReceiveWorldPress(WorldPressEvent worldPress);
    }
}
