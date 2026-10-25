using CozyGachapon.Contracts;
using UnityEngine;

namespace CozyGachapon.Audio
{
    public sealed class UnityAudioClock : IAudioClock
    {
        public double Now => Time.realtimeSinceStartupAsDouble;
    }
}
