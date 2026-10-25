using CozyGachapon.Tests.Pooling;
using NUnit.Framework;

namespace CozyGachapon.Tests.Audio
{
    public class MusicStormTests : MusicStartFixture
    {
        [Test]
        public void AC26_Mvp_NoMusicCue_StormNoError_MusicVoiceFree()
        {
            Boot(false);
            Unlock();
            MarkInteractive();
            Module.ServiceStep.Run();
            TickAt(StartTime);

            RunStorm();

            Assert.AreEqual(0, Sink.ErrorCount);
            Assert.AreEqual(0, MusicVoiceStartTotal());
            for (int voice = 0; voice < MusicConfig.MusicVoices; voice++)
            {
                Assert.IsFalse(MusicBackend.IsPlaying(voice));
            }
        }

        [Test]
        public void AC26_Vs_MusicSurvivesStorm()
        {
            Boot(true);
            Unlock();
            MarkInteractive();
            Module.ServiceStep.Run();
            TickAt(StartTime);
            int voice = MusicBackend.LastStartedVoice;
            UnityEngine.AudioClip clip = MusicBackend.GetClip(voice);

            RunStorm();

            Assert.IsTrue(MusicBackend.IsPlaying(voice));
            Assert.AreEqual(0, MusicBackend.GetStopCount(voice));
            Assert.AreEqual(1, MusicBackend.GetStartCount(voice));
            Assert.AreSame(clip, MusicBackend.GetClip(voice));
            Assert.AreEqual(0, Sink.ErrorCount);
        }

        [Test]
        public void Tick_MusicStartAndFade_DoesNotAllocate()
        {
            Boot(true);
            Unlock();
            MarkInteractive();
            Module.ServiceStep.Run();
            Clock.Set(StartTime);

            int allocations = GcAllocProbe.CountAllocations(RunFadeTicks);

            Assert.AreEqual(0, allocations);
            Assert.AreEqual(1, MusicBackend.StartCount);
        }

        [Test]
        public void Tick_MvpWithoutMusicCue_DoesNotAllocate()
        {
            Boot(false);
            Unlock();
            MarkInteractive();
            Module.ServiceStep.Run();
            Clock.Set(StartTime);

            int allocations = GcAllocProbe.CountAllocations(RunFadeTicks);

            Assert.AreEqual(0, allocations);
            Assert.AreEqual(0, MusicBackend.StartCount);
        }
    }
}
