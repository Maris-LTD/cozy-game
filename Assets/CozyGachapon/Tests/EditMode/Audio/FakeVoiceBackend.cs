using CozyGachapon.Audio;
using UnityEngine;

namespace CozyGachapon.Tests.Audio
{
    public sealed class FakeVoiceBackend : IVoiceBackend
    {
        private readonly AudioClip[] _clips;
        private readonly float[] _volumes;
        private readonly float[] _pitches;
        private readonly float[] _pans;
        private readonly bool[] _loops;
        private readonly bool[] _playing;
        private readonly bool[] _paused;
        private readonly int[] _startCounts;
        private readonly int[] _stopCounts;
        private readonly float[] _positions;

        public int VoiceCount { get; private set; }
        public bool SupportsPan { get; private set; } = true;
        public int StartCount { get; private set; }
        public int StopCount { get; private set; }
        public int SetVolumeCount { get; private set; }
        public int PauseCount { get; private set; }
        public int ResumeCount { get; private set; }
        public int LastStartedVoice { get; private set; } = -1;
        public int MaxStartedVoice { get; private set; } = -1;

        public int PlayingCount
        {
            get
            {
                int count = 0;
                for (int i = 0; i < VoiceCount; i++)
                {
                    if (_playing[i])
                    {
                        count++;
                    }
                }

                return count;
            }
        }

        public FakeVoiceBackend(int voiceCount)
        {
            VoiceCount = voiceCount;
            _clips = new AudioClip[voiceCount];
            _volumes = new float[voiceCount];
            _pitches = new float[voiceCount];
            _pans = new float[voiceCount];
            _loops = new bool[voiceCount];
            _playing = new bool[voiceCount];
            _paused = new bool[voiceCount];
            _startCounts = new int[voiceCount];
            _stopCounts = new int[voiceCount];
            _positions = new float[voiceCount];
        }

        public void SetSupportsPan(bool supportsPan)
        {
            SupportsPan = supportsPan;
        }

        public void Start(int voice, AudioClip clip, float volume, float pitch, float pan, bool loop)
        {
            _clips[voice] = clip;
            _volumes[voice] = volume;
            _pitches[voice] = pitch;
            _pans[voice] = pan;
            _loops[voice] = loop;
            _playing[voice] = true;
            _paused[voice] = false;
            _positions[voice] = 0f;
            _startCounts[voice]++;
            StartCount++;
            LastStartedVoice = voice;
            if (voice > MaxStartedVoice)
            {
                MaxStartedVoice = voice;
            }
        }

        public void Stop(int voice)
        {
            _playing[voice] = false;
            _paused[voice] = false;
            _stopCounts[voice]++;
            StopCount++;
        }

        public void Pause(int voice)
        {
            _paused[voice] = true;
            PauseCount++;
        }

        public void Resume(int voice)
        {
            _paused[voice] = false;
            ResumeCount++;
        }

        public void SetVolume(int voice, float volume)
        {
            _volumes[voice] = volume;
            SetVolumeCount++;
        }

        public bool IsPlaying(int voice)
        {
            return _playing[voice] && !_paused[voice];
        }

        public float GetPosition(int voice)
        {
            return _positions[voice];
        }

        public void AdvancePlayback(float seconds)
        {
            for (int i = 0; i < VoiceCount; i++)
            {
                if (_playing[i] && !_paused[i])
                {
                    _positions[i] += seconds;
                }
            }
        }

        public float GetSpatialBlend(int voice)
        {
            return 0f;
        }

        public void FinishVoice(int voice)
        {
            _playing[voice] = false;
        }

        public AudioClip GetClip(int voice)
        {
            return _clips[voice];
        }

        public float GetVolume(int voice)
        {
            return _volumes[voice];
        }

        public float GetPitch(int voice)
        {
            return _pitches[voice];
        }

        public float GetPan(int voice)
        {
            return _pans[voice];
        }

        public bool GetLoop(int voice)
        {
            return _loops[voice];
        }

        public bool IsPaused(int voice)
        {
            return _paused[voice];
        }

        public int GetStartCount(int voice)
        {
            return _startCounts[voice];
        }

        public int GetStopCount(int voice)
        {
            return _stopCounts[voice];
        }
    }
}
