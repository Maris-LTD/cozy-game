using CozyGachapon.Audio;
using CozyGachapon.Contracts;

namespace CozyGachapon.Tests.Audio
{
    public abstract class MusicStartFixture : AudioBootFixture
    {
        protected const double StartTime = 10d;
        protected const double StormStart = 100d;
        protected const double TickStep = 0.05d;
        protected const int FadeTicks = 80;

        protected AudioConfig MusicConfig { get; private set; }
        protected FakeVoiceBackend MusicBackend { get; private set; }
        protected AudioBootModule Module { get; private set; }

        protected void Boot(bool withMusic, bool withClip = true)
        {
            MusicConfig = LoadDefaultConfig();
            MusicBackend = new FakeVoiceBackend(MusicConfig.TotalVoices);
            Module = withMusic
                ? CreateModule(MusicConfig, MusicBackend, true, CreateShopLoopCue(withClip))
                : CreateModule(MusicConfig, MusicBackend, true);
        }

        protected void TickAt(double now)
        {
            Clock.Set(now);
            Module.Service.Tick();
        }

        protected void Unlock()
        {
            Module.HandleFirstInteraction(new FirstInteractionEvent());
        }

        protected void MarkInteractive()
        {
            Module.HandleInteractive(new InteractiveEvent(1f));
        }

        protected int MusicVoiceStartTotal()
        {
            int total = 0;
            for (int voice = 0; voice < MusicConfig.MusicVoices; voice++)
            {
                total += MusicBackend.GetStartCount(voice);
            }

            return total;
        }

        protected void RunStorm()
        {
            AudioStormShape shape = AudioStormShape.FromTargets(
                DefaultAudioAssets.LoadTargets(), DefaultAudioAssets.TapCooldownSeconds);
            new AudioStormDriver(Module.Service, Clock, MusicBackend, shape).Run(StormStart);
        }

        protected void RunFadeTicks()
        {
            for (int i = 0; i < FadeTicks; i++)
            {
                Clock.Advance(TickStep);
                Module.Service.Tick();
            }
        }
    }
}
