using System;
using CozyGachapon.Audio;
using CozyGachapon.Bootstrap;
using CozyGachapon.Contracts;
using CozyGachapon.Contracts.Inventory;
using CozyGachapon.Core.Inventory;
using CozyGachapon.Core.PetCatalog;
using CozyGachapon.Economy;
using CozyGachapon.Foundation;
using CozyGachapon.Pooling;
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
        [SerializeField] private PoolingConfig _poolingConfig;
        [SerializeField] private Transform _poolContainer;
        [SerializeField] private AudioConfig _audioConfig;
        [SerializeField] private AudioCueLibrary _audioCueLibrary;
        [SerializeField] private Transform _audioVoiceContainer;
        [SerializeField] private EconomyConfig _economyConfig;
        [SerializeField] private PetCatalogConfig _petCatalogConfig;
        [SerializeField] private InventoryConfig _inventoryConfig;
        [SerializeField] private bool _failFirstBootForTest;

        private IServiceRegistry _services;
        private PoolingBootModule _pooling;
        private AudioWiring _audio;
        private EconomyWiring _economy;
        private PetWiring _petWiring;
        private InventoryWiring _inventoryWiring;

        public BootstrapGraph Graph { get; private set; }
        public IServiceRegistry Services => _services;
        public IAudioService Audio { get; private set; }
        public UnityVoiceBackend AudioVoices { get; private set; }
        public EconomyCore Economy { get; private set; }
        public IPetDatabase PetDatabase => _services != null && _services.TryGet<IPetDatabase>(out var db) ? db : null;
        public IInventoryService Inventory => _services != null && _services.TryGet<IInventoryService>(out var inv) ? inv : null;

        private void Awake()
        {
            if (_view == null)
            {
                enabled = false;
                return;
            }

            if (_bootConfig == null || _sceneFlowConfig == null || !_sceneFlowConfig.HasValidAspectRange || _poolingConfig == null || _poolContainer == null)
            {
                _view.ShowFallbackError(BootStepSequencer.MissingConfigStepId);
                enabled = false;
                return;
            }

            _services = new ServiceRegistry();
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
            RegisterPoolingSteps();
            RegisterAudioSteps();
            RegisterPetSteps();
            RegisterEconomySteps();
            RegisterInventorySteps();
            RegisterDebugSteps();
        }

        private void Update()
        {
            Graph.SetScreenSize(Screen.width, Screen.height);
            _pooling.Tick();
            Graph.Tick(Time.unscaledDeltaTime);
            _economy?.Tick(Time.unscaledDeltaTime);
            _audio.Tick();
        }

        private void OnDestroy()
        {
            _economy?.Dispose();
            _audio?.Dispose();
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

        private void RegisterPoolingSteps()
        {
            _pooling = new PoolingBootModule(_poolingConfig, _poolContainer, new PoolDiagnosticsRecorder(), _bootConfig.TargetFps);
            _pooling.RegisterSteps(Graph.RegisterStep);
        }

        private void RegisterAudioSteps()
        {
            AudioPlatformPolicy policy = new AudioPlatformPolicy(Application.platform == RuntimePlatform.WebGLPlayer);
            _audio = new AudioWiring(
                _audioConfig,
                _audioCueLibrary,
                _audioVoiceContainer,
                new PlayerPrefsAudioSettingsStore(),
                new SystemAudioRandom(Environment.TickCount),
                policy);
            _audio.Register(Graph);
            Audio = _audio.Service;
            AudioVoices = _audio.Voices;
        }

        private void RegisterPetSteps()
        {
            _petWiring = new PetWiring(_petCatalogConfig, _services);
            _petWiring.Register(Graph);
        }

        private void RegisterEconomySteps()
        {
            _economy = new EconomyWiring(_economyConfig, _services);
            _economy.Register(Graph);
            Economy = _economy.Core;
        }

        private void RegisterInventorySteps()
        {
            _inventoryWiring = new InventoryWiring(_inventoryConfig, _services);
            _inventoryWiring.Register(Graph);
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
