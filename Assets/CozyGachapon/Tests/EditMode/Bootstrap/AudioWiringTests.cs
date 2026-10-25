using System.Collections.Generic;
using CozyGachapon.Audio;
using CozyGachapon.Contracts;
using CozyGachapon.Foundation;
using CozyGachapon.Tests.Audio;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace CozyGachapon.Tests.Bootstrap
{
    public class AudioWiringTests : BootstrapGraphTestBase
    {
        private const float BootSeconds = 1f;

        private GameObject _container;
        private AudioCueLibrary _library;
        private AudioConfig _config;

        [SetUp]
        public void SetUpWiring()
        {
            _container = new GameObject("AudioVoicesWiringTest");
            _config = DefaultAudioAssets.LoadConfig();
            _library = ScriptableObject.CreateInstance<AudioCueLibrary>();
            List<SoundCue> cues = DefaultAudioAssets.LoadCues();
            SerializedObject serialized = new SerializedObject(_library);
            SerializedProperty array = serialized.FindProperty("_cues");
            array.arraySize = cues.Count;
            for (int i = 0; i < cues.Count; i++)
            {
                array.GetArrayElementAtIndex(i).objectReferenceValue = cues[i];
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        [TearDown]
        public void TearDownWiring()
        {
            Object.DestroyImmediate(_container);
            Object.DestroyImmediate(_library);
        }

        [Test]
        public void Register_AddsAudioStep_AndCreatesNoSourcesBeforeRun()
        {
            BootstrapGraph graph = CreateGraph(null);
            AudioWiring wiring = CreateWiring(_config, _library, false);
            int before = graph.Sequencer.RegisteredCount;

            Assert.IsTrue(wiring.Register(graph));

            Assert.AreEqual(before + 1, graph.Sequencer.RegisteredCount);
            Assert.AreEqual(AudioServiceBootStep.StepId, wiring.Module.ServiceStep.Id);
            Assert.AreEqual(0, _container.GetComponents<AudioSource>().Length);
            Assert.IsFalse(wiring.Voices.IsAllocated);
        }

        [Test]
        public void AC36_BootRun_InitializesService_WithExactlyTotalVoices()
        {
            BootstrapGraph graph = CreateGraph(null);
            AudioWiring wiring = CreateWiring(_config, _library, false);
            wiring.Register(graph);

            TickFor(graph, null, BootSeconds);
            wiring.Tick();

            Assert.IsTrue(wiring.Module.ServiceStep.IsDone);
            Assert.IsTrue(wiring.Service.IsReady);
            Assert.AreEqual(_config.TotalVoices, wiring.Voices.VoiceCount);
            Assert.AreEqual(_config.TotalVoices, _container.GetComponents<AudioSource>().Length);
            Assert.AreEqual(AppLifecycleState.Running, graph.Lifecycle.State);
            Assert.IsTrue(wiring.Module.Service.RuntimeState.HasInteractive);
        }

        [Test]
        public void FirstInteractionBeforeRun_IsRemembered_WhenUnlockRequired()
        {
            BootstrapGraph graph = CreateGraph(null);
            AudioWiring wiring = CreateWiring(_config, _library, true);
            wiring.Register(graph);

            graph.FirstInteraction.NotifyUserGesture();
            TickFor(graph, null, BootSeconds);

            Assert.IsTrue(wiring.Service.IsReady);
            Assert.IsTrue(wiring.Module.Unlock.IsUnlocked);
        }

        [Test]
        public void FirstInteractionBeforeRegister_IsRemembered_WhenUnlockRequired()
        {
            BootstrapGraph graph = CreateGraph(null);
            graph.FirstInteraction.NotifyUserGesture();
            AudioWiring wiring = CreateWiring(_config, _library, true);
            wiring.Register(graph);

            TickFor(graph, null, BootSeconds);

            Assert.IsTrue(wiring.Module.Unlock.IsUnlocked);
        }

        [Test]
        public void LifecyclePausedResumed_SuspendsAndRestores_UntilDisposed()
        {
            BootstrapGraph graph = CreateGraph(null);
            AudioWiring wiring = CreateWiring(_config, _library, false);
            wiring.Register(graph);
            TickFor(graph, null, BootSeconds);

            graph.SetPageVisible(false, PauseReason.Hidden);
            Assert.IsTrue(wiring.Module.Service.RuntimeState.IsSuspended);

            graph.SetPageVisible(true, PauseReason.Hidden);
            Assert.IsFalse(wiring.Module.Service.RuntimeState.IsSuspended);

            wiring.Dispose();
            graph.SetPageVisible(false, PauseReason.Hidden);
            Assert.IsFalse(wiring.Module.Service.RuntimeState.IsSuspended);
            Assert.IsFalse(wiring.IsRegistered);
        }

        [Test]
        public void MissingConfig_FailsAudioStep_WithoutBlockingBoot()
        {
            BootstrapGraph graph = CreateGraph(null);
            AudioWiring wiring = CreateWiring(null, _library, false);
            wiring.Register(graph);

            Assert.DoesNotThrow(() => TickFor(graph, null, BootSeconds));

            Assert.IsTrue(wiring.Module.ServiceStep.IsFailed);
            Assert.IsFalse(wiring.Service.IsReady);
            Assert.AreEqual(0, _container.GetComponents<AudioSource>().Length);
            Assert.AreEqual(AppLifecycleState.Running, graph.Lifecycle.State);
        }

        [Test]
        public void MissingLibrary_FailsAudioStep_WithoutSources()
        {
            BootstrapGraph graph = CreateGraph(null);
            AudioWiring wiring = CreateWiring(_config, null, false);
            wiring.Register(graph);

            TickFor(graph, null, BootSeconds);

            Assert.IsTrue(wiring.Module.ServiceStep.IsFailed);
            Assert.AreEqual(0, _container.GetComponents<AudioSource>().Length);
            Assert.AreEqual(AppLifecycleState.Running, graph.Lifecycle.State);
        }

        [Test]
        public void TickBeforeReady_DoesNotThrow()
        {
            AudioWiring wiring = CreateWiring(_config, _library, false);

            Assert.DoesNotThrow(() => wiring.Tick());
            Assert.DoesNotThrow(() => wiring.Dispose());
        }

        private AudioWiring CreateWiring(AudioConfig config, AudioCueLibrary library, bool requiresUnlock)
        {
            return new AudioWiring(
                config,
                library,
                _container.transform,
                new FakeAudioSettingsStore(),
                new FixedAudioRandom(),
                new AudioPlatformPolicy(requiresUnlock));
        }
    }
}
