using CozyGachapon.Contracts;
using UnityEngine;

namespace CozyGachapon.Audio
{
    public sealed class PitchResolver
    {
        private const float NeutralPitch = 1f;
        private const float MinPitch = 0.01f;

        private readonly AudioConfig _config;
        private readonly IAudioRandom _random;
        private readonly CueClipSelector _clips;

        public PitchResolver(AudioConfig config, IAudioRandom random, CueClipSelector clips)
        {
            _config = config;
            _random = random;
            _clips = clips;
        }

        public float Resolve(int cueIndex, SoundCue cue, in AudioPlayParams playParams, AudioClip fallbackClip, out AudioClip clip)
        {
            float pitch;
            switch (cue.PitchMode)
            {
                case PitchMode.Scale:
                    pitch = ResolveScale(cue, playParams.PitchPosition, fallbackClip, out clip);
                    break;
                case PitchMode.Random:
                    clip = SelectClip(cueIndex, cue, fallbackClip);
                    pitch = ApplyJitter(cue);
                    break;
                default:
                    clip = SelectClip(cueIndex, cue, fallbackClip);
                    pitch = NeutralPitch;
                    break;
            }

            return AudioMath.IsFinite(pitch) ? Mathf.Max(pitch, MinPitch) : NeutralPitch;
        }

        private float ResolveScale(SoundCue cue, float pitchPosition, AudioClip fallbackClip, out AudioClip clip)
        {
            clip = fallbackClip;
            if (_config.ScaleIntervals.Count < 1)
            {
                return NeutralPitch;
            }

            int note = NoteIndexCalculator.NoteIndex(pitchPosition, _config.FallbackPitchPosition, _config.ScaleNoteCount);
            int semitone = NoteIndexCalculator.Semitone(note, _config.ScaleIntervals);
            if (!SamplePitchSelector.TrySelectSample(semitone, cue.PitchedSamples, out int index, out int shift))
            {
                return NeutralPitch;
            }

            clip = cue.PitchedSamples[index].Clip;
            return AudioMath.SemitoneRatio(shift) * ApplyJitter(cue);
        }

        private AudioClip SelectClip(int cueIndex, SoundCue cue, AudioClip fallbackClip)
        {
            AudioClip selected = _clips.Select(cueIndex, cue, _random);
            return selected != null ? selected : fallbackClip;
        }

        private float ApplyJitter(SoundCue cue)
        {
            return NeutralPitch + cue.PitchJitterRatio * _random.NextSigned();
        }
    }
}
