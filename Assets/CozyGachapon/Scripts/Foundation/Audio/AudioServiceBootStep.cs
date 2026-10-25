using System;
using System.Collections.Generic;
using CozyGachapon.Contracts;

namespace CozyGachapon.Audio
{
    public sealed class AudioServiceBootStep : IBootStep
    {
        public const string StepId = "audio.service";

        private readonly AudioService _service;
        private readonly AudioConfig _config;
        private readonly SoundCueRepository _repository;
        private readonly IVoiceBackend _backend;
        private readonly IAudioClock _clock;
        private readonly IAudioDiagnosticsSink _sink;
        private readonly IAudioRandom _random;
        private readonly IAudioSettingsStore _settingsStore;

        public string Id => StepId;
        public BootPhase Phase => BootPhase.ServiceInit;
        public IReadOnlyList<string> RunsAfter => Array.Empty<string>();
        public bool IsCritical => false;
        public bool IsDone { get; private set; }
        public bool IsFailed { get; private set; }

        public AudioServiceBootStep(
            AudioService service,
            AudioConfig config,
            SoundCueRepository repository,
            IVoiceBackend backend,
            IAudioClock clock,
            IAudioDiagnosticsSink sink,
            IAudioRandom random,
            IAudioSettingsStore settingsStore)
        {
            _service = service;
            _config = config;
            _repository = repository;
            _backend = backend;
            _clock = clock;
            _sink = sink;
            _random = random;
            _settingsStore = settingsStore;
        }

        public void Run()
        {
            if (IsDone || IsFailed)
            {
                return;
            }

            try
            {
                if (_service.Initialize(_config, _repository, _backend, _clock, _sink, _random, _settingsStore) || _service.IsReady)
                {
                    IsDone = true;
                    return;
                }

                Fail();
            }
            catch (Exception)
            {
                Fail();
                ReportException();
            }
        }

        private void Fail()
        {
            IsFailed = true;
            try
            {
                _service.EnterFailed(_settingsStore);
            }
            catch (Exception)
            {
            }
        }

        private void ReportException()
        {
            try
            {
                if (_sink != null)
                {
                    _sink.ReportError(AudioDiagnosticCode.InitializeException, null);
                }
            }
            catch (Exception)
            {
            }
        }
    }
}
