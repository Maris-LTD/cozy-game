namespace CozyGachapon.Contracts
{
    public readonly struct WorldPressEvent
    {
        public float WorldX { get; }
        public float WorldY { get; }
        public float ScreenX { get; }
        public float ScreenY { get; }

        public WorldPressEvent(float worldX, float worldY, float screenX, float screenY)
        {
            WorldX = worldX;
            WorldY = worldY;
            ScreenX = screenX;
            ScreenY = screenY;
        }
    }
}
