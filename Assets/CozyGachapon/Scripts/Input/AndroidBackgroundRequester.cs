using CozyGachapon.Contracts;
#if UNITY_ANDROID && !UNITY_EDITOR
using UnityEngine;
#endif

namespace CozyGachapon
{
    public sealed class AndroidBackgroundRequester : IAppBackgroundRequester
    {
        public void RequestMoveToBackground()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            using (AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using (AndroidJavaObject activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
            {
                activity.Call<bool>("moveTaskToBack", true);
            }
#endif
        }
    }
}
