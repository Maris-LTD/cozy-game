using System.Collections.Generic;
using CozyGachapon.Audio;
using UnityEditor;

namespace CozyGachapon.Tests.Audio
{
    public static class DefaultAudioAssets
    {
        public const string Folder = "Assets/CozyGachapon/Data/Audio";
        public const string CuesFolder = Folder + "/Cues";
        public const string ClipsFolder = Folder + "/Clips";
        public const float TapCooldownSeconds = 0.15f;

        public static AudioConfig LoadConfig()
        {
            return AssetDatabase.LoadAssetAtPath<AudioConfig>(Folder + "/AudioConfig.asset");
        }

        public static AudioBalanceTargets LoadTargets()
        {
            return AssetDatabase.LoadAssetAtPath<AudioBalanceTargets>(Folder + "/AudioBalanceTargets.asset");
        }

        public static List<SoundCue> LoadCues()
        {
            List<SoundCue> cues = new List<SoundCue>();
            string[] guids = AssetDatabase.FindAssets("t:SoundCue", new[] { CuesFolder });
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                cues.Add(AssetDatabase.LoadAssetAtPath<SoundCue>(path));
            }

            return cues;
        }
    }
}
