using System;
using System.Collections.Generic;
using CozyGachapon.Contracts;
using CozyGachapon.Foundation;

namespace CozyGachapon.Tests.Bootstrap
{
    public sealed class GraphTestClock : ITimeProvider
    {
        public DateTimeOffset UtcNow { get; private set; } = new DateTimeOffset(2026, 10, 25, 9, 0, 0, TimeSpan.Zero);

        public void Advance(double seconds)
        {
            UtcNow = UtcNow.AddSeconds(seconds);
        }
    }

    public sealed class RecordingCurtainPresenter : ICurtainPresenter
    {
        public int CallCount { get; private set; }
        public BootFailedEvent LastFailure { get; private set; }

        public void ShowBootFailure(BootFailedEvent failure)
        {
            CallCount++;
            LastFailure = failure;
        }
    }

    public sealed class RecordingPlatformStub : IAppBackgroundRequester, IUiHitTester
    {
        public List<string> Calls { get; } = new List<string>();
        public int BackgroundRequestCount { get; private set; }

        public int UnexpectedCallCount
        {
            get
            {
                int count = 0;
                for (int i = 0; i < Calls.Count; i++)
                {
                    if (!string.Equals(Calls[i], nameof(RequestMoveToBackground), StringComparison.Ordinal)
                        && !string.Equals(Calls[i], nameof(HitsUi), StringComparison.Ordinal))
                    {
                        count++;
                    }
                }

                return count;
            }
        }

        public void RequestMoveToBackground()
        {
            BackgroundRequestCount++;
            Calls.Add(nameof(RequestMoveToBackground));
        }

        public bool HitsUi(float screenX, float screenY, int pointerId)
        {
            Calls.Add(nameof(HitsUi));
            return false;
        }
    }

    public sealed class FakeBootStep : IBootStep
    {
        private readonly bool _failsOnRun;

        public string Id { get; }
        public BootPhase Phase { get; }
        public IReadOnlyList<string> RunsAfter => Array.Empty<string>();
        public bool IsCritical { get; }
        public bool IsDone { get; private set; }
        public bool IsFailed { get; private set; }
        public int RunCount { get; private set; }

        public FakeBootStep(string id, BootPhase phase, bool isCritical, bool failsOnRun)
        {
            Id = id;
            Phase = phase;
            IsCritical = isCritical;
            _failsOnRun = failsOnRun;
        }

        public void Run()
        {
            RunCount++;
            IsFailed = _failsOnRun;
            IsDone = !_failsOnRun;
        }
    }

    public sealed class GraphEventRecorder
    {
        private readonly Dictionary<string, int> _counts = new Dictionary<string, int>(StringComparer.Ordinal);
        private readonly Dictionary<string, int> _firstTick = new Dictionary<string, int>(StringComparer.Ordinal);

        public int CurrentTick { get; set; }
        public IEnumerable<string> ObservedNames => _counts.Keys;

        public void Subscribe(BootstrapGraph graph)
        {
            graph.Sequencer.OnBootCompleted += _ => Record("BootCompleted");
            graph.Sequencer.OnBootFailed += _ => Record("BootFailed");
            graph.Curtain.OnInteractive += _ => Record("Interactive");
            graph.Lifecycle.OnPaused += _ => Record("Paused");
            graph.Lifecycle.OnResumed += _ => Record("Resumed");
            graph.Lifecycle.OnQuitting += _ => Record("Quitting");
            graph.Lifecycle.OnAdClosed += _ => Record("AdClosed");
            graph.Tabs.OnTabChanged += _ => Record("TabChanged");
            graph.Viewport.OnFrameChanged += _ => Record("FrameChanged");
            graph.FirstInteraction.OnFirstInteraction += _ => Record("FirstInteraction");
            graph.Welcome.OnCompleted += _ => Record("WelcomeQueueCompleted");
        }

        public int Count(string name)
        {
            return _counts.TryGetValue(name, out int count) ? count : 0;
        }

        public int FirstTick(string name)
        {
            return _firstTick.TryGetValue(name, out int tick) ? tick : -1;
        }

        private void Record(string name)
        {
            _counts.TryGetValue(name, out int count);
            _counts[name] = count + 1;
            if (!_firstTick.ContainsKey(name))
            {
                _firstTick[name] = CurrentTick;
            }
        }
    }
}
