using CozyGachapon.Audio;
using CozyGachapon.Contracts;
using CozyGachapon.Foundation;
using UnityEngine;

namespace CozyGachapon
{
    public sealed class AudioWiring
    {
        private BootstrapGraph _graph;

        public AudioBootModule Module { get; private set; }
        public IAudioService Service { get; private set; }
        public UnityVoiceBackend Voices { get; private set; }
        public AudioDiagnosticsRecorder Diagnostics { get; private set; }
        public bool IsRegistered => _graph != null;

        public AudioWiring(
            AudioConfig config,
            AudioCueLibrary library,
            Transform voiceContainer,
            IAudioSettingsStore settingsStore,
            IAudioRandom random,
            AudioPlatformPolicy platformPolicy)
        {
            Voices = new UnityVoiceBackend(voiceContainer, config != null ? config.TotalVoices : 0);
            Diagnostics = new AudioDiagnosticsRecorder();
            SoundCueRepository repository = library != null ? new SoundCueRepository(library.Cues) : null;
            Module = new AudioBootModule(
                config, repository, Voices, new UnityAudioClock(), Diagnostics, random, settingsStore, platformPolicy);
            Service = Module.Service;
        }

        public bool Register(BootstrapGraph graph)
        {
            if (graph == null || _graph != null)
            {
                return false;
            }

            _graph = graph;
            bool registered = Module.RegisterSteps(graph.RegisterStep);
            graph.Lifecycle.OnPaused += HandlePaused;
            graph.Lifecycle.OnResumed += HandleResumed;
            graph.FirstInteraction.OnFirstInteraction += HandleFirstInteraction;
            graph.Curtain.OnInteractive += HandleInteractive;
            if (graph.FirstInteraction.HasFired)
            {
                Module.HandleFirstInteraction(new FirstInteractionEvent());
            }

            return registered;
        }

        public void Tick()
        {
            Module.Service.Tick();
        }

        public void Dispose()
        {
            if (_graph == null)
            {
                return;
            }

            _graph.Lifecycle.OnPaused -= HandlePaused;
            _graph.Lifecycle.OnResumed -= HandleResumed;
            _graph.FirstInteraction.OnFirstInteraction -= HandleFirstInteraction;
            _graph.Curtain.OnInteractive -= HandleInteractive;
            _graph = null;
        }

        private void HandlePaused(PausedEvent pausedEvent)
        {
            Module.HandlePaused(pausedEvent);
        }

        private void HandleResumed(ResumedEvent resumedEvent)
        {
            Module.HandleResumed(resumedEvent);
        }

        private void HandleFirstInteraction(FirstInteractionEvent interactionEvent)
        {
            Module.HandleFirstInteraction(interactionEvent);
        }

        private void HandleInteractive(InteractiveEvent interactiveEvent)
        {
            Module.HandleInteractive(interactiveEvent);
        }
    }
}
