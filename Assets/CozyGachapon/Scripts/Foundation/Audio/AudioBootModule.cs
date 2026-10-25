using System;
using CozyGachapon.Contracts;

namespace CozyGachapon.Audio
{
    public sealed class AudioBootModule
    {
        public AudioService Service { get; }
        public AudioServiceBootStep ServiceStep { get; }
        public AudioUnlockController Unlock { get; }

        public AudioBootModule(
            AudioConfig config,
            SoundCueRepository repository,
            IVoiceBackend backend,
            IAudioClock clock,
            IAudioDiagnosticsSink sink,
            IAudioRandom random,
            IAudioSettingsStore settingsStore,
            AudioPlatformPolicy platformPolicy)
        {
            Service = new AudioService();
            Unlock = new AudioUnlockController(Service.RuntimeState, platformPolicy);
            ServiceStep = new AudioServiceBootStep(
                Service, config, repository, backend, clock, sink, random, settingsStore, Unlock);
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
            Unlock.HandleFirstInteraction();
        }

        public void HandleInteractive(in InteractiveEvent interactiveEvent)
        {
            Service.RuntimeState.MarkInteractive();
        }
    }
}
