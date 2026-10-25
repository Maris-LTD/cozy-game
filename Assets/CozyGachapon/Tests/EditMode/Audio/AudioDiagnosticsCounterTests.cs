using CozyGachapon.Audio;
using CozyGachapon.Contracts;
using NUnit.Framework;

namespace CozyGachapon.Tests.Audio
{
    public class AudioDiagnosticsCounterTests : AudioServiceFixture
    {
        private const string CueId = "Test.Plinko";
        private const string MissingId = "Test.Missing";
        private const float Interval = 0.1f;

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

        private void AssertStatsConsistent(int expectedRequests)
        {
            AudioStats stats = Service.Stats;
            Assert.AreEqual(expectedRequests, stats.Requests);
            Assert.AreEqual(stats.Requests, stats.Played + stats.DroppedTotal);
        }
    }
}
