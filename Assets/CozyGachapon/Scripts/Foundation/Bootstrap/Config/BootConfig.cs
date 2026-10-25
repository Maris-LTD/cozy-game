using UnityEngine;

namespace CozyGachapon.Foundation
{
    [CreateAssetMenu(fileName = "BootConfig", menuName = "CozyGachapon/Bootstrap/BootConfig")]
    public class BootConfig : ScriptableObject
    {
        [SerializeField] private float _stepTimeoutSeconds = 10f;
        [SerializeField] private float _errorPanelFadeSeconds = 0.2f;
        [SerializeField] private float _curtainMinDisplaySeconds = 0.4f;
        [SerializeField] private float _curtainRevealDuration = 0.35f;
        [SerializeField] private float _maxBootToInteractiveSeconds = 2.0f;
        [SerializeField] private int _targetFps = 60;

        public float StepTimeoutSeconds => _stepTimeoutSeconds;
        public float ErrorPanelFadeSeconds => _errorPanelFadeSeconds;
        public float CurtainMinDisplaySeconds => _curtainMinDisplaySeconds;
        public float CurtainRevealDuration => _curtainRevealDuration;
        public float MaxBootToInteractiveSeconds => _maxBootToInteractiveSeconds;
        public int TargetFps => _targetFps;

        public void SetStepTimeoutForTest(float timeoutSeconds)
        {
            _stepTimeoutSeconds = timeoutSeconds;
        }

        public void SetCurtainTimingForTest(float minDisplay, float revealDuration, float maxBudget)
        {
            _curtainMinDisplaySeconds = minDisplay;
            _curtainRevealDuration = revealDuration;
            _maxBootToInteractiveSeconds = maxBudget;
        }
    }
}
