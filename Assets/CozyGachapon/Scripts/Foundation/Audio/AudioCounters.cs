using System;
using CozyGachapon.Contracts;

namespace CozyGachapon.Audio
{
    public sealed class AudioCounters
    {
        private readonly int[] _droppedByReason;

        public int Requests { get; private set; }
        public int Played { get; private set; }
        public int SameCueSteals { get; private set; }
        public int PrioritySteals { get; private set; }
        public int PeakVoices { get; private set; }
        public double MinSameCueStealAgeSeconds { get; private set; } = double.MaxValue;

        public AudioCounters()
        {
            _droppedByReason = new int[Enum.GetValues(typeof(DropReason)).Length];
        }

        public void RecordPlayed(int activeVoices)
        {
            Requests++;
            Played++;
            if (activeVoices > PeakVoices)
            {
                PeakVoices = activeVoices;
            }
        }

        public void RecordSameCueSteal(double victimAgeSeconds)
        {
            SameCueSteals++;
            if (victimAgeSeconds < MinSameCueStealAgeSeconds)
            {
                MinSameCueStealAgeSeconds = victimAgeSeconds;
            }
        }

        public void RecordPrioritySteal()
        {
            PrioritySteals++;
        }

        public void RecordDropped(DropReason reason)
        {
            int index = (int)reason;
            if (reason == DropReason.None || index < 0 || index >= _droppedByReason.Length)
            {
                return;
            }

            Requests++;
            _droppedByReason[index]++;
        }

        public int GetDropped(DropReason reason)
        {
            int index = (int)reason;
            return index >= 0 && index < _droppedByReason.Length ? _droppedByReason[index] : 0;
        }

        public AudioStats ToStats(int activeVoices)
        {
            return new AudioStats(
                Requests,
                Played,
                activeVoices,
                GetDropped(DropReason.NotReady),
                GetDropped(DropReason.UnknownCue),
                GetDropped(DropReason.Locked),
                GetDropped(DropReason.Suspended),
                GetDropped(DropReason.UserMuted),
                GetDropped(DropReason.BusSilent),
                GetDropped(DropReason.BackgroundFiltered),
                GetDropped(DropReason.RateLimited),
                GetDropped(DropReason.InstanceCap),
                GetDropped(DropReason.NoVoice),
                SameCueSteals,
                PrioritySteals,
                PeakVoices,
                SameCueSteals == 0 ? 0d : MinSameCueStealAgeSeconds);
        }
    }
}
