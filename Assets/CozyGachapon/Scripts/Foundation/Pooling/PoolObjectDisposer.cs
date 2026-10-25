using UnityEngine;

namespace CozyGachapon.Pooling
{
    public static class PoolObjectDisposer
    {
        public static void Dispose(Object target)
        {
            if (target == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Object.Destroy(target);
                return;
            }

            Object.DestroyImmediate(target);
        }
    }
}
