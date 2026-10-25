namespace CozyGachapon.Contracts
{
    public readonly struct AudioPlayParams
    {
        public float PitchPosition { get; }
        public float PanPosition { get; }

        public bool HasPitchPosition => !float.IsNaN(PitchPosition);
        public bool HasPanPosition => !float.IsNaN(PanPosition);

        public AudioPlayParams(float pitchPosition, float panPosition)
        {
            PitchPosition = pitchPosition;
            PanPosition = panPosition;
        }

        public static AudioPlayParams None => new AudioPlayParams(float.NaN, float.NaN);
    }
}
