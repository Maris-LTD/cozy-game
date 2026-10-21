namespace CozyGachapon.Contracts
{
    public enum BootPhase
    {
        ServiceInit = 0,
        DataLoad = 1,
        SaveLoad = 2,
        GameplayInit = 3,
        Prewarm = 4
    }
}
