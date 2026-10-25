using CozyGachapon.Audio;
using CozyGachapon.Contracts;
using CozyGachapon.Tests.Pooling;
using NUnit.Framework;

namespace CozyGachapon.Tests.Audio
{
    public class AudioDiagnosticsCounterTests : AudioServiceFixture
    {
        private const string CueId = "Test.Plinko";
        private const string MissingId = "Test.Missing";
        private const float Interval = 0.1f;
        private const string LowId = "Test.Low";
        private const string HighId = "Test.High";
        private const int LowPriority = 10;
        private const int HighPriority = 90;
        private const int StatsReadIterations = 1000;
        private const double AgeTolerance = 1e-6;

        [Test]
        public void EveryResult_IncrementsExactlyOneCounter()
        {
            AssertDropped(DropReason.NotReady, Service.Play(CueId, AudioPlayParams.None));
            AssertStatsConsistent(1);

            InitializeWithCues(CreateLimitedCue(CueId, AudioBus.Plinko, 1, Interval, InstanceCapPolicy.DropNew));
            AudioRuntimeState state = Service.RuntimeState;

            AssertDropped(DropReason.UnknownCue, PlayAt(MissingId, 1d));
            AssertStatsConsistent(2);

            state.SetLocked(true);
            AssertDropped(DropReason.Locked, PlayAt(CueId, 1d));
            AssertStatsConsistent(3);
            state.SetLocked(false);

            state.SetSuspendReason(MuteReason.Background, true);
            AssertDropped(DropReason.Suspended, PlayAt(CueId, 1d));
            AssertStatsConsistent(4);
            state.SetSuspendReason(MuteReason.Background, false);

            state.SetUserMuted(true);
            AssertDropped(DropReason.UserMuted, PlayAt(CueId, 1d));
            AssertStatsConsistent(5);
            state.SetUserMuted(false);

            state.SetBusSilent(AudioBus.Gameplay, true);
            AssertDropped(DropReason.BusSilent, PlayAt(CueId, 1d));
            AssertStatsConsistent(6);
            state.SetBusSilent(AudioBus.Gameplay, false);

            state.SetBusBackground(AudioBus.Plinko, true);
            AssertDropped(DropReason.BackgroundFiltered, PlayAt(CueId, 1d));
            AssertStatsConsistent(7);
            state.SetBusBackground(AudioBus.Plinko, false);

            AssertPlayed(PlayAt(CueId, 1d));
            AssertStatsConsistent(8);
            AssertDropped(DropReason.RateLimited, PlayAt(CueId, 1.05d));
            AssertStatsConsistent(9);
            AssertDropped(DropReason.InstanceCap, PlayAt(CueId, 1.5d));
            AssertStatsConsistent(10);

            AudioStats stats = Service.Stats;
            Assert.AreEqual(1, stats.Played);
            Assert.AreEqual(1, stats.ActiveVoices);
            Assert.AreEqual(Backend.PlayingCount, stats.ActiveVoices);
            Assert.AreEqual(0, stats.DroppedNoVoice);
            Assert.AreEqual(0, stats.GetDropped(DropReason.None));
            DropReason[] once =
            {
                DropReason.NotReady,
                DropReason.UnknownCue,
                DropReason.Locked,
                DropReason.Suspended,
                DropReason.UserMuted,
                DropReason.BusSilent,
                DropReason.BackgroundFiltered,
                DropReason.RateLimited,
                DropReason.InstanceCap
            };
            for (int i = 0; i < once.Length; i++)
            {
                Assert.AreEqual(1, stats.GetDropped(once[i]), once[i].ToString());
            }
        }

        [Test]
        public void ActiveVoices_FollowsBackend_AfterTickReclaimsFinishedVoice()
        {
            InitializeWithCues(CreateLimitedCue(CueId, AudioBus.Plinko, 1, Interval, InstanceCapPolicy.DropNew));
            AssertPlayed(PlayAt(CueId, 1d));
            Assert.AreEqual(1, Service.Stats.ActiveVoices);

            Backend.FinishVoice(Backend.LastStartedVoice);
            Service.Tick();

            Assert.AreEqual(0, Service.Stats.ActiveVoices);
            Assert.AreEqual(Backend.PlayingCount, Service.Stats.ActiveVoices);
        }

