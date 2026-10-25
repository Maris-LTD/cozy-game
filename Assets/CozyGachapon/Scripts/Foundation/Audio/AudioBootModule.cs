using System;
using CozyGachapon.Contracts;

namespace CozyGachapon.Audio
{
    public sealed class AudioBootModule
    {
        public AudioService Service { get; }
        public AudioServiceBootStep ServiceStep { get; }

        public AudioBootModule(
            AudioConfig config,
            SoundCueRepository repository,
            IVoiceBackend backend,
            IAudioClock clock,
            IAudioDiagnosticsSink sink,
            IAudioRandom random,
            IAudioSettingsStore settingsStore)
        {
            Service = new AudioService();
            ServiceStep = new AudioServiceBootStep(Service, config, repository, backend, clock, sink, random, settingsStore);
        }

        public bool RegisterSteps(Func<IBootStep, bool> register)
        {
            return register(ServiceStep);
        }

        public void HandlePaused(in PausedEvent pausedEvent)
        {
            Service.HandlePaused(pausedEvent);
        }

        public void HandleResumed(in ResumedEvent resumedEvent)
        {
            Service.HandleResumed(resumedEvent);
        }

        public void HandleFirstInteraction(in FirstInteractionEvent interactionEvent)
        {
            Service.RuntimeState.MarkFirstInteraction();
        }
    }
}
