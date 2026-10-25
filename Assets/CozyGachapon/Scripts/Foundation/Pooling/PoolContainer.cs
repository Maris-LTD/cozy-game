using UnityEngine;

namespace CozyGachapon.Pooling
{
    public sealed class PoolContainer
    {
        public Transform Create(Transform root, string poolId)
        {
            GameObject container = new GameObject(poolId);
            container.transform.SetParent(root, false);
            return container.transform;
        }
    }
}
