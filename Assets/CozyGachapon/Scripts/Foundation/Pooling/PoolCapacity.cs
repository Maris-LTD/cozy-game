namespace CozyGachapon.Pooling
{
    public static class PoolCapacity
    {
        public static int Compute(int clientDeclaredMax, int headroom)
        {
            return clientDeclaredMax + headroom;
        }

        public static long ComputeWide(int clientDeclaredMax, int headroom)
        {
            return (long)clientDeclaredMax + headroom;
        }
    }
}
