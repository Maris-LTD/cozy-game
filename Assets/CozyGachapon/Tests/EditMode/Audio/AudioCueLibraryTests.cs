using System.Collections.Generic;
using CozyGachapon.Audio;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace CozyGachapon.Tests.Audio
{
    public class AudioCueLibraryTests
    {
        private const string LibraryPath = DefaultAudioAssets.Folder + "/AudioCueLibrary.asset";

        private AudioCueLibrary _created;

        [TearDown]
        public void TearDown()
        {
            if (_created != null)
            {
                Object.DestroyImmediate(_created);
            }
        }

        [Test]
        public void DefaultLibrary_ListsEveryCueInCuesFolder_WithoutNullsOrDuplicates()
        {
            AudioCueLibrary library = AssetDatabase.LoadAssetAtPath<AudioCueLibrary>(LibraryPath);
            List<SoundCue> folderCues = DefaultAudioAssets.LoadCues();

            Assert.IsNotNull(library);
            Assert.AreEqual(folderCues.Count, library.Cues.Count);
            HashSet<string> ids = new HashSet<string>();
            for (int i = 0; i < library.Cues.Count; i++)
            {
                SoundCue cue = library.Cues[i];
                Assert.IsNotNull(cue);
                Assert.IsTrue(folderCues.Contains(cue));
                Assert.IsTrue(ids.Add(cue.CueId));
            }
        }

        [Test]
        public void NewLibrary_HasNoCues()
        {
            _created = ScriptableObject.CreateInstance<AudioCueLibrary>();

            Assert.AreEqual(0, _created.Cues.Count);
        }
    }
}
