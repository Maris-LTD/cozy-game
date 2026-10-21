using UnityEngine;

namespace CozyGachapon.Foundation
{
    [CreateAssetMenu(fileName = "BootConfig", menuName = "CozyGachapon/Bootstrap/BootConfig")]
    public class BootConfig : ScriptableObject
    {
        [SerializeField] private float _stepTimeoutSeconds = 10f;
        [SerializeField] private float _errorPanelFadeSeconds = 0.3f;
        [SerializeField] private float _curtainMinDisplaySeconds = 0.5f;
        [SerializeField] private float _curtainRevealDuration = 0.6f;

        public float StepTimeoutSeconds => _stepTimeoutSeconds;
        public float ErrorPanelFadeSeconds => _errorPanelFadeSeconds;
        public float CurtainMinDisplaySeconds => _curtainMinDisplaySeconds;
        public float CurtainRevealDuration => _curtainRevealDuration;

        public void SetStepTimeoutForTest(float timeoutSeconds)
        {
            _stepTimeoutSeconds = timeoutSeconds;
        }
    }
}
