using System.Buffers;

public static class BufferPool
{
    private static ArrayPool<byte> pool = ArrayPool<byte>.Shared;
    private static int NextPowerOfTwo(int x)
    {
        if (x <= 0) return 0;
        x--;
        x |= x >> 1;
        x |= x >> 2;
        x |= x >> 4;
        x |= x >> 8;
        x |= x >> 16;
        return x + 1;
    }
    public static byte[] Rent(int size)
    {
        size = NextPowerOfTwo(size);
        return pool.Rent(size);
    }
    public static void Return(this byte[] buffer)
    {
        var l= buffer.Length;
        if((l & (l - 1)) == 0) pool.Return(buffer);
    }
}