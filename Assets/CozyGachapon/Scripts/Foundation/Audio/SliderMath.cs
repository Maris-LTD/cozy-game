namespace CozyGachapon.Audio
{
    public static class SliderMath
    {
        public static float Normalize(float slider, float defaultSlider)
        {
            if (float.IsNaN(slider))
            {
                slider = defaultSlider;
            }

            if (slider > 1f)
            {
                return 1f;
            }

            return slider < 0f ? 0f : slider;
        }

        public static bool TryToDb(float slider, float minDb, float defaultSlider, out float userDb)
        {
            float normalized = Normalize(slider, defaultSlider);
            if (normalized <= 0f)
            {
                userDb = minDb;
                return false;
            }

            userDb = minDb * (1f - normalized);
            return true;
        }
    }
}
