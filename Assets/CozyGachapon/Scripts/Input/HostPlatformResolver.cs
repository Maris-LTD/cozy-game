using CozyGachapon.Contracts;
using UnityEngine;

namespace CozyGachapon
{
    public static class HostPlatformResolver
    {
        public static HostPlatform Resolve()
        {
            switch (Application.platform)
            {
                case RuntimePlatform.Android:
                    return HostPlatform.Android;
                case RuntimePlatform.WebGLPlayer:
                    return HostPlatform.WebGL;
                default:
                    return HostPlatform.Other;
            }
        }
    }
}
