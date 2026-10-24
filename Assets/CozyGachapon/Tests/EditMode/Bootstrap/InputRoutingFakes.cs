using System.Collections.Generic;
using CozyGachapon.Contracts;

namespace CozyGachapon.Tests.Bootstrap
{
    public sealed class FakeWorldViewport : IWorldViewport
    {
        private readonly float _minX;
        private readonly float _minY;
        private readonly float _maxX;
        private readonly float _maxY;
        private readonly float _offsetX;
        private readonly float _offsetY;

        public int ReceiveCount { get; private set; }
        public WorldPressEvent LastPress { get; private set; }

        public FakeWorldViewport(float minX, float minY, float maxX, float maxY, float offsetX = 0f, float offsetY = 0f)
        {
            _minX = minX;
            _minY = minY;
            _maxX = maxX;
            _maxY = maxY;
            _offsetX = offsetX;
            _offsetY = offsetY;
        }

        public bool TryScreenToWorld(float screenX, float screenY, out float worldX, out float worldY)
        {
            worldX = screenX + _offsetX;
            worldY = screenY + _offsetY;
            return screenX >= _minX && screenX <= _maxX && screenY >= _minY && screenY <= _maxY;
        }

        public void ReceiveWorldPress(WorldPressEvent worldPress)
        {
            ReceiveCount++;
            LastPress = worldPress;
        }
    }

    public sealed class FakeUiHitTester : IUiHitTester
    {
        public bool Hits { get; set; }

        public bool HitsUi(float screenX, float screenY, int pointerId)
        {
            return Hits;
        }
    }

    public sealed class FakeFrameRect : IFrameRectProvider
    {
        private readonly float _maxX;
        private readonly float _maxY;

        public FakeFrameRect(float maxX, float maxY)
        {
            _maxX = maxX;
            _maxY = maxY;
        }

        public bool Contains(float screenX, float screenY)
        {
            return screenX >= 0f && screenY >= 0f && screenX <= _maxX && screenY <= _maxY;
        }
    }

    public sealed class FakeModalBackHandler : IModalBackHandler
    {
        private readonly List<string> _callLog;
        private readonly string _name;

        public int CallCount { get; private set; }

        public FakeModalBackHandler(string name, List<string> callLog)
        {
            _name = name;
            _callLog = callLog;
        }

        public void HandleBack()
        {
            CallCount++;
            _callLog.Add(_name);
        }
    }

    public sealed class FakeBackgroundRequester : IAppBackgroundRequester
    {
        public int RequestCount { get; private set; }

        public void RequestMoveToBackground()
        {
            RequestCount++;
        }
    }

    public sealed class FakeBootStatus : IBootStatusReader
    {
        public bool IsBooting { get; set; }
        public bool IsBootFailed { get; set; }
    }
}
