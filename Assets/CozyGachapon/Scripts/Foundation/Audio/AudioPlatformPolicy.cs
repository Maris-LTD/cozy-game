namespace CozyGachapon.Audio
{
    public readonly struct AudioPlatformPolicy
    {
        public bool RequiresUnlock { get; }

        public AudioPlatformPolicy(bool requiresUnlock)
        {
            RequiresUnlock = requiresUnlock;
        }
    }
}
