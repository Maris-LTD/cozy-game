using CozyGachapon.Contracts;
using UnityEngine;

namespace CozyGachapon.Foundation
{
    [CreateAssetMenu(fileName = "SceneFlowConfig", menuName = "CozyGachapon/Bootstrap/SceneFlowConfig")]
    public class SceneFlowConfig : ScriptableObject
    {
        [SerializeField] private string _bootSceneName = "Boot";
        [SerializeField] private string _gameSceneName = "MainGame";
        [SerializeField] private float _referenceWidth = 1080f;
        [SerializeField] private float _referenceHeight = 1920f;
        [SerializeField] private AppTab _defaultTab = AppTab.Board;
        [SerializeField] private float _minAspect = 0.4286f;
        [SerializeField] private float _maxAspect = 0.5625f;

        public string BootSceneName => _bootSceneName;
        public string GameSceneName => _gameSceneName;
        public float ReferenceWidth => _referenceWidth;
        public float ReferenceHeight => _referenceHeight;
        public AppTab DefaultTab => _defaultTab;
        public float MinAspect => _minAspect;
        public float MaxAspect => _maxAspect;

        public bool HasValidAspectRange =>
            _referenceWidth > 0f
            && _referenceHeight > 0f
            && _minAspect > 0f
            && _minAspect < _maxAspect
            && _maxAspect <= _referenceWidth / _referenceHeight;

        public void SetAspectRangeForTest(float minAspect, float maxAspect)
        {
            _minAspect = minAspect;
            _maxAspect = maxAspect;
        }
    }
}
