using System;
using CozyGachapon.Contracts;

namespace CozyGachapon.Foundation
{
    public sealed class BootstrapGraph
    {
        private readonly ICurtainPresenter _presenter;
        private bool _isStarted;

        public InputBarrier Gate { get; private set; }
        public BootStepSequencer Sequencer { get; private set; }
        public StartupCurtainController Curtain { get; private set; }
        public AppLifecycleWatchdog Lifecycle { get; private set; }
        public BootStatusTracker Status { get; private set; }
        public ViewportCalculator Viewport { get; private set; }
        public TabRouter Tabs { get; private set; }
        public WorldPressRouter WorldPress { get; private set; }
        public BackButtonRouter Back { get; private set; }
        public FirstInteractionDetector FirstInteraction { get; private set; }
        public WelcomeQueue Welcome { get; private set; }

        public bool IsVisibleForBoot => Lifecycle.IsPageVisible || Lifecycle.IsAdOpen;

        public BootstrapGraph(
            BootConfig bootConfig,
            SceneFlowConfig sceneFlowConfig,
            ICurtainPresenter presenter,
            IUiHitTester uiHitTester,
            IAppBackgroundRequester backgroundRequester,
            HostPlatform platform,
            ITimeProvider timeProvider)
        {
            if (bootConfig == null)
            {
                throw new ArgumentNullException(nameof(bootConfig));
            }

            if (sceneFlowConfig == null)
            {
                throw new ArgumentNullException(nameof(sceneFlowConfig));
            }

            _presenter = presenter ?? throw new ArgumentNullException(nameof(presenter));

            Gate = new InputBarrier();
            Sequencer = new BootStepSequencer(bootConfig, sceneFlowConfig);
            Curtain = new StartupCurtainController(bootConfig, Gate);
            Lifecycle = new AppLifecycleWatchdog(timeProvider);
            Status = new BootStatusTracker(Lifecycle);
            Viewport = new ViewportCalculator(sceneFlowConfig);
            Tabs = new TabRouter(sceneFlowConfig);
            Tabs.IsBooting = true;
            WorldPress = new WorldPressRouter(Gate, Tabs, uiHitTester, Viewport);
            Back = new BackButtonRouter(Tabs, Status, backgroundRequester, platform);
            FirstInteraction = new FirstInteractionDetector();
            Welcome = new WelcomeQueue(sceneFlowConfig, Gate);

            Sequencer.OnBootCompleted += HandleBootCompleted;
            Sequencer.OnBootFailed += HandleBootFailed;
            Curtain.OnInteractive += HandleInteractive;
        }

        public bool RegisterStep(IBootStep step)
        {
            return Sequencer.RegisterStep(step);
        }

        public void SetScreenSize(int width, int height)
        {
            Viewport.SetScreenSize(width, height);
        }

        public void Tick(float deltaTime)
        {
            Viewport.FlushFrame();

            if (!_isStarted)
            {
                _isStarted = true;
                Sequencer.Start();
            }

            Sequencer.Tick(deltaTime, IsVisibleForBoot);
            Curtain.Tick(deltaTime);
        }

        public void SetPageVisible(bool isVisible, PauseReason reason)
        {
            Lifecycle.OnVisibilityChanged(isVisible, reason);
            ApplyCurtainVisibility();
        }

        public void SetAdOpen(bool isOpen)
        {
            Lifecycle.SetAdOpen(isOpen);
            ApplyCurtainVisibility();
        }

        public void SetWindowFocus(bool hasFocus)
        {
            Lifecycle.OnWindowFocusChanged(hasFocus);
        }

        public void NotifyQuit()
        {
            Lifecycle.OnApplicationQuit();
        }

        private void HandleBootCompleted(BootCompletedEvent evt)
        {
            Curtain.NotifyBootCompleted(evt);
            Lifecycle.NotifyBootCompleted();
            ApplyCurtainVisibility();
        }

        private void HandleBootFailed(BootFailedEvent evt)
        {
            Curtain.NotifyBootFailed(evt);
            Status.NotifyBootFailed(evt);
            _presenter.ShowBootFailure(evt);
        }

        private void HandleInteractive(InteractiveEvent evt)
        {
            Lifecycle.NotifyInteractive();
            Tabs.IsBooting = false;
            Welcome.Begin();
        }

        private void ApplyCurtainVisibility()
        {
            Curtain.SetVisibility(IsVisibleForBoot);
        }
    }
}
