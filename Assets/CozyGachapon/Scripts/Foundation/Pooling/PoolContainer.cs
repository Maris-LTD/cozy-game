using System.Collections.Generic;
using UnityEngine;

namespace CozyGachapon.Pooling
{
    public sealed class PoolContainer
    {
        private readonly List<Transform> _created = new List<Transform>();

        public int Count => _created.Count;

        public Transform Create(Transform root, string poolId)
        {
            GameObject container = new GameObject(poolId);
            container.transform.SetParent(root, false);
            _created.Add(container.transform);
            return container.transform;
        }

        public void DestroyAll()
        {
            for (int i = _created.Count - 1; i >= 0; i--)
            {
                Transform container = _created[i];
                if (container != null)
                {
                    PoolObjectDisposer.Dispose(container.gameObject);
                }
            }

            _created.Clear();
        }
    }
}
