using System;
using CozyGachapon.Contracts;

namespace CozyGachapon.Foundation
{
    public sealed class FirstInteractionDetector
    {
        public event Action<FirstInteractionEvent> OnFirstInteraction;

        public bool HasFired { get; private set; }

        public bool NotifyUserGesture()
        {
            if (HasFired)
            {
                return false;
            }

            HasFired = true;
            OnFirstInteraction?.Invoke(new FirstInteractionEvent());
            return true;
        }
    }
}
