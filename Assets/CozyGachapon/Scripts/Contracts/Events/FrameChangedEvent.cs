namespace CozyGachapon.Contracts
{
    public readonly struct FrameChangedEvent
    {
        public float FrameX { get; }
        public float FrameY { get; }
        public float FrameWidth { get; }
        public float FrameHeight { get; }
        public float FrameScale { get; }
        public float ExtraHeightRef { get; }

        public FrameChangedEvent(float frameX, float frameY, float frameWidth, float frameHeight, float frameScale, float extraHeightRef)
        {
            FrameX = frameX;
            FrameY = frameY;
            FrameWidth = frameWidth;
            FrameHeight = frameHeight;
            FrameScale = frameScale;
            ExtraHeightRef = extraHeightRef;
        }
    }
}
