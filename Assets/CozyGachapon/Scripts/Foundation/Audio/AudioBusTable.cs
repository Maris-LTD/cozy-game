using CozyGachapon.Contracts;

namespace CozyGachapon.Audio
{
    public static class AudioBusTable
    {
        public static bool TryGetParent(AudioBus bus, out AudioBus parent)
        {
            switch (bus)
            {
                case AudioBus.Music:
                case AudioBus.Ambience:
                case AudioBus.Sfx:
                    parent = AudioBus.Master;
                    return true;
                case AudioBus.Gameplay:
                case AudioBus.Ui:
                    parent = AudioBus.Sfx;
                    return true;
                case AudioBus.Plinko:
                case AudioBus.Gacha:
                    parent = AudioBus.Gameplay;
                    return true;
                default:
                    parent = AudioBus.Master;
                    return false;
            }
        }
    }
}
