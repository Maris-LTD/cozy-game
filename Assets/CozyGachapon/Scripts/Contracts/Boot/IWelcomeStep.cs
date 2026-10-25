using System;

namespace CozyGachapon.Contracts
{
    public interface IWelcomeStep
    {
        string Id { get; }
        bool ShouldShow();
        void Show(Action onDone);
    }
}
