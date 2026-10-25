namespace CozyGachapon
{
    public sealed class RetryTriggerGuard
    {
        public bool IsTriggered { get; private set; }

        public bool TryTrigger()
        {
            if (IsTriggered)
            {
                return false;
            }

            IsTriggered = true;
            return true;
        }
    }
}
