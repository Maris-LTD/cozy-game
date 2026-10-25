using CozyGachapon.Audio;
using CozyGachapon.Contracts;
using NUnit.Framework;

namespace CozyGachapon.Tests.Audio
{
    public class PlayAdmissionTests : AudioServiceFixture
    {
        private const string CoinSpawnId = "Plinko.CoinSpawn";
        private const string MissingCueId = "Khong.Ton.Tai";
        private const string CueId = "Test.Plinko";
        private const string KeepCueId = "Test.PlinkoKeep";
        private const string MusicCueId = "Test.Music";
        private const float Interval = 0.1f;
        private const double T0 = 1d;

        [Test]
        public void AC7_PlayBeforeInitialize_NotReady_ThenUnknownCueAfterInitialize()
        {
            PlayResult beforeInit = default;
            Assert.DoesNotThrow(() => beforeInit = Service.Play(CoinSpawnId, AudioPlayParams.None));
            AssertDropped(DropReason.NotReady, beforeInit);
            Assert.IsFalse(Service.IsReady);
            Assert.IsFalse(Service.TryResolveCue(CoinSpawnId, out _));

            InitializeWithDefaultAssets();
            Assert.IsTrue(Service.IsReady);

            PlayResult unknown = default;
            Assert.DoesNotThrow(() => unknown = Service.Play(MissingCueId, AudioPlayParams.None));
            AssertDropped(DropReason.UnknownCue, unknown);
            Assert.AreEqual(0, Backend.PlayingCount);
            Assert.AreEqual(0, Backend.StartCount);
            Assert.AreEqual(1, Sink.CountWarnings(AudioDiagnosticCode.UnknownCue));
            Assert.AreEqual(MissingCueId, Sink.LastWarningCueId);

            AudioStats stats = Service.Stats;
            Assert.AreEqual(2, stats.Requests);
            Assert.AreEqual(1, stats.DroppedNotReady);
            Assert.AreEqual(1, stats.DroppedUnknownCue);
            Assert.AreEqual(0, stats.Played);
        }

        [Test]
        public void AC7_NullAndOutOfRangeRequests_UnknownCue_NoVoiceStarted()
        {
            InitializeWithDefaultAssets();
            Assert.IsTrue(Service.TryResolveCue(CoinSpawnId, out int coinIndex));

            Assert.DoesNotThrow(() =>
            {
                AssertDropped(DropReason.UnknownCue, Service.Play(null, AudioPlayParams.None));
                AssertDropped(DropReason.UnknownCue, Service.Play(string.Empty, AudioPlayParams.None));
                AssertDropped(DropReason.UnknownCue, Service.Play(-1, AudioPlayParams.None));
                AssertDropped(DropReason.UnknownCue, Service.Play(int.MaxValue, AudioPlayParams.None));
            });

            Assert.AreEqual(0, Backend.StartCount);
            Assert.AreEqual(4, Service.Stats.DroppedUnknownCue);
            AssertPlayed(Service.Play(coinIndex, AudioPlayParams.None));
        }

        [Test]
        public void AC10_Locked_DropsWithLocked()
        {
            InitializeSingleCue();
            Service.RuntimeState.SetLocked(true);

            AssertDropped(DropReason.Locked, PlayAt(CueId, T0));
        }

        [TestCase(MuteReason.Ad)]
        [TestCase(MuteReason.Background)]
        public void AC10_Suspended_DropsWithSuspended(MuteReason reason)
        {
            InitializeSingleCue();
            Service.RuntimeState.SetSuspendReason(reason, true);

            AssertDropped(DropReason.Suspended, PlayAt(CueId, T0));
        }

        [Test]
        public void AC10_UserMuted_DropsSfxCue()
        {
            InitializeSingleCue();
            Service.RuntimeState.SetUserMuted(true);

            AssertDropped(DropReason.UserMuted, PlayAt(CueId, T0));
        }

        [Test]
        public void AC10_UserMuted_DoesNotApplyToMusicCue()
        {
            SoundCue music = CreateLimitedCue(MusicCueId, AudioBus.Music, 1, Interval, InstanceCapPolicy.DropNew);
            InitializeWithCues(music);
            Service.RuntimeState.SetUserMuted(true);

            AssertPlayed(PlayAt(MusicCueId, T0));
            Assert.Less(Backend.LastStartedVoice, Config.MusicVoices);
        }

        [TestCase(AudioBus.Plinko)]
        [TestCase(AudioBus.Sfx)]
        [TestCase(AudioBus.Master)]
        public void AC10_BusSilentOnCuePath_DropsWithBusSilent(AudioBus silentBus)
        {
            InitializeSingleCue();
            Service.RuntimeState.SetBusSilent(silentBus, true);

            AssertDropped(DropReason.BusSilent, PlayAt(CueId, T0));
        }

