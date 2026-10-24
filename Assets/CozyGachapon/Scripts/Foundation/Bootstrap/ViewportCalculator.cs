using System;
using CozyGachapon.Contracts;

namespace CozyGachapon.Foundation
{
    public sealed class ViewportCalculator : IFrameRectProvider
    {
        private readonly SceneFlowConfig _config;
        private int _pendingWidth;
        private int _pendingHeight;
        private int _appliedWidth;
        private int _appliedHeight;

        public bool HasFrame { get; private set; }
        public FrameChangedEvent CurrentFrame { get; private set; }

        public event Action<FrameChangedEvent> OnFrameChanged;

        public ViewportCalculator(SceneFlowConfig config)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            if (!config.HasValidAspectRange)
            {
                throw new ArgumentException("Invalid aspect range", nameof(config));
            }

            _config = config;
        }

        public void SetScreenSize(int width, int height)
        {
            _pendingWidth = width;
            _pendingHeight = height;
        }

        public void FlushFrame()
        {
            if (_pendingWidth <= 0 || _pendingHeight <= 0)
            {
                return;
            }

            if (HasFrame && _pendingWidth == _appliedWidth && _pendingHeight == _appliedHeight)
            {
                return;
            }

            if (!_config.HasValidAspectRange)
            {
                return;
            }

            _appliedWidth = _pendingWidth;
            _appliedHeight = _pendingHeight;
            CurrentFrame = FrameRectCalculator.Compute(
                _appliedWidth,
                _appliedHeight,
                _config.MinAspect,
                _config.MaxAspect,
                _config.ReferenceWidth,
                _config.ReferenceHeight);
            HasFrame = true;
            OnFrameChanged?.Invoke(CurrentFrame);
        }

        public bool Contains(float screenX, float screenY)
        {
            if (!HasFrame)
            {
                return true;
            }

            FrameChangedEvent frame = CurrentFrame;
            return screenX >= frame.FrameX
                && screenX <= frame.FrameX + frame.FrameWidth
                && screenY >= frame.FrameY
                && screenY <= frame.FrameY + frame.FrameHeight;
        }
    }
}
