using System;
using System.Collections.Generic;
using CozyGachapon.Audio;
using NUnit.Framework;
using UnityEngine;

namespace CozyGachapon.Tests.Audio
{
    public abstract class AudioMisconfigFixture : AudioTestFixture
    {
        private const int SampleRate = 44100;

        protected AudioConfig DefaultConfig { get; private set; }
        protected AudioBalanceTargets Targets { get; private set; }
        protected List<SoundCue> Cues { get; private set; }

        [SetUp]
        public void SetUpMisconfig()
        {
            DefaultConfig = Track(UnityEngine.Object.Instantiate(DefaultAudioAssets.LoadConfig()));
            Targets = Track(UnityEngine.Object.Instantiate(DefaultAudioAssets.LoadTargets()));
            Cues = new List<SoundCue>();
            List<SoundCue> originals = DefaultAudioAssets.LoadCues();
            for (int i = 0; i < originals.Count; i++)
            {
                Cues.Add(Track(UnityEngine.Object.Instantiate(originals[i])));
            }
        }

        protected SoundCue Cue(string cueId)
        {
            for (int i = 0; i < Cues.Count; i++)
            {
                if (Cues[i].CueId == cueId)
                {
                    return Cues[i];
                }
            }

            throw new InvalidOperationException(cueId);
        }

        protected AudioClip CreateClipOfLength(string clipName, float seconds)
        {
            int samples = Mathf.RoundToInt(seconds * SampleRate);
            return Track(AudioClip.Create(clipName, samples, 1, SampleRate, false));
        }

        protected List<AudioConfigIssue> RunValidator()
        {
            List<AudioConfigIssue> issues = new List<AudioConfigIssue>();
            AudioConfigValidator.Validate(DefaultConfig, Targets, Cues, DefaultAudioAssets.TapCooldownSeconds, issues);
            return issues;
        }

        protected AudioConfigIssue AssertSingleIssue(AudioConfigIssueCode code)
        {
            List<AudioConfigIssue> issues = RunValidator();
            Assert.AreEqual(1, issues.Count, DescribeIssues(issues));
            Assert.AreEqual(code, issues[0].Code);
            return issues[0];
        }

        private static string DescribeIssues(List<AudioConfigIssue> issues)
        {
            System.Text.StringBuilder builder = new System.Text.StringBuilder();
            for (int i = 0; i < issues.Count; i++)
            {
                builder.Append(issues[i].Code).Append(':').Append(issues[i].CueId).Append(' ');
            }

            return builder.ToString();
        }
    }
}
