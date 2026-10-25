using CozyGachapon.Audio;
using NUnit.Framework;
using UnityEngine;

namespace CozyGachapon.Tests.Audio
{
    public class UnityVoiceBackendTests
    {
        private const int ClipSamples = 4410;
        private const int ClipFrequency = 44100;

        private GameObject _container;
        private AudioClip _clip;
        private int _totalVoices;

        [SetUp]
        public void SetUp()
        {
            _container = new GameObject("AudioVoicesTest");
            _clip = AudioClip.Create("VoiceBackendTestClip", ClipSamples, 1, ClipFrequency, false);
            _totalVoices = DefaultAudioAssets.LoadConfig().TotalVoices;
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_container);
            Object.DestroyImmediate(_clip);
        }

        [Test]
        public void Constructor_CreatesNoAudioSources()
        {
            UnityVoiceBackend backend = new UnityVoiceBackend(_container.transform, _totalVoices);

            Assert.IsFalse(backend.IsAllocated);
            Assert.AreEqual(0, _container.GetComponents<AudioSource>().Length);
        }

        [Test]
        public void AC36_VoiceCount_AllocatesExactlyTotalVoices_With2DDefaults()
        {
            UnityVoiceBackend backend = new UnityVoiceBackend(_container.transform, _totalVoices);
            Assert.AreEqual(0, backend.VoiceCount);

            backend.Allocate();

            Assert.AreEqual(_totalVoices, backend.VoiceCount);
            AudioSource[] sources = _container.GetComponents<AudioSource>();
            Assert.AreEqual(_totalVoices, sources.Length);
            for (int i = 0; i < sources.Length; i++)
            {
                Assert.IsFalse(sources[i].playOnAwake);
                Assert.AreEqual(0f, sources[i].spatialBlend);
                Assert.IsFalse(sources[i].loop);
                Assert.AreEqual(0f, backend.GetSpatialBlend(i));
            }
        }

        [Test]
        public void Allocate_Twice_DoesNotAddSources()
        {
            UnityVoiceBackend backend = new UnityVoiceBackend(_container.transform, _totalVoices);

            backend.Allocate();
            backend.Allocate();
            Assert.AreEqual(_totalVoices, backend.VoiceCount);

            Assert.AreEqual(_totalVoices, _container.GetComponents<AudioSource>().Length);
        }

        [Test]
        public void NullContainerOrZeroVoices_HasNoVoices_AndDoesNotThrow()
        {
            UnityVoiceBackend noContainer = new UnityVoiceBackend(null, _totalVoices);
            UnityVoiceBackend noVoices = new UnityVoiceBackend(_container.transform, 0);

            Assert.AreEqual(0, noContainer.VoiceCount);
            Assert.AreEqual(0, noVoices.VoiceCount);
            Assert.AreEqual(0, _container.GetComponents<AudioSource>().Length);
            Assert.DoesNotThrow(() => noContainer.Start(0, _clip, 1f, 1f, 0f, false));
        }

        [Test]
        public void Start_SetsClipVolumePitchPanLoop_AndLastStartedVoice()
        {
            UnityVoiceBackend backend = new UnityVoiceBackend(_container.transform, _totalVoices);
            backend.Allocate();

            backend.Start(3, _clip, 0.5f, 1.25f, -0.4f, true);

            AudioSource source = backend.GetSource(3);
            Assert.AreSame(_clip, source.clip);
            Assert.AreEqual(0.5f, source.volume, 1e-5f);
            Assert.AreEqual(1.25f, source.pitch, 1e-5f);
            Assert.AreEqual(-0.4f, source.panStereo, 1e-5f);
            Assert.IsTrue(source.loop);
            Assert.AreEqual(3, backend.LastStartedVoice);
        }

        [Test]
        public void Start_NonPositivePitch_KeepsPositivePitch()
        {
            UnityVoiceBackend backend = new UnityVoiceBackend(_container.transform, _totalVoices);
            backend.Allocate();

            backend.Start(0, _clip, 1f, 0f, 0f, false);
            backend.Start(1, _clip, 1f, -1f, 0f, false);

            Assert.Greater(backend.GetSource(0).pitch, 0f);
            Assert.Greater(backend.GetSource(1).pitch, 0f);
        }

        [Test]
        public void ControlCalls_DoNotCreateOrDestroySources()
        {
            UnityVoiceBackend backend = new UnityVoiceBackend(_container.transform, _totalVoices);
            backend.Allocate();

            for (int i = 0; i < _totalVoices; i++)
            {
                backend.Start(i, _clip, 1f, 1f, 0f, false);
                backend.Pause(i);
                backend.Resume(i);
                backend.SetVolume(i, 0.25f);
                backend.GetPosition(i);
                backend.IsPlaying(i);
                backend.Stop(i);
            }

            Assert.AreEqual(_totalVoices, _container.GetComponents<AudioSource>().Length);
            Assert.AreEqual(0.25f, backend.GetSource(0).volume, 1e-5f);
        }

        [Test]
        public void OutOfRangeVoice_IsIgnored()
        {
            UnityVoiceBackend backend = new UnityVoiceBackend(_container.transform, _totalVoices);
            backend.Allocate();

            Assert.DoesNotThrow(() => backend.Start(-1, _clip, 1f, 1f, 0f, false));
            Assert.DoesNotThrow(() => backend.Start(_totalVoices, _clip, 1f, 1f, 0f, false));
            Assert.DoesNotThrow(() => backend.Pause(_totalVoices));
            Assert.DoesNotThrow(() => backend.Resume(-1));
            Assert.DoesNotThrow(() => backend.Stop(_totalVoices));
            Assert.DoesNotThrow(() => backend.SetVolume(-1, 1f));
            Assert.IsFalse(backend.IsPlaying(_totalVoices));
            Assert.AreEqual(0f, backend.GetPosition(-1));
            Assert.IsNull(backend.GetSource(_totalVoices));
            Assert.AreEqual(-1, backend.LastStartedVoice);
        }

        [Test]
        public void SupportsPan_IsTrue()
        {
            UnityVoiceBackend backend = new UnityVoiceBackend(_container.transform, _totalVoices);

            Assert.IsTrue(backend.SupportsPan);
        }
    }
}
