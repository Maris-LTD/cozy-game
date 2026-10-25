using CozyGachapon.Audio;
using CozyGachapon.Contracts;
using NUnit.Framework;
using UnityEditor;

namespace CozyGachapon.Tests.Audio
{
    public class AudioSettingsPersistenceTests : AudioMuteFixture
    {
        private const float MusicSlider = 0.5f;
        private const float SfxSlider = 0.25f;
        private const float ConfigDefaultSlider = 0.6f;
        private const string ValidId = "Test.Valid";

        [Test]
        public void AC27_EmptyStore_UsesConfigDefaults_WithoutWriting()
        {
            InitializeWithMusic();
            AudioConfig config = DefaultAudioAssets.LoadConfig();

            Assert.AreEqual(config.DefaultUserMuted, Service.IsUserMuted);
            Assert.AreEqual(config.DefaultUserSlider, Service.GetUserVolume(AudioBus.Music));
            Assert.AreEqual(config.DefaultUserSlider, Service.GetUserVolume(AudioBus.Sfx));
            Assert.AreEqual(0, SettingsStore.UserMutedWriteCount);
            Assert.AreEqual(0, SettingsStore.SliderWriteCount);
        }

        [Test]
        public void AC27_EmptyStore_ReadsMutedAndSliderDefaultsFromConfig()
        {
            SerializedObject serialized = new SerializedObject(Config);
            serialized.FindProperty("_defaultUserMuted").boolValue = true;
            serialized.FindProperty("_defaultUserSlider").floatValue = ConfigDefaultSlider;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            InitializeWithCues(CreateCue(ValidId));

            Assert.IsTrue(Service.IsUserMuted);
            Assert.AreEqual(ConfigDefaultSlider, Service.GetUserVolume(AudioBus.Music));
            Assert.AreEqual(ConfigDefaultSlider, Service.GetUserVolume(AudioBus.Sfx));
            AssertDropped(DropReason.UserMuted, PlayAt(ValidId, 1d));
        }

        [Test]
        public void AC27_MutedIsRestoredOnNextAudio_BeforeAnyPlay()
        {
            InitializeWithMusic();
            Service.SetUserMuted(true);

            Assert.IsTrue(SettingsStore.HasUserMuted);
            Assert.IsTrue(SettingsStore.StoredUserMuted);

            FakeVoiceBackend backend = new FakeVoiceBackend(DefaultVoiceCount);
            FakeAudioClock clock = new FakeAudioClock();
            AudioService next = CreateServiceOnSameStore(backend, clock);
            next.MuteChanged += RecordMuteChanged;
            int changes = MuteChangedCount;

            Assert.IsTrue(next.IsUserMuted);
            Assert.AreEqual(0, next.Stats.Played);

            clock.Advance(1d);
            AssertPlayed(next.Play(ShopLoopId, AudioPlayParams.None));
            Assert.AreEqual(0f, backend.GetVolume(backend.LastStartedVoice));
            clock.Advance(UserMuteFadeSeconds + SettleMargin);
            next.Tick();
            Assert.AreEqual(0f, backend.GetVolume(backend.LastStartedVoice));
            Assert.AreEqual(1, next.Stats.Played);
            Assert.AreEqual(changes, MuteChangedCount);
        }

        [Test]
        public void Sliders_AreSavedAndReadBackOnNextAudio()
        {
            InitializeWithMusic();

            Service.SetUserVolume(AudioBus.Music, MusicSlider);
            Service.SetUserVolume(AudioBus.Sfx, SfxSlider);

            Assert.AreEqual(2, SettingsStore.SliderWriteCount);
            Assert.AreEqual(MusicSlider, SettingsStore.GetStoredSlider(AudioBus.Music));
            Assert.AreEqual(SfxSlider, SettingsStore.GetStoredSlider(AudioBus.Sfx));

            AudioService next = CreateServiceOnSameStore(new FakeVoiceBackend(DefaultVoiceCount), new FakeAudioClock());

            Assert.AreEqual(MusicSlider, next.GetUserVolume(AudioBus.Music));
            Assert.AreEqual(SfxSlider, next.GetUserVolume(AudioBus.Sfx));
            Assert.IsFalse(next.IsUserMuted);
        }

        [Test]
        public void SfxSliderAtZero_IsRestoredAsSilentBus()
        {
            InitializeWithMusic();
            Service.SetUserVolume(AudioBus.Sfx, 0f);
            FakeAudioClock clock = new FakeAudioClock();

            AudioService next = CreateServiceOnSameStore(new FakeVoiceBackend(DefaultVoiceCount), clock);

            Assert.AreEqual(0f, next.GetUserVolume(AudioBus.Sfx));
            clock.Advance(1d);
            AssertDropped(DropReason.BusSilent, next.Play(PegHitId, AudioPlayParams.None));
        }

        [Test]
        public void ClearedStore_FallsBackToDefaults()
        {
            InitializeWithMusic();
            Service.SetUserMuted(true);
            Service.SetUserVolume(AudioBus.Music, MusicSlider);
            SettingsStore.Clear();
            AudioConfig config = DefaultAudioAssets.LoadConfig();

            AudioService next = CreateServiceOnSameStore(new FakeVoiceBackend(DefaultVoiceCount), new FakeAudioClock());

            Assert.AreEqual(config.DefaultUserMuted, next.IsUserMuted);
            Assert.AreEqual(config.DefaultUserSlider, next.GetUserVolume(AudioBus.Music));
        }

        [Test]
        public void SliderOnOtherBus_IsNotWritten()
        {
            InitializeWithMusic();

            Service.SetUserVolume(AudioBus.Ui, MusicSlider);
            Service.SetUserVolume(AudioBus.Master, MusicSlider);

            Assert.AreEqual(0, SettingsStore.SliderWriteCount);
        }

        [Test]
        public void PrefilledStore_IsAppliedAtInitialize()
        {
            FakeAudioSettingsStore store = new FakeAudioSettingsStore();
            store.WriteUserMuted(true);
            store.WriteSlider(AudioBus.Music, MusicSlider);
            UseSettingsStore(store);

            InitializeWithMusic();

            Assert.IsTrue(Service.IsUserMuted);
            Assert.AreEqual(MusicSlider, Service.GetUserVolume(AudioBus.Music));
            Assert.AreEqual(0, MuteChangedCount);
            AssertDropped(DropReason.UserMuted, PlayAt(PegHitId, 1d));
        }
    }
}
