using CozyGachapon.Contracts;
using CozyGachapon.Foundation;
using UnityEngine;

namespace CozyGachapon
{
    [DefaultExecutionOrder(-1000)]
    public sealed class BootstrapController : MonoBehaviour
    {

        [SerializeField] private BootConfig _bootConfig;
        [SerializeField] private SceneFlowConfig _sceneFlowConfig;
        [SerializeField] private StartupCurtainView _view;
        [SerializeField] private AppLifecycleAdapter _lifecycleAdapter;
        [SerializeField] private PointerPressInputAdapter _pointerAdapter;
        [SerializeField] private BackKeyInputAdapter _backKeyAdapter;
        [SerializeField] private EventSystemUiHitTester _uiHitTester;
        [SerializeField] private bool _failFirstBootForTest;

        public BootstrapGraph Graph { get; private set; }

        private void Awake()
        {
            if (_view == null)
            {
                enabled = false;
                return;
            }

            if (_bootConfig == null || _sceneFlowConfig == null || !_sceneFlowConfig.HasValidAspectRange)
            {
                _view.ShowFallbackError(BootStepSequencer.MissingConfigStepId);
                enabled = false;
                return;
            }

            IUiHitTester uiHitTester = _uiHitTester != null ? _uiHitTester : null;
            Graph = new BootstrapGraph(
                _bootConfig,
                _sceneFlowConfig,
                _view,
                uiHitTester,
                new AndroidBackgroundRequester(),
                HostPlatformResolver.Resolve(),
                new SystemTimeProvider());

            _view.Bind(Graph.Curtain, Graph.Viewport, _bootConfig.ErrorPanelFadeSeconds);
            InitializeAdapters();
            RegisterDebugSteps();
        }

        private void Update()
        {
            Graph.SetScreenSize(Screen.width, Screen.height);
            Graph.Tick(Time.unscaledDeltaTime);
        }

        private void InitializeAdapters()
        {
            if (_lifecycleAdapter != null)
            {
                _lifecycleAdapter.Initialize(Graph);
            }

            if (_pointerAdapter != null)
            {
                _pointerAdapter.Initialize(Graph.WorldPress, Graph.FirstInteraction);
            }

            if (_backKeyAdapter != null)
            {
                _backKeyAdapter.Initialize(Graph.Back, Graph.FirstInteraction);
            }
        }

        private void RegisterDebugSteps()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (_failFirstBootForTest)
            {
                Graph.RegisterStep(new FailOnceBootStep());
            }
#endif
        }
    }
}
