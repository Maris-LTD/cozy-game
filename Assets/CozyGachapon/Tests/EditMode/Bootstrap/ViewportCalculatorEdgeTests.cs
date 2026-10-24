using CozyGachapon.Contracts;
using CozyGachapon.Foundation;
using NUnit.Framework;
using UnityEngine;

namespace CozyGachapon.Tests.Bootstrap
{
    public class ViewportCalculatorEdgeTests
    {
        private const float Tight = 0.01f;

        private SceneFlowConfig _config;
        private ViewportCalculator _calculator;
        private int _eventCount;
        private FrameChangedEvent _lastEvent;

        [SetUp]
        public void SetUp()
        {
            _config = ScriptableObject.CreateInstance<SceneFlowConfig>();
            _calculator = new ViewportCalculator(_config);
            _eventCount = 0;
            _calculator.OnFrameChanged += OnFrameChanged;
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_config);
        }

        private void OnFrameChanged(FrameChangedEvent frame)
        {
            _eventCount++;
            _lastEvent = frame;
        }

        private void FlushSize(int width, int height)
        {
            _calculator.SetScreenSize(width, height);
            _calculator.FlushFrame();
        }

        [Test]
        public void ZeroSizeThenValidSize_RaisesOneEventWithValidFrame()
        {
            FlushSize(0, 0);
            FlushSize(1080, 2400);

            Assert.AreEqual(1, _eventCount);
            Assert.IsTrue(_calculator.HasFrame);
            Assert.AreEqual(2400f, _lastEvent.FrameHeight, Tight);
        }

        [Test]
        public void ResizeToDifferentSizeNextFrame_RaisesSecondEventWithNewFrame()
        {
            FlushSize(1080, 2400);
            FlushSize(1920, 1080);

            Assert.AreEqual(2, _eventCount);
            Assert.AreEqual(607.5f, _calculator.CurrentFrame.FrameWidth, Tight);
            Assert.AreEqual(656.25f, _calculator.CurrentFrame.FrameX, Tight);
        }

        [Test]
        public void ConfigInvalidatedAfterConstruction_KeepsFrameAndRaisesNoEvent()
        {
            FlushSize(1080, 2400);
            _config.SetAspectRangeForTest(0.5f, 0.4f);

            FlushSize(1920, 1080);

            Assert.AreEqual(1, _eventCount);
            Assert.AreEqual(1080f, _calculator.CurrentFrame.FrameWidth, Tight);
            Assert.IsFalse(float.IsNaN(_calculator.CurrentFrame.ExtraHeightRef));
        }

        [Test]
        public void Contains_Phone1080x2700_LetterboxTopAndBottomAreOutside()
        {
            FlushSize(1080, 2700);

            Assert.IsTrue(_calculator.Contains(540f, 1350f));
            Assert.IsFalse(_calculator.Contains(540f, 40f));
            Assert.IsFalse(_calculator.Contains(540f, 2660f));
        }
    }
}
