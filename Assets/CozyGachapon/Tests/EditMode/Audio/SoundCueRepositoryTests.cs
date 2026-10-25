using System.Collections.Generic;
using CozyGachapon.Audio;
using CozyGachapon.Tests.Pooling;
using NUnit.Framework;
using UnityEngine;

namespace CozyGachapon.Tests.Audio
{
    public class SoundCueRepositoryTests : AudioTestFixture
    {
        [Test]
        public void ValidCueResolvesToItsIndexAndAsset()
        {
            SoundCue first = CreateCue("Ui.Tap");
            SoundCue second = CreateCue("Ui.Confirm");
            SoundCueRepository repository = new SoundCueRepository(new List<SoundCue> { first, second });

            Assert.IsTrue(repository.TryGetIndex("Ui.Confirm", out int index));
            Assert.AreSame(second, repository.GetCue(index));
            Assert.IsTrue(repository.TryGetIndex("Ui.Tap", out index));
            Assert.AreSame(first, repository.GetCue(index));
            Assert.AreEqual(2, repository.Count);
            Assert.AreEqual(0, repository.UnusableCount);
        }

        [Test]
        public void UnknownAndNullCueIdsAreNotFound()
        {
            SoundCueRepository repository = new SoundCueRepository(new List<SoundCue> { CreateCue("Ui.Tap") });

            Assert.IsFalse(repository.TryGetIndex("Ui.Missing", out _));
            Assert.IsFalse(repository.TryGetIndex(null, out _));
            Assert.IsFalse(repository.TryGetIndex(string.Empty, out _));
        }

        [Test]
        public void DuplicateCueIdsDiscardEveryCopy()
        {
            SoundCueRepository repository = new SoundCueRepository(new List<SoundCue>
            {
                CreateCue("Ui.Tap"),
                CreateCue("Ui.Dup"),
                CreateCue("Ui.Dup")
            });

            Assert.IsFalse(repository.TryGetIndex("Ui.Dup", out _));
            Assert.IsTrue(repository.TryGetIndex("Ui.Tap", out _));
            Assert.AreEqual(1, repository.Count);
            Assert.AreEqual(2, repository.UnusableCount);
        }

        [Test]
        public void BrokenCuesAreMarkedUnusable()
        {
            SoundCueRepository repository = new SoundCueRepository(new List<SoundCue>
            {
                null,
                CreateCue(string.Empty),
                CreateCue("   "),
                CreateCue("Ui.NoClip", false),
                CreateScaleCue("Plinko.NoSamples", false),
                CreateScaleCue("Plinko.Scale", true),
                CreateCue("Ui.Tap")
            });

            Assert.AreEqual(2, repository.Count);
            Assert.AreEqual(5, repository.UnusableCount);
            Assert.IsFalse(repository.TryGetIndex("Ui.NoClip", out _));
            Assert.IsFalse(repository.TryGetIndex("Plinko.NoSamples", out _));
            Assert.IsTrue(repository.TryGetIndex("Plinko.Scale", out _));
            Assert.IsTrue(repository.TryGetIndex("Ui.Tap", out _));
        }

        [Test]
        public void CueWithOnlyNullClipsIsUnusable()
        {
            SoundCue cue = CreateCue("Ui.NullClip", false);
            cue.SetClipsForTest(new AudioClip[] { null });
            SoundCueRepository repository = new SoundCueRepository(new List<SoundCue> { cue });

            Assert.AreEqual(0, repository.Count);
            Assert.AreEqual(1, repository.UnusableCount);
        }

        [Test]
        public void NullListBuildsEmptyRepository()
        {
            SoundCueRepository repository = new SoundCueRepository(null);

            Assert.AreEqual(0, repository.Count);
            Assert.AreEqual(0, repository.UnusableCount);
            Assert.IsFalse(repository.TryGetIndex("Ui.Tap", out _));
        }

        [Test]
        public void LookupDoesNotAllocate()
        {
            SoundCueRepository repository = new SoundCueRepository(new List<SoundCue> { CreateCue("Ui.Tap") });
            string known = "Ui.Tap";
            string unknown = "Ui.Missing";

            int allocations = GcAllocProbe.CountAllocations(() =>
            {
                for (int i = 0; i < 1000; i++)
                {
                    repository.TryGetIndex(known, out _);
                    repository.TryGetIndex(unknown, out _);
                }
            });

            Assert.AreEqual(0, allocations);
        }
    }
}
