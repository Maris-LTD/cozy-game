using System.Collections.Generic;
using CozyGachapon.Audio;
using CozyGachapon.Contracts;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace CozyGachapon.Tests.Audio
{
    public abstract class AudioTestFixture
    {
        private readonly List<Object> _createdObjects = new List<Object>();

        protected AudioConfig Config { get; private set; }

        [SetUp]
        public void SetUpFixture()
        {
            Config = Track(ScriptableObject.CreateInstance<AudioConfig>());
        }

        [TearDown]
        public void TearDownFixture()
        {
            for (int i = _createdObjects.Count - 1; i >= 0; i--)
            {
                if (_createdObjects[i] != null)
                {
                    Object.DestroyImmediate(_createdObjects[i]);
                }
            }

            _createdObjects.Clear();
        }

        protected T Track<T>(T instance) where T : Object
        {
            _createdObjects.Add(instance);
            return instance;
        }

        protected AudioClip CreateClip()
        {
            return Track(AudioClip.Create("test", 1, 1, 44100, false));
        }

        protected SoundCue CreateCue(string cueId, bool withClip = true)
        {
            SoundCue cue = Track(ScriptableObject.CreateInstance<SoundCue>());
            AudioClip[] clips = withClip ? new[] { CreateClip() } : new AudioClip[0];
            cue.SetIdentityForTest(cueId, AudioBus.Sfx);
            cue.SetClipsForTest(clips);
            return cue;
        }

        protected static void SetLoop(SoundCue cue, bool loop)
        {
            SetCueBool(cue, "_loop", loop);
        }

        protected static void SetCueBool(SoundCue cue, string propertyName, bool value)
        {
            SerializedObject serialized = new SerializedObject(cue);
            SerializedProperty property = serialized.FindProperty(propertyName);
            Assert.IsNotNull(property, propertyName);
            property.boolValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        protected SoundCue CreateScaleCue(string cueId, bool withPitchedSample)
        {
            SoundCue cue = Track(ScriptableObject.CreateInstance<SoundCue>());
            PitchedSample[] samples = withPitchedSample
                ? new[] { new PitchedSample(CreateClip(), 4) }
                : new PitchedSample[0];
            cue.SetIdentityForTest(cueId, AudioBus.Plinko);
            cue.SetPitchForTest(PitchMode.Scale, 0.03f, samples);
            return cue;
        }
    }
}
