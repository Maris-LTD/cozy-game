namespace CozyGachapon.Contracts
{
    public enum BackPressResult
    {
        IgnoredWhileBooting = 0,
        HandledByModal = 1,
        ReturnedToBoard = 2,
        TabLocked = 3,
        MovedToBackground = 4,
        NoAction = 5
    }
}
