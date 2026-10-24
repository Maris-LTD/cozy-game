namespace CozyGachapon.Contracts
{
    public enum WorldPressResult
    {
        Routed = 0,
        NotFirstInFrame = 1,
        OutsideFrame = 2,
        HitUi = 3,
        Blocked = 4,
        NoViewport = 5
    }
}
