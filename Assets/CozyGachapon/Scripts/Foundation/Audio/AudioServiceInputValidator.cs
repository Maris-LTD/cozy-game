using CozyGachapon.Contracts;

namespace CozyGachapon.Audio
{
    public static class AudioServiceInputValidator
    {
        public static bool HasValidInputs(
            AudioConfig config,
            SoundCueRepository repository,
            IVoiceBackend backend,
            IAudioClock clock,
            IAudioDiagnosticsSink sink,
            IAudioRandom random,
            IAudioSettingsStore settingsStore)
        {
            if (config == null || repository == null || backend == null || clock == null || sink == null || random == null
                || settingsStore == null)
            {
                return false;
            }

            if (config.TotalVoices < 1 || config.MusicVoices < 0 || config.AmbienceVoices < 0)
            {
                return false;
            }

            return config.MusicVoices + config.AmbienceVoices <= config.TotalVoices
                && backend.VoiceCount >= config.TotalVoices;
        }
    }
}
