using System;
using CozyGachapon.Contracts;

namespace CozyGachapon.Audio
{
    public static class AudioBusPath
    {
        private static readonly int MaxHops = Enum.GetValues(typeof(AudioBus)).Length;

        public static bool TryGetPathDb(AudioConfig config, AudioBus bus, out float pathDb)
        {
            pathDb = 0f;
            if (!Enum.IsDefined(typeof(AudioBus), bus))
            {
                return false;
            }

            AudioBus current = bus;
            for (int step = 0; step < MaxHops; step++)
            {
                pathDb += config.GetBusDefaultDb(current);
                if (current == AudioBus.Master)
                {
                    return true;
                }

                if (!AudioBusTable.TryGetParent(current, out AudioBus parent))
                {
                    return false;
                }

                current = parent;
            }

            return false;
        }

        public static bool TryGetPathLength(AudioBus bus, out int hops)
        {
            hops = 0;
            if (!Enum.IsDefined(typeof(AudioBus), bus))
            {
                return false;
            }

            AudioBus current = bus;
            for (int step = 0; step < MaxHops; step++)
            {
                if (current == AudioBus.Master)
                {
                    hops = step;
                    return true;
                }

                if (!AudioBusTable.TryGetParent(current, out AudioBus parent))
                {
                    return false;
                }

                current = parent;
            }

            return false;
        }
    }
}