        [Test]
        public void AC10_BusSilentOffCuePath_StillPlays()
        {
            InitializeSingleCue();
            Service.RuntimeState.SetBusSilent(AudioBus.Ui, true);

            AssertPlayed(PlayAt(CueId, T0));
        }

        [Test]
        public void AC10_BackgroundBus_FiltersOnlyCuesThatDoNotKeep()
        {
            SoundCue filtered = CreateLimitedCue(CueId, AudioBus.Plinko, 1, Interval, InstanceCapPolicy.DropNew);
            SoundCue kept = CreateLimitedCue(KeepCueId, AudioBus.Plinko, 1, Interval, InstanceCapPolicy.DropNew);
            SetKeepWhenBusBackground(kept, true);
            InitializeWithCues(filtered, kept);
            Service.RuntimeState.SetBusBackground(AudioBus.Plinko, true);

            AssertDropped(DropReason.BackgroundFiltered, PlayAt(CueId, T0));
            AssertPlayed(PlayAt(KeepCueId, T0));
        }

        [Test]
        public void AC10_RateLimited_DropsWithinMinInterval()
        {
            InitializeSingleCue();
            AssertPlayed(PlayAt(CueId, T0));

            AssertDropped(DropReason.RateLimited, PlayAt(CueId, T0 + Interval * 0.5d));
        }

        [Test]
        public void AC10_InstanceCap_DropNewAtCapAfterMinInterval()
        {
            InitializeSingleCue();
            AssertPlayed(PlayAt(CueId, T0));

            AssertDropped(DropReason.InstanceCap, PlayAt(CueId, T0 + Interval * 2d));
            Assert.AreEqual(1, Backend.PlayingCount);
        }

        [Test]
        public void AC10_NoVoice_WhenEverySfxVoiceIsBusy()
        {
            int sfxVoices = Config.TotalVoices - Config.MusicVoices - Config.AmbienceVoices;
            SoundCue cue = CreateLimitedCue(CueId, AudioBus.Plinko, sfxVoices * 2, 0f, InstanceCapPolicy.DropNew);
            InitializeWithCues(cue);
            for (int i = 0; i < sfxVoices; i++)
            {
                AssertPlayed(PlayAt(CueId, T0));
            }

            AssertDropped(DropReason.NoVoice, PlayAt(CueId, T0));
            Assert.AreEqual(sfxVoices, Backend.PlayingCount);
        }

        [Test]
        public void AC10_EveryStepFailing_ReportsLowestStepFirst_ThenNextAsEachIsCleared()
        {
            InitializeSingleCue();
            AssertPlayed(PlayAt(CueId, T0));
            int voice = Backend.LastStartedVoice;
            AudioRuntimeState state = Service.RuntimeState;
            state.SetLocked(true);
            state.SetSuspendReason(MuteReason.Ad, true);
            state.SetUserMuted(true);
            state.SetBusSilent(AudioBus.Plinko, true);
            state.SetBusBackground(AudioBus.Plinko, true);

            AssertDropped(DropReason.Locked, PlayAt(CueId, T0));
            state.SetLocked(false);
            AssertDropped(DropReason.Suspended, PlayAt(CueId, T0));
            state.SetSuspendReason(MuteReason.Ad, false);
            AssertDropped(DropReason.UserMuted, PlayAt(CueId, T0));
            state.SetUserMuted(false);
            AssertDropped(DropReason.BusSilent, PlayAt(CueId, T0));
            state.SetBusSilent(AudioBus.Plinko, false);
            AssertDropped(DropReason.BackgroundFiltered, PlayAt(CueId, T0));
            state.SetBusBackground(AudioBus.Plinko, false);
            AssertDropped(DropReason.RateLimited, PlayAt(CueId, T0));
            AssertDropped(DropReason.InstanceCap, PlayAt(CueId, T0 + Interval * 2d));

            Backend.FinishVoice(voice);
            Service.Tick();
            AssertPlayed(PlayAt(CueId, T0 + Interval * 2d));
        }

        [Test]
        public void AC10_SuspendedWithBusSilentAndInstanceCap_ReportsSuspended()
        {
            InitializeSingleCue();
            AssertPlayed(PlayAt(CueId, T0));
            Service.RuntimeState.SetSuspendReason(MuteReason.Background, true);
            Service.RuntimeState.SetBusSilent(AudioBus.Plinko, true);

            AssertDropped(DropReason.Suspended, PlayAt(CueId, T0 + Interval * 2d));
        }

        private void InitializeSingleCue()
        {
            InitializeWithCues(CreateLimitedCue(CueId, AudioBus.Plinko, 1, Interval, InstanceCapPolicy.DropNew));
        }
    }
}
