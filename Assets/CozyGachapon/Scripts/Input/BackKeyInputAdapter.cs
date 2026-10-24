using CozyGachapon.Foundation;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CozyGachapon
{
    public sealed class BackKeyInputAdapter : MonoBehaviour
    {
        private BackButtonRouter _backButtonRouter;
        private FirstInteractionDetector _firstInteractionDetector;

        public void Initialize(BackButtonRouter backButtonRouter, FirstInteractionDetector firstInteractionDetector)
        {
            _backButtonRouter = backButtonRouter;
            _firstInteractionDetector = firstInteractionDetector;
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null || _backButtonRouter == null || _firstInteractionDetector == null)
            {
                return;
            }

            if (keyboard.anyKey.wasPressedThisFrame)
            {
                _firstInteractionDetector.NotifyUserGesture();
            }

            if (keyboard.escapeKey.wasPressedThisFrame)
            {
                _backButtonRouter.HandleBack();
            }
        }
    }
}
