namespace CozyGachapon.Contracts
{
    public readonly struct PlayResult
    {
        public bool Played { get; }
        public DropReason Reason { get; }

        private PlayResult(bool played, DropReason reason)
        {
            Played = played;
            Reason = reason;
        }

        public static PlayResult Success()
        {
            return new PlayResult(true, DropReason.None);
        }

        public static PlayResult Dropped(DropReason reason)
        {
            return new PlayResult(false, reason);
        }
    }
}
