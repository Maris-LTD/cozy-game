using System;
using CozyGachapon.Contracts;
using UnityEngine;

namespace CozyGachapon.Pooling
{
    [Serializable]
    public class PoolDefinition
    {
        [SerializeField] private string _id;
        [SerializeField] private GameObject _prefab;
        [SerializeField] private PoolExhaustionPolicy _policy;
        [SerializeField] private int _headroom;

        public string Id => _id;
        public GameObject Prefab => _prefab;
        public PoolExhaustionPolicy Policy => _policy;
        public int Headroom => _headroom;

        public PoolDefinition(string id, GameObject prefab, PoolExhaustionPolicy policy, int headroom)
        {
            _id = id;
            _prefab = prefab;
            _policy = policy;
            _headroom = headroom;
        }
    }
}
