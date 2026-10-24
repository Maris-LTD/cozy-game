using CozyGachapon.Foundation;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace CozyGachapon
{
    public sealed class PointerPressInputAdapter : MonoBehaviour
    {
        private const int MousePointerId = -1;

        private WorldPressRouter _worldPressRouter;
        private FirstInteractionDetector _firstInteractionDetector;

        public void Initialize(WorldPressRouter worldPressRouter, FirstInteractionDetector firstInteractionDetector)
        {
            _worldPressRouter = worldPressRouter;
            _firstInteractionDetector = firstInteractionDetector;
        }

        private void Update()
        {
            if (_worldPressRouter == null || _firstInteractionDetector == null)
            {
                return;
            }

            Touchscreen touchscreen = Touchscreen.current;
            if (touchscreen != null)
            {
                for (int i = 0; i < touchscreen.touches.Count; i++)
                {
                    TouchControl touch = touchscreen.touches[i];
                    if (touch.press.wasPressedThisFrame)
                    {
                        Route(touch.position.ReadValue(), touch.touchId.ReadValue());
                    }
                }
            }

            Mouse mouse = Mouse.current;
            if (mouse != null && mouse.leftButton.wasPressedThisFrame)
            {
                Route(mouse.position.ReadValue(), MousePointerId);
            }
        }

        private void Route(Vector2 screenPosition, int pointerId)
        {
            _firstInteractionDetector.NotifyUserGesture();
            _worldPressRouter.RoutePress(Time.frameCount, screenPosition.x, screenPosition.y, pointerId);
        }
    }
}