        [Test]
        public void PeakVoices_KeepsMaximum_AfterVoicesFinish()
        {
            const int Instances = 3;
            InitializeWithCues(CreateLimitedCue(CueId, AudioBus.Plinko, Instances, 0f, InstanceCapPolicy.DropNew));
            int[] voices = new int[Instances];
            for (int i = 0; i < Instances; i++)
            {
                voices[i] = PlayVoiceAt(CueId, 1d + i);
            }

            Assert.AreEqual(Instances, Service.Stats.PeakVoices);
            for (int i = 0; i < Instances; i++)
            {
                Backend.FinishVoice(voices[i]);
            }

            Service.Tick();

            Assert.AreEqual(0, Service.Stats.ActiveVoices);
            Assert.AreEqual(Instances, Service.Stats.PeakVoices);
        }

        [Test]
        public void MinSameCueStealAge_RecordsSmallestVictimAge()
        {
            const double LongAge = 0.5d;
            const double ShortAge = 0.2d;
            InitializeWithCues(CreateLimitedCue(CueId, AudioBus.Plinko, 1, 0f, InstanceCapPolicy.StealOldest));
            AssertPlayed(PlayAt(CueId, 1d));
            Assert.AreEqual(0d, Service.Stats.MinSameCueStealAgeSeconds);

            AssertPlayed(PlayAt(CueId, 1d + LongAge));
            Assert.AreEqual(LongAge, Service.Stats.MinSameCueStealAgeSeconds, AgeTolerance);

            AssertPlayed(PlayAt(CueId, 1d + LongAge + ShortAge));

            Assert.AreEqual(2, Service.Stats.SameCueSteals);
            Assert.AreEqual(ShortAge, Service.Stats.MinSameCueStealAgeSeconds, AgeTolerance);
            Assert.AreEqual(0, Service.Stats.PrioritySteals);
        }

        [Test]
        public void PrioritySteals_CountsPriorityStealOnly()
        {
            int sfxVoices = Config.TotalVoices - Config.MusicVoices - Config.AmbienceVoices;
            SoundCue low = CreateLimitedCue(LowId, AudioBus.Plinko, sfxVoices, 0f, InstanceCapPolicy.DropNew);
            low.SetLimitsForTest(LowPriority, sfxVoices, 0f);
            SoundCue high = CreateLimitedCue(HighId, AudioBus.Plinko, 1, 0f, InstanceCapPolicy.DropNew);
            high.SetLimitsForTest(HighPriority, 1, 0f);
            InitializeWithCues(low, high);
            for (int i = 0; i < sfxVoices; i++)
            {
                AssertPlayed(PlayAt(LowId, 1d + i));
            }

            Assert.AreEqual(0, Service.Stats.PrioritySteals);
            AssertPlayed(PlayAt(HighId, 1d + sfxVoices));

            Assert.AreEqual(1, Service.Stats.PrioritySteals);
            Assert.AreEqual(0, Service.Stats.SameCueSteals);
            Assert.AreEqual(sfxVoices, Service.Stats.PeakVoices);
        }

        [Test]
        public void Stats_ReadRepeatedly_IsStableAndDoesNotAllocate()
        {
            InitializeWithCues(CreateLimitedCue(CueId, AudioBus.Plinko, 1, 0f, InstanceCapPolicy.StealOldest));
            AssertPlayed(PlayAt(CueId, 1d));
            AssertPlayed(PlayAt(CueId, 2d));
            AssertDropped(DropReason.UnknownCue, PlayAt(MissingId, 2d));
            AudioStats first = Service.Stats;
            AudioStats second = Service.Stats;
            AudioService service = Service;

            int allocations = GcAllocProbe.CountAllocations(() =>
            {
                for (int i = 0; i < StatsReadIterations; i++)
                {
                    AudioStats stats = service.Stats;
                    stats.GetDropped(DropReason.RateLimited);
                }
            });

            Assert.AreEqual(0, allocations);
            Assert.AreEqual(first.Requests, second.Requests);
            Assert.AreEqual(first.Played, second.Played);
            Assert.AreEqual(first.DroppedUnknownCue, second.DroppedUnknownCue);
            Assert.AreEqual(first.SameCueSteals, second.SameCueSteals);
            Assert.AreEqual(first.PeakVoices, second.PeakVoices);
            Assert.AreEqual(first.MinSameCueStealAgeSeconds, second.MinSameCueStealAgeSeconds);
            Assert.AreEqual(first.Requests, service.Stats.Requests);
        }

        private void AssertStatsConsistent(int expectedRequests)
        {
            AudioStats stats = Service.Stats;
            Assert.AreEqual(expectedRequests, stats.Requests);
            Assert.AreEqual(stats.Requests, stats.Played + stats.DroppedTotal);
        }
    }
}
