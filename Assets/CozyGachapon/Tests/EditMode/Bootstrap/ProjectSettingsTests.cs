using NUnit.Framework;
using UnityEditor;

namespace CozyGachapon.Tests.Bootstrap
{
    public class ProjectSettingsTests
    {
        [Test]
        public void PlayerSettings_RunInBackground_IsTrue()
        {
            Assert.IsTrue(PlayerSettings.runInBackground);
        }

        [Test]
        public void PlayerSettings_DefaultOrientation_IsPortrait()
        {
            Assert.AreEqual(UIOrientation.Portrait, PlayerSettings.defaultInterfaceOrientation);
        }

        [Test]
        public void PlayerSettings_LandscapeAutorotate_IsDisabled()
        {
            Assert.IsFalse(PlayerSettings.allowedAutorotateToLandscapeLeft);
            Assert.IsFalse(PlayerSettings.allowedAutorotateToLandscapeRight);
            Assert.IsFalse(PlayerSettings.allowedAutorotateToPortraitUpsideDown);
        }
    }
}
