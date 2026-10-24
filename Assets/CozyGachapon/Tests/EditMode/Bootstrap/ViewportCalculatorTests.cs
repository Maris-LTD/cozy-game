using System;
using CozyGachapon.Contracts;
using CozyGachapon.Foundation;
using NUnit.Framework;
using UnityEngine;

namespace CozyGachapon.Tests.Bootstrap
{
    public class ViewportCalculatorTests
    {
        private const float Tight = 0.01f;
        private const float Loose = 0.5f;

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
            UnityEngine.Object.DestroyImmediate(_config);
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
        public void Desktop1920x1080_PillarboxedFrame607x1080AtX656()
        {
            FlushSize(1920, 1080);

            FrameChangedEvent frame = _calculator.CurrentFrame;
            Assert.AreEqual(607.5f, frame.FrameWidth, Tight);
            Assert.AreEqual(1080f, frame.FrameHeight, Tight);
            Assert.AreEqual(656.25f, frame.FrameX, Tight);
            Assert.AreEqual(0f, frame.FrameY, Tight);
            Assert.AreEqual(0.5625f, frame.FrameScale, Tight);
            Assert.AreEqual(0f, frame.ExtraHeightRef, Tight);
            Assert.AreEqual(1, _eventCount);
        }

        [Test]
        public void Phone1080x2400_FullScreenFrameWithExtraHeight480()
        {
            FlushSize(1080, 2400);

            FrameChangedEvent frame = _calculator.CurrentFrame;
            Assert.AreEqual(0f, frame.FrameX, Tight);
            Assert.AreEqual(0f, frame.FrameY, Tight);
            Assert.AreEqual(1080f, frame.FrameWidth, Tight);
            Assert.AreEqual(2400f, frame.FrameHeight, Tight);
            Assert.AreEqual(1f, frame.FrameScale, Tight);
            Assert.AreEqual(480f, frame.ExtraHeightRef, Tight);
        }

        [Test]
        public void Phone1080x2700_LetterboxedAt9by21()
        {
            FlushSize(1080, 2700);

            FrameChangedEvent frame = _calculator.CurrentFrame;
            Assert.AreEqual(1080f, frame.FrameWidth, Loose);
            Assert.AreEqual(2519.8f, frame.FrameHeight, Loose);
            Assert.AreEqual(90f, frame.FrameY, Loose);
            Assert.AreEqual(1f, frame.FrameScale, Tight);
            Assert.AreEqual(600f, frame.ExtraHeightRef, Loose);
        }

        [Test]
        public void Tablet1536x2048_PillarboxedFrame1152AtX192()
        {
            FlushSize(1536, 2048);

            FrameChangedEvent frame = _calculator.CurrentFrame;
            Assert.AreEqual(1152f, frame.FrameWidth, Tight);
            Assert.AreEqual(2048f, frame.FrameHeight, Tight);
            Assert.AreEqual(192f, frame.FrameX, Tight);
            Assert.AreEqual(1.0667f, frame.FrameScale, Tight);
            Assert.AreEqual(0f, frame.ExtraHeightRef, Tight);
        }

        [Test]
        public void Reference1080x1920_FrameMatchesScreenScaleOneExtraZero()
        {
            FlushSize(1080, 1920);

            FrameChangedEvent frame = _calculator.CurrentFrame;
            Assert.AreEqual(0f, frame.FrameX, Tight);
            Assert.AreEqual(0f, frame.FrameY, Tight);
            Assert.AreEqual(1080f, frame.FrameWidth, Tight);
            Assert.AreEqual(1920f, frame.FrameHeight, Tight);
            Assert.AreEqual(1f, frame.FrameScale, Tight);
            Assert.AreEqual(0f, frame.ExtraHeightRef, Tight);
        }

        [Test]
        public void ZeroSizeAfterValidFrame_KeepsFrameAndDoesNotRaiseEvent()
        {
            FlushSize(1080, 1920);
            FrameChangedEvent before = _calculator.CurrentFrame;

            FlushSize(0, 0);
            FlushSize(0, 1920);

            Assert.AreEqual(1, _eventCount);
            Assert.AreEqual(before.FrameWidth, _calculator.CurrentFrame.FrameWidth, Tight);
            Assert.AreEqual(before.FrameHeight, _calculator.CurrentFrame.FrameHeight, Tight);
            Assert.AreEqual(before.FrameScale, _calculator.CurrentFrame.FrameScale, Tight);
        }

        [Test]
        public void MultipleResizesInOneFrame_RaisesExactlyOneEventWithLastSize()
        {
            _calculator.SetScreenSize(1920, 1080);
            _calculator.SetScreenSize(1080, 2700);
            _calculator.SetScreenSize(1080, 2400);
            _calculator.FlushFrame();

            Assert.AreEqual(1, _eventCount);
            Assert.AreEqual(1080f, _lastEvent.FrameWidth, Tight);
            Assert.AreEqual(2400f, _lastEvent.FrameHeight, Tight);
            Assert.AreEqual(480f, _lastEvent.ExtraHeightRef, Tight);
        }

        [Test]
        public void SameSizeNextFrame_DoesNotRaiseEvent()
        {
            FlushSize(1080, 2400);
            FlushSize(1080, 2400);

            Assert.AreEqual(1, _eventCount);

            _calculator.SetScreenSize(1920, 1080);
            _calculator.SetScreenSize(1080, 2400);
            _calculator.FlushFrame();

            Assert.AreEqual(1, _eventCount);
        }

        [Test]
        public void FlushWithoutAnySize_DoesNotRaiseEventAndHasNoFrame()
        {
            _calculator.FlushFrame();

            Assert.AreEqual(0, _eventCount);
            Assert.IsFalse(_calculator.HasFrame);
        }

        [Test]
        public void Contains_NoFrameYet_TreatsWholeScreenAsInside()
        {
            Assert.IsTrue(_calculator.Contains(-500f, 99999f));
        }

        [Test]
        public void Contains_Desktop1920x1080_InsideFrameTrueOnPillarFalse()
        {
            FlushSize(1920, 1080);

            Assert.IsTrue(_calculator.Contains(960f, 540f));
            Assert.IsFalse(_calculator.Contains(100f, 540f));
            Assert.IsFalse(_calculator.Contains(1800f, 540f));
        }

        [Test]
        public void Config_DefaultAspectRange_IsValid()
        {
            Assert.IsTrue(_config.HasValidAspectRange);
        }

        [Test]
        public void Config_MinNotLessThanMax_IsInvalid()
        {
            _config.SetAspectRangeForTest(0.5625f, 0.5625f);
            Assert.IsFalse(_config.HasValidAspectRange);

            _config.SetAspectRangeForTest(0.6f, 0.5f);
            Assert.IsFalse(_config.HasValidAspectRange);
        }

        [Test]
        public void Config_MaxAboveReferenceRatio_IsInvalid()
        {
            _config.SetAspectRangeForTest(0.4286f, 0.6f);

            Assert.IsFalse(_config.HasValidAspectRange);
        }

        [Test]
        public void Constructor_InvalidAspectRange_Throws()
        {
            _config.SetAspectRangeForTest(0.6f, 0.5f);

            Assert.Throws<ArgumentException>(() => new ViewportCalculator(_config));
            Assert.Throws<ArgumentNullException>(() => new ViewportCalculator(null));
        }
    }
}
