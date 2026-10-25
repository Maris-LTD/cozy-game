namespace CozyGachapon.Contracts
{
    public readonly struct PlayerActionContext
    {
        public PlayerActionKind Kind { get; }
        public long Ticket { get; }

        public bool IsValid => Kind != PlayerActionKind.None && Ticket > 0L;

        internal PlayerActionContext(PlayerActionKind kind, long ticket)
        {
            Kind = kind;
            Ticket = ticket;
        }
    }
}
