using System.Collections.Generic;
using CozyGachapon.Audio;
using CozyGachapon.Contracts;
using NUnit.Framework;
using UnityEditor;

namespace CozyGachapon.Tests.Audio
{
    public class AudioConfigValidatorTests
    {
        private const double GainTolerance = 0.01d;
        private const double Epsilon = 0.0001d;

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
        public void DefaultCueTableHasZeroIssues()
        {
            List<AudioConfigIssue> issues = new List<AudioConfigIssue>();

            AudioConfigValidator.Validate(_config, _targets, _cues, DefaultAudioAssets.TapCooldownSeconds, issues);

            Assert.AreEqual(0, AudioConfigValidator.CountErrors(issues));
            Assert.AreEqual(0, issues.Count);
        }

        [Test]
        public void StormInstancesEqualTheCapacityAndStayValid()
        {
            int instances = AudioBudgetMath.StormInstances(_config, _cues);
            int capacity = AudioBudgetMath.StormVoiceCapacity(_config);

            Assert.AreEqual(12, instances);
            Assert.AreEqual(12, capacity);
            Assert.LessOrEqual(instances, capacity);
        }

        [Test]
        public void StormPeakGainIsBelowTheCeiling()
        {
            double gain = AudioBudgetMath.StormPeakGain(_config, _cues);

            Assert.AreEqual(1.76d, gain, GainTolerance);
            Assert.LessOrEqual(gain, _targets.StormPeakGainMax);
        }

        [Test]
        public void StealAgeOfBackgroundStealOldestCuesMeetsTheMinimum()
        {
            Assert.AreEqual(0.24d, AudioBudgetMath.StealAge(Find("Plinko.PegHitScored")), Epsilon);
            Assert.AreEqual(0.20d, AudioBudgetMath.StealAge(Find("Plinko.PegHitUnscored")), Epsilon);

            for (int i = 0; i < _cues.Count; i++)
            {
                SoundCue cue = _cues[i];
                if (AudioBudgetMath.IsStormCue(_config, cue) && cue.InstanceCapPolicy == InstanceCapPolicy.StealOldest)
                {
                    Assert.GreaterOrEqual(AudioBudgetMath.StealAge(cue), _targets.MinStealAgeSeconds, cue.CueId);
                }
            }
        }

        [Test]
        public void SpawnCuesAreDropNewAndTheirPlayTimeFitsTheLimit()
        {
            string[] spawnIds = { "Plinko.CoinSpawn", "Plinko.CoinSpawnAuto" };
            for (int i = 0; i < spawnIds.Length; i++)
            {
                SoundCue cue = Find(spawnIds[i]);
                double playSeconds = AudioBudgetMath.SpawnPlaySeconds(cue, out _, out _);

                Assert.AreEqual(InstanceCapPolicy.DropNew, cue.InstanceCapPolicy, spawnIds[i]);
                Assert.LessOrEqual(playSeconds, _targets.ManualSpawnClipMaxSeconds, spawnIds[i]);
            }
        }

        [Test]
        public void ManualSpawnLimitFitsExactlyInsideTheCooldownMargin()
        {
            double limit = (double)_targets.ManualSpawnClipMaxSeconds + _targets.SpawnClipMarginSeconds;

            Assert.AreEqual(0.12f, _targets.ManualSpawnClipMaxSeconds, 0.00001f);
            Assert.LessOrEqual(limit, DefaultAudioAssets.TapCooldownSeconds);
        }

        [Test]
        public void ScaleCuesStayWithinTheSampleShiftForEveryNote()
        {
            int checkedCues = 0;
            for (int i = 0; i < _cues.Count; i++)
            {
                SoundCue cue = _cues[i];
                if (cue.PitchMode != PitchMode.Scale)
                {
                    continue;
                }

                checkedCues++;
                Assert.Greater(cue.PitchedSamples.Count, 0, cue.CueId);
                for (int note = 0; note < _config.ScaleNoteCount; note++)
                {
                    int semitone = NoteIndexCalculator.Semitone(note, _config.ScaleIntervals);
                    Assert.IsTrue(SamplePitchSelector.TrySelectSample(semitone, cue.PitchedSamples, out _, out int shift));
                    Assert.LessOrEqual(System.Math.Abs(shift), _config.MaxSampleShiftSemitones, cue.CueId + " note " + note);
                }
            }

            Assert.AreEqual(2, checkedCues);
        }

        [Test]
        public void NoteIndexMapsToTheScaleSemitones()
        {
            int[] expected = { 0, 2, 4, 7, 9, 12, 14, 16, 19, 21 };
            for (int note = 0; note < expected.Length; note++)
            {
                Assert.AreEqual(expected[note], NoteIndexCalculator.Semitone(note, _config.ScaleIntervals));
            }
        }

        [Test]
        public void SampleSelectionPrefersTheLowerSampleOnATie()
        {
            SoundCue cue = Find("Plinko.PegHitScored");

            Assert.IsTrue(SamplePitchSelector.TrySelectSample(8, cue.PitchedSamples, out int index, out int shift));
            Assert.AreEqual(4, cue.PitchedSamples[index].NativeSemitone);
            Assert.AreEqual(4, shift);
        }

        [Test]
        public void ValidatingTheShippedAssetsDoesNotDirtyThem()
        {
            List<AudioConfigIssue> issues = new List<AudioConfigIssue>();

            AudioConfigValidator.Validate(_config, _targets, _cues, DefaultAudioAssets.TapCooldownSeconds, issues);

            Assert.IsFalse(EditorUtility.IsDirty(_config));
            Assert.IsFalse(EditorUtility.IsDirty(_targets));
            for (int i = 0; i < _cues.Count; i++)
            {
                Assert.IsFalse(EditorUtility.IsDirty(_cues[i]), _cues[i].CueId);
            }
        }
    }
}
