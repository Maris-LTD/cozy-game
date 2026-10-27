namespace CozyGachapon.Contracts.Inventory
{
    public enum AddPetRejectReason
    {
        None = 0,
        NotReady = 1,
        InvalidAmount = 2,
        UnknownId = 3,
        QueueFull = 4
    }
}
