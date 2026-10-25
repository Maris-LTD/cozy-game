using System;
using System.Collections.Generic;
using CozyGachapon.Contracts;
using CozyGachapon.Foundation;
using NUnit.Framework;
using UnityEngine;

namespace CozyGachapon.Tests.Bootstrap
{
    public abstract class WelcomeQueueTestBase
    {
        protected SceneFlowConfig _config;
        protected InputBlockGate _gate;
        protected WelcomeQueue _queue;
        protected List<string> _showLog;
        protected int _completedCount;
        protected WelcomeQueueCompletedEvent _lastEvent;

        [SetUp]
        public void SetUp()
        {
            _config = ScriptableObject.CreateInstance<SceneFlowConfig>();
            _gate = new InputBlockGate();
            _queue = new WelcomeQueue(_config, _gate);
            _showLog = new List<string>();
            _completedCount = 0;
            _queue.OnCompleted += OnCompleted;
        }

        [TearDown]
        public void TearDown()
        {
            UnityEngine.Object.DestroyImmediate(_config);
        }

        private void OnCompleted(WelcomeQueueCompletedEvent completed)
        {
            _completedCount++;
            _lastEvent = completed;
        }

        protected FakeWelcomeStep AddStep(string id, bool shouldShow = true)
        {
            FakeWelcomeStep step = new FakeWelcomeStep(id, _showLog) { ShouldShowValue = shouldShow };
            Assert.IsTrue(_queue.Register(step));
            return step;
        }

        protected void SetOrder(params string[] order)
        {
            _config.SetWelcomeOrderForTest(order);
        }
    }
}
