using System.Buffers;

public static class BufferPool
{
    private static ArrayPool<byte> pool = ArrayPool<byte>.Shared;

    public static byte[] Rent(int size) => pool.Rent(size);
    public static void Return(this byte[] buffer) => pool.Return(buffer);
}