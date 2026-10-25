using System.Collections.Generic;
using CozyGachapon.Audio;
using CozyGachapon.Contracts;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace CozyGachapon.Tests.Audio
{
    public class DefaultCueTableTests
    {
        private const float Epsilon = 0.0001f;
        private const float TokMaxLengthSeconds = 0.116f;
        private const int ExpectedCueCount = 18;

        private AudioConfig _config;
        private AudioBalanceTargets _targets;
        private List<SoundCue> _cues;

        [SetUp]
        public void SetUp()
        {
            _config = DefaultAudioAssets.LoadConfig();
            _targets = DefaultAudioAssets.LoadTargets();
            _cues = DefaultAudioAssets.LoadCues();
        }

        private SoundCue Find(string cueId)
        {
            for (int i = 0; i < _cues.Count; i++)
            {
                if (_cues[i].CueId == cueId)
                {
                    return _cues[i];
                }
            }

            return null;
        }

        [Test]
        public void DefaultAssetsExist()
        {
            Assert.IsNotNull(_config);
            Assert.IsNotNull(_targets);
            Assert.AreEqual(ExpectedCueCount, _cues.Count);
        }

        [Test]
        public void CueTableHasExactlyTheMvpRows()
        {
            string[] expected =
            {
                "Plinko.JackpotHit", "Gacha.RarityReveal.common", "Gacha.RarityReveal.rare", "Gacha.Crank",
                "Gacha.EggPop", "Plinko.JackpotBurstSpawn", "Plinko.BucketX5", "Plinko.BucketX2",
                "Plinko.BucketX1", "Ui.Confirm", "Ui.SoundOn", "Ui.Tap", "Plinko.PegHitScored",
                "Plinko.CoinSpawn", "Plinko.CoinSpawnAuto", "Plinko.CoinFizzle", "Plinko.BoardBusy",
                "Plinko.PegHitUnscored"
            };

            for (int i = 0; i < expected.Length; i++)
            {
                Assert.IsNotNull(Find(expected[i]), expected[i]);
            }

            Assert.IsNull(Find("Plinko.RainLoop"));
            Assert.IsNull(Find("Music.ShopLoop"));
        }

        [TestCase("Plinko.JackpotHit", AudioBus.Plinko, -6f, 100, 2, 0.10f, InstanceCapPolicy.DropNew, PitchMode.Random, 0.02f, true)]
        [TestCase("Gacha.RarityReveal.common", AudioBus.Gacha, -4f, 90, 1, 0f, InstanceCapPolicy.StealOldest, PitchMode.None, 0f, false)]
        [TestCase("Gacha.RarityReveal.rare", AudioBus.Gacha, -4f, 90, 1, 0f, InstanceCapPolicy.StealOldest, PitchMode.None, 0f, false)]
        [TestCase("Gacha.Crank", AudioBus.Gacha, -8f, 90, 1, 0.05f, InstanceCapPolicy.StealOldest, PitchMode.Random, 0.03f, false)]
        [TestCase("Gacha.EggPop", AudioBus.Gacha, -6f, 90, 1, 0.05f, InstanceCapPolicy.StealOldest, PitchMode.Random, 0.03f, false)]
        [TestCase("Plinko.JackpotBurstSpawn", AudioBus.Plinko, -12f, 85, 3, 0.06f, InstanceCapPolicy.StealOldest, PitchMode.Scale, 0.01f, false)]
        [TestCase("Plinko.BucketX5", AudioBus.Plinko, -8f, 80, 3, 0.03f, InstanceCapPolicy.StealOldest, PitchMode.Random, 0.02f, true)]
        [TestCase("Plinko.BucketX2", AudioBus.Plinko, -9f, 80, 3, 0.03f, InstanceCapPolicy.StealOldest, PitchMode.Random, 0.02f, true)]
        [TestCase("Plinko.BucketX1", AudioBus.Plinko, -10f, 80, 3, 0.03f, InstanceCapPolicy.StealOldest, PitchMode.Random, 0.02f, true)]
        [TestCase("Ui.Confirm", AudioBus.Ui, -10f, 70, 2, 0.10f, InstanceCapPolicy.DropNew, PitchMode.None, 0f, false)]
        [TestCase("Ui.SoundOn", AudioBus.Ui, -10f, 70, 2, 0.10f, InstanceCapPolicy.DropNew, PitchMode.None, 0f, false)]
        [TestCase("Ui.Tap", AudioBus.Ui, -14f, 70, 2, 0.05f, InstanceCapPolicy.StealOldest, PitchMode.Random, 0.02f, false)]
        [TestCase("Plinko.PegHitScored", AudioBus.Plinko, -14f, 60, 6, 0.04f, InstanceCapPolicy.StealOldest, PitchMode.Scale, 0.03f, false)]
        [TestCase("Plinko.CoinSpawn", AudioBus.Plinko, -14f, 50, 1, 0.05f, InstanceCapPolicy.DropNew, PitchMode.Random, 0.03f, false)]
        [TestCase("Plinko.CoinSpawnAuto", AudioBus.Plinko, -20f, 50, 1, 0.10f, InstanceCapPolicy.DropNew, PitchMode.Random, 0.03f, false)]
        [TestCase("Plinko.CoinFizzle", AudioBus.Plinko, -22f, 45, 1, 0.10f, InstanceCapPolicy.DropNew, PitchMode.Random, 0.03f, false)]
        [TestCase("Plinko.BoardBusy", AudioBus.Plinko, -22f, 40, 1, 0.10f, InstanceCapPolicy.DropNew, PitchMode.None, 0f, false)]
        [TestCase("Plinko.PegHitUnscored", AudioBus.Plinko, -26f, 10, 2, 0.10f, InstanceCapPolicy.StealOldest, PitchMode.Random, 0.05f, false)]
        public void CueRowMatchesTheCueTable(
            string cueId,
            AudioBus bus,
            float volumeDb,
            int priority,
            int maxInstances,
            float minInterval,
            InstanceCapPolicy policy,
            PitchMode pitchMode,
            float jitter,
            bool keep)
        {
            SoundCue cue = Find(cueId);

            Assert.IsNotNull(cue, cueId);
            Assert.AreEqual(bus, cue.Bus);
            Assert.AreEqual(volumeDb, cue.VolumeDb, Epsilon);
            Assert.AreEqual(priority, cue.Priority);
            Assert.AreEqual(maxInstances, cue.MaxInstances);
            Assert.AreEqual(minInterval, cue.MinInterval, Epsilon);
            Assert.AreEqual(policy, cue.InstanceCapPolicy);
            Assert.AreEqual(pitchMode, cue.PitchMode);
            Assert.AreEqual(jitter, cue.PitchJitterRatio, Epsilon);
            Assert.AreEqual(keep, cue.KeepWhenBusBackground);
        }

        [Test]
        public void EveryCueBusHasExactlyOnePathEndingAtMaster()
        {
            for (int i = 0; i < _cues.Count; i++)
            {
                AudioBus current = _cues[i].Bus;
                Assert.IsTrue(AudioBusPath.TryGetPathLength(current, out int hops), _cues[i].CueId);
                Assert.Greater(hops, 0, _cues[i].CueId);

                for (int step = 0; step < hops; step++)
                {
                    Assert.IsTrue(AudioBusTable.TryGetParent(current, out AudioBus parent));
                    current = parent;
                }

                Assert.AreEqual(AudioBus.Master, current, _cues[i].CueId);
                Assert.IsTrue(AudioBusPath.TryGetPathDb(_config, _cues[i].Bus, out _), _cues[i].CueId);
            }
        }

        [Test]
        public void MusicBusIsOffsetFromSfxByTheConfiguredAmount()
        {
            float offset = _config.GetBusDefaultDb(AudioBus.Music) - _config.GetBusDefaultDb(AudioBus.Sfx);

            Assert.AreEqual(_config.MusicOffsetFromSfxDb, offset, Epsilon);
            Assert.AreEqual(-12f, offset, Epsilon);
        }

        [Test]
        public void SpawnClipsAreTheShortTokClip()
        {
            string[] spawnIds = { "Plinko.CoinSpawn", "Plinko.CoinSpawnAuto" };
            for (int i = 0; i < spawnIds.Length; i++)
            {
                SoundCue cue = Find(spawnIds[i]);
                Assert.AreEqual(1, cue.Clips.Count, spawnIds[i]);
                Assert.AreEqual("tok", cue.Clips[0].name);
                Assert.LessOrEqual(cue.Clips[0].length, TokMaxLengthSeconds);
                Assert.Greater(cue.Clips[0].length, 0f);
            }
        }

        [TestCase("Plinko.PegHitScored", "marimba")]
        [TestCase("Plinko.JackpotBurstSpawn", "chime")]
        public void ScaleCuesCarryThreeShortSamplesAtNativeSemitones(string cueId, string clipPrefix)
        {
            SoundCue cue = Find(cueId);
            int[] expectedNative = { 4, 12, 19 };

            Assert.AreEqual(expectedNative.Length, cue.PitchedSamples.Count);
            for (int i = 0; i < expectedNative.Length; i++)
            {
                PitchedSample sample = cue.PitchedSamples[i];
                Assert.AreEqual(expectedNative[i], sample.NativeSemitone);
                Assert.IsTrue(sample.Clip.name.StartsWith(clipPrefix), sample.Clip.name);
                Assert.LessOrEqual(sample.Clip.length, _targets.PegClipLengthMaxSeconds);
            }
        }

        [Test]
        public void PlaceholderClipsShipWithALicenseNote()
        {
            string licensePath = DefaultAudioAssets.ClipsFolder + "/LICENSE.txt";
            TextAsset license = AssetDatabase.LoadAssetAtPath<TextAsset>(licensePath);

            Assert.IsNotNull(license);
            StringAssert.Contains("placeholder generated, no third-party source", license.text);
        }
    }
}
