using CozyGachapon.Contracts;
using UnityEngine;

namespace CozyGachapon.Audio
{
    public sealed class CueRuntimeTable
    {
        private readonly SoundCueRepository _repository;
        private readonly bool[] _usable;
        private readonly bool[] _hasPlayed;
        private readonly double[] _lastPlayed;
        private readonly int[] _instances;
        private readonly int[] _priority;
        private readonly AudioClip[] _defaultClip;
        private readonly VoicePartition[] _partition;

        public int Count => _usable.Length;

        public CueRuntimeTable(SoundCueRepository repository, IAudioDiagnosticsSink sink)
        {
            _repository = repository;
            int count = repository.Count;
            _usable = new bool[count];
            _hasPlayed = new bool[count];
            _lastPlayed = new double[count];
            _instances = new int[count];
            _priority = new int[count];
            _defaultClip = new AudioClip[count];
            _partition = new VoicePartition[count];

            if (repository.UnusableCount > 0)
            {
                sink.ReportWarning(AudioDiagnosticCode.CueExcluded, null);
            }

            for (int i = 0; i < count; i++)
            {
                SoundCue cue = repository.GetCue(i);
                _usable[i] = IsRuntimeUsable(cue);
                _defaultClip[i] = FindDefaultClip(cue);
                _partition[i] = ToPartition(cue.Bus);
                _priority[i] = cue.Priority;
                if (!_usable[i])
                {
                    sink.ReportWarning(AudioDiagnosticCode.CueExcluded, cue.CueId);
                }
            }
        }

        public bool TryResolve(string cueId, out int cueIndex)
        {
            if (_repository.TryGetIndex(cueId, out cueIndex) && IsKnown(cueIndex))
            {
                return true;
            }

            cueIndex = -1;
            return false;
        }

        public bool IsKnown(int cueIndex)
        {
            return cueIndex >= 0 && cueIndex < _usable.Length && _usable[cueIndex];
        }

        public SoundCue GetCue(int cueIndex)
        {
            return _repository.GetCue(cueIndex);
        }

        public AudioClip GetDefaultClip(int cueIndex)
        {
            return _defaultClip[cueIndex];
        }

        public VoicePartition GetPartition(int cueIndex)
        {
            return _partition[cueIndex];
        }

        public int GetPriority(int cueIndex)
        {
            return _priority[cueIndex];
        }

        public bool HasPlayed(int cueIndex)
        {
            return _hasPlayed[cueIndex];
        }

        public double GetLastPlayed(int cueIndex)
        {
            return _lastPlayed[cueIndex];
        }

        public int GetInstances(int cueIndex)
        {
            return _instances[cueIndex];
        }

        public void MarkPlayed(int cueIndex, double now)
        {
            _hasPlayed[cueIndex] = true;
            _lastPlayed[cueIndex] = now;
        }

        public void AddInstance(int cueIndex)
        {
            _instances[cueIndex]++;
        }

        public void RemoveInstance(int cueIndex)
        {
            if (_instances[cueIndex] > 0)
            {
                _instances[cueIndex]--;
            }
        }

        private static bool IsRuntimeUsable(SoundCue cue)
        {
            if (cue.MaxInstances < 1 || !(cue.MinInterval >= 0f) || cue.Priority < SoundCue.PriorityMin || cue.Priority > SoundCue.PriorityMax)
            {
                return false;
            }

            return AudioBusPath.TryGetPathLength(cue.Bus, out _);
        }

        private static AudioClip FindDefaultClip(SoundCue cue)
        {
            if (cue.PitchMode == PitchMode.Scale)
            {
                for (int i = 0; i < cue.PitchedSamples.Count; i++)
                {
                    PitchedSample sample = cue.PitchedSamples[i];
                    if (sample != null && sample.Clip != null)
                    {
                        return sample.Clip;
                    }
                }

                return null;
            }

            for (int i = 0; i < cue.Clips.Count; i++)
            {
                if (cue.Clips[i] != null)
                {
                    return cue.Clips[i];
                }
            }

            return null;
        }

        private static VoicePartition ToPartition(AudioBus bus)
        {
            switch (bus)
            {
                case AudioBus.Music:
                    return VoicePartition.Music;
                case AudioBus.Ambience:
                    return VoicePartition.Ambience;
                default:
                    return VoicePartition.Sfx;
            }
        }
    }
}
