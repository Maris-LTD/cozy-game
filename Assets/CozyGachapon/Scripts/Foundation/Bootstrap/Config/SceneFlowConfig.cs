using UnityEngine;

namespace CozyGachapon.Foundation
{
    [CreateAssetMenu(fileName = "SceneFlowConfig", menuName = "CozyGachapon/Bootstrap/SceneFlowConfig")]
    public class SceneFlowConfig : ScriptableObject
    {
        [SerializeField] private string _bootSceneName = "Boot";
        [SerializeField] private string _gameSceneName = "Game";
        [SerializeField] private float _referenceWidth = 1080f;
        [SerializeField] private float _referenceHeight = 1920f;

        public string BootSceneName => _bootSceneName;
        public string GameSceneName => _gameSceneName;
        public float ReferenceWidth => _referenceWidth;
        public float ReferenceHeight => _referenceHeight;
    }
}
