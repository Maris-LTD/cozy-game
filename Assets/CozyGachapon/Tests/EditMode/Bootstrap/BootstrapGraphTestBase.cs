using CozyGachapon.Contracts;
using CozyGachapon.Foundation;
using NUnit.Framework;
using UnityEngine;

namespace CozyGachapon.Tests.Bootstrap
{
    public abstract class BootstrapGraphTestBase
    {
        protected const float Dt = 1f / 60f;
        protected const int ScreenWidth = 1080;
        protected const int ScreenHeight = 1920;

        protected BootConfig _bootConfig;
        protected SceneFlowConfig _sceneFlowConfig;
        protected GraphTestClock _clock;
        protected RecordingCurtainPresenter _presenter;
        protected RecordingPlatformStub _platform;
        protected int _tick;

        [SetUp]
        public void SetUpGraphFixture()
        {
            _bootConfig = ScriptableObject.CreateInstance<BootConfig>();
            _sceneFlowConfig = ScriptableObject.CreateInstance<SceneFlowConfig>();
            _clock = new GraphTestClock();
            _presenter = new RecordingCurtainPresenter();
            _platform = new RecordingPlatformStub();
            _tick = 0;
        }

        [TearDown]
        public void TearDownGraphFixture()
        {
            Object.DestroyImmediate(_bootConfig);
            Object.DestroyImmediate(_sceneFlowConfig);
        }

        protected BootstrapGraph CreateGraph(GraphEventRecorder recorder)
        {
            BootstrapGraph graph = new BootstrapGraph(
                _bootConfig,
                _sceneFlowConfig,
                _presenter,
                _platform,
                _platform,
                HostPlatform.Android,
                _clock);
            recorder?.Subscribe(graph);
            graph.SetScreenSize(ScreenWidth, ScreenHeight);
            return graph;
        }

        protected void TickOnce(BootstrapGraph graph, GraphEventRecorder recorder)
        {
            _tick++;
            if (recorder != null)
            {
                recorder.CurrentTick = _tick;
            }

            _clock.Advance(Dt);
            graph.Tick(Dt);
        }

        protected void TickFor(BootstrapGraph graph, GraphEventRecorder recorder, float seconds)
        {
            int ticks = Mathf.CeilToInt(seconds / Dt);
            for (int i = 0; i < ticks; i++)
            {
                TickOnce(graph, recorder);
            }
        }
    }
}
