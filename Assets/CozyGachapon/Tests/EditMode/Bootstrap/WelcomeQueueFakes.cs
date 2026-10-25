using System;
using System.Collections.Generic;
using CozyGachapon.Contracts;

namespace CozyGachapon.Tests.Bootstrap
{
    public class FakeWelcomeStep : IWelcomeStep
    {
        private readonly List<string> _showLog;

        public string Id { get; }
        public bool ShouldShowValue { get; set; } = true;
        public bool CompleteImmediately { get; set; }
        public int ShowCount { get; private set; }
        public Action PendingDone { get; private set; }

        public FakeWelcomeStep(string id, List<string> showLog)
        {
            Id = id;
            _showLog = showLog;
        }

        public bool ShouldShow() => ShouldShowValue;

        public void Show(Action onDone)
        {
            ShowCount++;
            PendingDone = onDone;
            _showLog.Add(Id);
            if (CompleteImmediately)
            {
                onDone();
            }
        }
    }
}
