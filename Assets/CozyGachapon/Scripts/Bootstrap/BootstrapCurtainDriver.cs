using CozyGachapon.Contracts;
using CozyGachapon.Foundation;
using UnityEngine;

namespace CozyGachapon
{
    public sealed class BootstrapCurtainDriver : MonoBehaviour
    {
        private const string MissingConfigStepId = "boot.config";
        private const string PreviewFailedStepId = "pet.catalog";

        [SerializeField] private BootConfig _bootConfig;
        [SerializeField] private SceneFlowConfig _sceneFlowConfig;
        [SerializeField] private StartupCurtainView _view;
        [SerializeField] private CurtainPreviewMode _previewMode;

        private StartupCurtainController _controller;
        private ViewportCalculator _viewport;
        private bool _previewApplied;

        private void Awake()
        {
            if (_view == null)
            {
                enabled = false;
                return;
            }

            if (_bootConfig == null || _sceneFlowConfig == null || !_sceneFlowConfig.HasValidAspectRange)
            {
                _view.ShowFallbackError(MissingConfigStepId);
                enabled = false;
                return;
            }

            _controller = new StartupCurtainController(_bootConfig, new InputBlockGate());
            _viewport = new ViewportCalculator(_sceneFlowConfig);
            _view.Bind(_controller, _viewport, _bootConfig.ErrorPanelFadeSeconds);
        }

        private void Update()
        {
            _viewport.SetScreenSize(Screen.width, Screen.height);
            _viewport.FlushFrame();

            if (!_previewApplied && _previewMode == CurtainPreviewMode.FailAtPetCatalog)
            {
                _previewApplied = true;
                BootFailedEvent failure = new BootFailedEvent(PreviewFailedStepId, BootPhase.DataLoad, "preview");
                _controller.NotifyBootFailed(failure);
                _view.ShowBootFailure(failure.StepId);
            }

            _controller.Tick(Time.unscaledDeltaTime);
            if (_controller.State == CurtainState.Open)
            {
                enabled = false;
            }
        }
    }
}
