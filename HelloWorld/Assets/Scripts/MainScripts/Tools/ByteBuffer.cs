using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

public sealed class SafeByteBuffer
{
    private static object sync = new object();
    private static Dictionary<int, Stack<SafeByteBuffer>> pool = new();
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
    public static SafeByteBuffer Rent(int capacity = 256)
    {
        int size = NextPowerOfTwo(capacity);
        if (size <= 0) throw new ArgumentOutOfRangeException(nameof(capacity));
        lock (sync)
        {
            if (pool.TryGetValue(size, out var stack) && stack.Count > 0)
            {
                return stack.Pop();
            }
        }
        return new SafeByteBuffer(size);
    }
    public static void Return(SafeByteBuffer b)
    {
        if (b == null) return;
        b.Clear();
        int key = b.Capacity;
        lock (sync)
        {
            if (!pool.TryGetValue(key, out var stack))
            {
                stack = new Stack<SafeByteBuffer>();
                pool.Add(key, stack);
            }
            stack.Push(b);
        }
    }


    private byte[] _buffer;
    private int _wpos;
    private int _rpos;

    public int WPos => _wpos;
    public int RPos => _rpos;
    public int Length => _wpos;
    public int Capacity => _buffer.Length;
    public Memory<byte> Memory => _buffer.AsMemory(0, _wpos);

    public SafeByteBuffer(int capacity = 1024)
    {
        if (capacity <= 0) throw new ArgumentOutOfRangeException(nameof(capacity));
        int _capacity = NextPowerOfTwo(capacity);
        if (_capacity <= 0) throw new ArgumentOutOfRangeException(nameof(capacity));
        _buffer = new byte[_capacity];
        _wpos = 0;
        _rpos = 0;
    }
    public void SetWPos(int pos)
    {
        if (pos > _buffer.Length) throw new ArgumentOutOfRangeException(nameof(pos));
        _wpos = pos;
    }
    public void SetRPos(int pos)
    {
        if (pos > _wpos) throw new ArgumentOutOfRangeException(nameof(pos));
        _rpos = pos;
    }
    public void Clear()
    {
        _wpos = 0;
        _rpos = 0;
    }

    #region 扩容
    private void EnsureCapacity(int need)
    {
        if (_wpos + need <= _buffer.Length) return;
        int size = NextPowerOfTwo(Math.Max(_buffer.Length * 2, _wpos + need));
        if (size <= 0) throw new OutOfMemoryException();
        Array.Resize(ref _buffer, size);
    }
    private void EnsureReadable(int need)
    {
        if (_rpos + need > _wpos) throw new IndexOutOfRangeException("SafeByteBuffer readable bytes are not enough.");
    }
    #endregion

    #region 基础写入
    public void WriteByte(byte value)
    {
        EnsureCapacity(1);
        _buffer[_wpos++] = value;
    }
    public void WriteShort(short value)
    {
        EnsureCapacity(2);
        BinaryPrimitives.WriteInt16LittleEndian(_buffer.AsSpan(_wpos, 2), value);
        _wpos += 2;
    }
    public void WriteUShort(ushort value)
    {
        EnsureCapacity(2);
        BinaryPrimitives.WriteUInt16LittleEndian(_buffer.AsSpan(_wpos, 2), value);
        _wpos += 2;
    }
    public void WriteInt(int value)
    {
        EnsureCapacity(4);
        BinaryPrimitives.WriteInt32LittleEndian(_buffer.AsSpan(_wpos, 4), value);
        _wpos += 4;
    }
    public void WriteUInt(uint value)
    {
        EnsureCapacity(4);
        BinaryPrimitives.WriteUInt32LittleEndian(_buffer.AsSpan(_wpos, 4), value);
        _wpos += 4;
    }
    public void WriteLong(long value)
    {
        EnsureCapacity(8);
        BinaryPrimitives.WriteInt64LittleEndian(_buffer.AsSpan(_wpos, 8), value);
        _wpos += 8;
    }
    public void WriteFloat(float value)
    {
        EnsureCapacity(4);
        var bytes = BitConverter.GetBytes(value);
        if (!BitConverter.IsLittleEndian) Array.Reverse(bytes);
        Buffer.BlockCopy(bytes, 0, _buffer, _wpos, 4);
        _wpos += 4;
    }
    public void WriteDouble(double value)
    {
        EnsureCapacity(8);
        var bytes = BitConverter.GetBytes(value);
        if (!BitConverter.IsLittleEndian) Array.Reverse(bytes);
        Buffer.BlockCopy(bytes, 0, _buffer, _wpos, 8);
        _wpos += 8;
    }
    public void WriteString(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            WriteUShort(0);
            return;
        }
        int byteCount = Encoding.UTF8.GetByteCount(value);
        if (byteCount > ushort.MaxValue) throw new ArgumentException("String UTF8 byte length cannot exceed 65535.", nameof(value));
        EnsureCapacity(2 + byteCount);
        BinaryPrimitives.WriteUInt16LittleEndian(_buffer.AsSpan(_wpos, 2), (ushort)byteCount);
        _wpos += 2;
        Encoding.UTF8.GetBytes(value, _buffer.AsSpan(_wpos, byteCount));
        _wpos += byteCount;
    }
    #endregion

    #region 数组批量写入
    public void WriteByteArray(byte[] arr)
    {
        int count = arr == null ? 0 : arr.Length;
        WriteInt(count);
        if (count == 0) return;
        EnsureCapacity(count);
        Buffer.BlockCopy(arr, 0, _buffer, _wpos, count);
        _wpos += count;
    }
    public void WriteShortArray(short[] arr)
    {
        int count = arr == null ? 0 : arr.Length;
        WriteInt(count);
        for (int i = 0; i < count; i++) WriteShort(arr[i]);
    }
    public void WriteUShortArray(ushort[] arr)
    {
        int count = arr == null ? 0 : arr.Length;
        WriteInt(count);
        for (int i = 0; i < count; i++) WriteUShort(arr[i]);
    }
    public void WriteIntArray(int[] arr)
    {
        int count = arr == null ? 0 : arr.Length;
        WriteInt(count);
        for (int i = 0; i < count; i++) WriteInt(arr[i]);
    }
    public void WriteUIntArray(uint[] arr)
    {
        int count = arr == null ? 0 : arr.Length;
        WriteInt(count);
        for (int i = 0; i < count; i++) WriteUInt(arr[i]);
    }
    public void WriteLongArray(long[] arr)
    {
        int count = arr == null ? 0 : arr.Length;
        WriteInt(count);
        for (int i = 0; i < count; i++) WriteLong(arr[i]);
    }
    public void WriteFloatArray(float[] arr)
    {
        int count = arr == null ? 0 : arr.Length;
        WriteInt(count);
        for (int i = 0; i < count; i++) WriteFloat(arr[i]);
    }
    public void WriteDoubleArray(double[] arr)
    {
        int count = arr == null ? 0 : arr.Length;
        WriteInt(count);
        for (int i = 0; i < count; i++) WriteDouble(arr[i]);
    }
    public void WriteStringArray(string[] arr)
    {
        int count = arr == null ? 0 : arr.Length;
        WriteInt(count);
        for (int i = 0; i < count; i++) WriteString(arr[i]);
    }
    #endregion

    #region List 批量写入
    public void WriteByteList(List<byte> list)
    {
        int count = list == null ? 0 : list.Count;
        WriteInt(count);
        if (count == 0) return;
        EnsureCapacity(count);
        for (int i = 0; i < count; i++) _buffer[_wpos++] = list[i];
    }
    public void WriteShortList(List<short> list)
    {
        int count = list == null ? 0 : list.Count;
        WriteInt(count);
        for (int i = 0; i < count; i++) WriteShort(list[i]);
    }
    public void WriteUShortList(List<ushort> list)
    {
        int count = list == null ? 0 : list.Count;
        WriteInt(count);
        for (int i = 0; i < count; i++) WriteUShort(list[i]);
    }
    public void WriteIntList(List<int> list)
    {
        int count = list == null ? 0 : list.Count;
        WriteInt(count);
        for (int i = 0; i < count; i++) WriteInt(list[i]);
    }
    public void WriteUIntList(List<uint> list)
    {
        int count = list == null ? 0 : list.Count;
        WriteInt(count);
        for (int i = 0; i < count; i++) WriteUInt(list[i]);
    }
    public void WriteLongList(List<long> list)
    {
        int count = list == null ? 0 : list.Count;
        WriteInt(count);
        for (int i = 0; i < count; i++) WriteLong(list[i]);
    }
    public void WriteFloatList(List<float> list)
    {
        int count = list == null ? 0 : list.Count;
        WriteInt(count);
        for (int i = 0; i < count; i++) WriteFloat(list[i]);
    }
    public void WriteDoubleList(List<double> list)
    {
        int count = list == null ? 0 : list.Count;
        WriteInt(count);
        for (int i = 0; i < count; i++) WriteDouble(list[i]);
    }
    public void WriteStringList(List<string> list)
    {
        int count = list == null ? 0 : list.Count;
        WriteInt(count);
        for (int i = 0; i < count; i++) WriteString(list[i]);
    }
    #endregion

    #region 基础读取
    public byte ReadByte()
    {
        EnsureReadable(1);
        return _buffer[_rpos++];
    }
    public short ReadShort()
    {
        EnsureReadable(2);
        var v = BinaryPrimitives.ReadInt16LittleEndian(_buffer.AsSpan(_rpos, 2));
        _rpos += 2;
        return v;
    }
    public ushort ReadUShort()
    {
        EnsureReadable(2);
        var v = BinaryPrimitives.ReadUInt16LittleEndian(_buffer.AsSpan(_rpos, 2));
        _rpos += 2;
        return v;
    }
    public int ReadInt()
    {
        EnsureReadable(4);
        var v = BinaryPrimitives.ReadInt32LittleEndian(_buffer.AsSpan(_rpos, 4));
        _rpos += 4;
        return v;
    }
    public uint ReadUInt()
    {
        EnsureReadable(4);
        var v = BinaryPrimitives.ReadUInt32LittleEndian(_buffer.AsSpan(_rpos, 4));
        _rpos += 4;
        return v;
    }
    public long ReadLong()
    {
        EnsureReadable(8);
        var v = BinaryPrimitives.ReadInt64LittleEndian(_buffer.AsSpan(_rpos, 8));
        _rpos += 8;
        return v;
    }
    public float ReadFloat()
    {
        EnsureReadable(4);
        var v = BitConverter.ToSingle(_buffer, _rpos);
        _rpos += 4;
        return v;
    }
    public double ReadDouble()
    {
        EnsureReadable(8);
        var v = BitConverter.ToDouble(_buffer, _rpos);
        _rpos += 8;
        return v;
    }
    public string ReadString()
    {
        ushort len = ReadUShort();
        if (len == 0) return string.Empty;
        EnsureReadable(len);
        var value = Encoding.UTF8.GetString(_buffer, _rpos, len);
        _rpos += len;
        return value;
    }
    #endregion

    #region 数组批量读取
    public byte[] ReadByteArray()
    {
        int count = ReadInt();
        if (count == 0) return Array.Empty<byte>();
        EnsureReadable(count);
        var arr = new byte[count];
        Buffer.BlockCopy(_buffer, _rpos, arr, 0, count);
        _rpos += count;
        return arr;
    }
    public short[] ReadShortArray()
    {
        int count = ReadInt();
        if (count == 0) return Array.Empty<short>();
        var arr = new short[count];
        for (int i = 0; i < count; i++) arr[i] = ReadShort();
        return arr;
    }
    public ushort[] ReadUShortArray()
    {
        int count = ReadInt();
        if (count == 0) return Array.Empty<ushort>();
        var arr = new ushort[count];
        for (int i = 0; i < count; i++) arr[i] = ReadUShort();
        return arr;
    }
    public int[] ReadIntArray()
    {
        int count = ReadInt();
        if (count == 0) return Array.Empty<int>();
        var arr = new int[count];
        for (int i = 0; i < count; i++) arr[i] = ReadInt();
        return arr;
    }
    public uint[] ReadUIntArray()
    {
        int count = ReadInt();
        if (count == 0) return Array.Empty<uint>();
        var arr = new uint[count];
        for (int i = 0; i < count; i++) arr[i] = ReadUInt();
        return arr;
    }
    public long[] ReadLongArray()
    {
        int count = ReadInt();
        if (count == 0) return Array.Empty<long>();
        var arr = new long[count];
        for (int i = 0; i < count; i++) arr[i] = ReadLong();
        return arr;
    }
    public float[] ReadFloatArray()
    {
        int count = ReadInt();
        if (count == 0) return Array.Empty<float>();
        var arr = new float[count];
        for (int i = 0; i < count; i++) arr[i] = ReadFloat();
        return arr;
    }
    public double[] ReadDoubleArray()
    {
        int count = ReadInt();
        if (count == 0) return Array.Empty<double>();
        var arr = new double[count];
        for (int i = 0; i < count; i++) arr[i] = ReadDouble();
        return arr;
    }
    public string[] ReadStringArray()
    {
        int count = ReadInt();
        if (count == 0) return Array.Empty<string>();
        var arr = new string[count];
        for (int i = 0; i < count; i++) arr[i] = ReadString();
        return arr;
    }
    #endregion

    #region List 批量读取
    public List<byte> ReadByteList()
    {
        int count = ReadInt();
        var list = new List<byte>(count);
        EnsureReadable(count);
        for (int i = 0; i < count; i++) list.Add(_buffer[_rpos++]);
        return list;
    }
    public List<short> ReadShortList()
    {
        int count = ReadInt();
        var list = new List<short>(count);
        for (int i = 0; i < count; i++) list.Add(ReadShort());
        return list;
    }
    public List<ushort> ReadUShortList()
    {
        int count = ReadInt();
        var list = new List<ushort>(count);
        for (int i = 0; i < count; i++) list.Add(ReadUShort());
        return list;
    }
    public List<int> ReadIntList()
    {
        int count = ReadInt();
        var list = new List<int>(count);
        for (int i = 0; i < count; i++) list.Add(ReadInt());
        return list;
    }
    public List<uint> ReadUIntList()
    {
        int count = ReadInt();
        var list = new List<uint>(count);
        for (int i = 0; i < count; i++) list.Add(ReadUInt());
        return list;
    }
    public List<long> ReadLongList()
    {
        int count = ReadInt();
        var list = new List<long>(count);
        for (int i = 0; i < count; i++) list.Add(ReadLong());
        return list;
    }
    public List<float> ReadFloatList()
    {
        int count = ReadInt();
        var list = new List<float>(count);
        for (int i = 0; i < count; i++) list.Add(ReadFloat());
        return list;
    }
    public List<double> ReadDoubleList()
    {
        int count = ReadInt();
        var list = new List<double>(count);
        for (int i = 0; i < count; i++) list.Add(ReadDouble());
        return list;
    }
    public List<string> ReadStringList()
    {
        int count = ReadInt();
        var list = new List<string>(count);
        for (int i = 0; i < count; i++) list.Add(ReadString());
        return list;
    }
    #endregion
}


public sealed unsafe class UnsafeByteBuffer
{
    private static object sync = new object();
    private static Dictionary<int, Stack<UnsafeByteBuffer>> pool = new();
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
    public static UnsafeByteBuffer Rent(int capacity = 256)
    {
        int size = NextPowerOfTwo(capacity);
        if (size <= 0) throw new ArgumentOutOfRangeException(nameof(capacity));
        lock (sync)
        {
            if (pool.TryGetValue(size, out Stack<UnsafeByteBuffer> stack) && stack.Count > 0)
            {
                return stack.Pop();
            }
        }
        return new UnsafeByteBuffer(size);
    }
    public static void Return(UnsafeByteBuffer b)
    {
        if (b == null) return;
        b.Clear();
        int key = b.Capacity;
        lock (sync)
        {
            if (!pool.TryGetValue(key, out var stack))
            {
                stack = new Stack<UnsafeByteBuffer>();
                pool.Add(key, stack);
            }
            stack.Push(b);
        }
    }


    private byte* _ptr;
    private int _wpos;
    private int _rpos;
    private int _capacity;

    public int WPos
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _wpos;
    }
    public int RPos
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _rpos;
    }
    public int Length
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _wpos;
    }
    public int Capacity
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _capacity;
    }
    public Span<byte> Span
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new Span<byte>(_ptr, _wpos);
    }
    public Span<byte> FullSpan
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new Span<byte>(_ptr, _capacity);
    }

    public UnsafeByteBuffer(int capacity = 1024)
    {
        if (capacity <= 0) throw new ArgumentOutOfRangeException(nameof(capacity));
        _capacity = NextPowerOfTwo(capacity);
        if (_capacity <= 0) throw new ArgumentOutOfRangeException(nameof(capacity));
        IntPtr ptr = Marshal.AllocHGlobal(_capacity);
        if (ptr == IntPtr.Zero) throw new OutOfMemoryException();
        _ptr = (byte*)ptr;
        _wpos = 0;
        _rpos = 0;
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetWPos(int pos)
    {
        if (pos > _capacity) throw new ArgumentOutOfRangeException(nameof(pos));
        _wpos = pos;
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetRPos(int pos)
    {
        if (pos > _wpos) throw new ArgumentOutOfRangeException(nameof(pos));
        _rpos = pos;
    }
    public void Clear()
    {
        _wpos = 0;
        _rpos = 0;
    }

    #region 扩容
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void EnsureCapacity(int need)
    {
        if (_wpos + need <= _capacity) return;
        int size = NextPowerOfTwo(Math.Max(_capacity * 2, _wpos + need));
        if (size <= 0) throw new OutOfMemoryException();
        IntPtr newPtr = Marshal.AllocHGlobal(size);
        if (newPtr == IntPtr.Zero) throw new OutOfMemoryException();
        if (_wpos > 0) Buffer.MemoryCopy(_ptr, (void*)newPtr, size, _wpos);
        Marshal.FreeHGlobal((IntPtr)_ptr);
        _ptr = (byte*)newPtr;
        _capacity = size;
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void EnsureReadable(int need)
    {
        if (_rpos + need > _wpos) throw new IndexOutOfRangeException("ByteBuffer readable bytes are not enough.");
    }
    #endregion

    #region 基础写入
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void WriteValue<T>(T value) where T : unmanaged
    {
        int size = sizeof(T);
        EnsureCapacity(size);
        Unsafe.WriteUnaligned(_ptr + _wpos, value);
        _wpos += size;
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteValueAt<T>(int offset, T value) where T : unmanaged
    {
        int size = sizeof(T);
        EnsureCapacity(offset + size);
        Unsafe.WriteUnaligned(_ptr + offset, value);
        if (offset + size > _wpos) _wpos = offset + size;
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteByte(byte value) => WriteValue(value);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteShort(short value) => WriteValue(value);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteUShort(ushort value) => WriteValue(value);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteInt(int value) => WriteValue(value);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteUInt(uint value) => WriteValue(value);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteLong(long value) => WriteValue(value);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteFloat(float value) => WriteValue(value);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteDouble(double value) => WriteValue(value);
    public void WriteString(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            WriteUShort(0);
            return;
        }
        fixed (char* chars = value)
        {
            int byteCount = Encoding.UTF8.GetByteCount(chars, value.Length);
            if (byteCount > ushort.MaxValue) throw new ArgumentException("String UTF8 byte length cannot exceed 65535.", nameof(value));
            EnsureCapacity(2 + byteCount);
            Unsafe.WriteUnaligned(_ptr + _wpos, (ushort)byteCount);
            _wpos += 2;
            Encoding.UTF8.GetBytes(chars, value.Length, _ptr + _wpos, byteCount);
            _wpos += byteCount;
        }
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteSpan(Span<byte> src)
    {
        int size = src.Length;
        if (size == 0) return;
        EnsureCapacity(size);
        fixed (byte* srcPtr = src)
        {
            Buffer.MemoryCopy(srcPtr, _ptr + _wpos, size, size);
        }
        _wpos += size;
    }
    #endregion

    #region 数组批量写入
    private void WriteArrayValues<T>(T[] arr) where T : unmanaged
    {
        int count = arr == null ? 0 : arr.Length;
        WriteInt(count);
        if (count == 0) return;
        int size = sizeof(T);
        int byteCount = count * size;
        EnsureCapacity(byteCount);
        fixed (T* src = arr)
        {
            Buffer.MemoryCopy(src, _ptr + _wpos, byteCount, byteCount);
        }
        _wpos += byteCount;
    }
    public void WriteByteArray(byte[] arr) => WriteArrayValues(arr);
    public void WriteShortArray(short[] arr) => WriteArrayValues(arr);
    public void WriteUShortArray(ushort[] arr) => WriteArrayValues(arr);
    public void WriteIntArray(int[] arr) => WriteArrayValues(arr);
    public void WriteUIntArray(uint[] arr) => WriteArrayValues(arr);
    public void WriteLongArray(long[] arr) => WriteArrayValues(arr);
    public void WriteFloatArray(float[] arr) => WriteArrayValues(arr);
    public void WriteDoubleArray(double[] arr) => WriteArrayValues(arr);
    public void WriteStringArray(string[] arr)
    {
        int count = arr == null ? 0 : arr.Length;
        WriteInt(count);
        for (int i = 0; i < count; i++) WriteString(arr[i]);
    }
    #endregion

    #region List 批量写入
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void WriteListValues<T>(List<T> list) where T : unmanaged
    {
        int count = list == null ? 0 : list.Count;
        WriteInt(count);
        if (count == 0) return;
        int size = sizeof(T);
        EnsureCapacity(count * size);
        for (int i = 0; i < count; i++)
        {
            Unsafe.WriteUnaligned(_ptr + _wpos, list[i]);
            _wpos += size;
        }
    }
    public void WriteByteList(List<byte> list) => WriteListValues(list);
    public void WriteShortList(List<short> list) => WriteListValues(list);
    public void WriteUShortList(List<ushort> list) => WriteListValues(list);
    public void WriteIntList(List<int> list) => WriteListValues(list);
    public void WriteUIntList(List<uint> list) => WriteListValues(list);
    public void WriteLongList(List<long> list) => WriteListValues(list);
    public void WriteFloatList(List<float> list) => WriteListValues(list);
    public void WriteDoubleList(List<double> list) => WriteListValues(list);
    public void WriteStringList(List<string> list)
    {
        int count = list == null ? 0 : list.Count;
        WriteInt(count);
        for (int i = 0; i < count; i++) WriteString(list[i]);
    }
    #endregion

    #region 基础读取
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private T ReadValue<T>() where T : unmanaged
    {
        int size = sizeof(T);
        EnsureReadable(size);
        T value = Unsafe.ReadUnaligned<T>(_ptr + _rpos);
        _rpos += size;
        return value;
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public byte ReadByte() => ReadValue<byte>();
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public short ReadShort() => ReadValue<short>();
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ushort ReadUShort() => ReadValue<ushort>();
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int ReadInt() => ReadValue<int>();
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public uint ReadUInt() => ReadValue<uint>();
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public long ReadLong() => ReadValue<long>();
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float ReadFloat() => ReadValue<float>();
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public double ReadDouble() => ReadValue<double>();
    public string ReadString()
    {
        ushort len = ReadUShort();
        if (len == 0) return string.Empty;
        EnsureReadable(len);
        string value = Encoding.UTF8.GetString(_ptr + _rpos, len);
        _rpos += len;
        return value;
    }
    #endregion

    #region 数组批量读取
    private T[] ReadArray<T>() where T : unmanaged
    {
        int count = ReadInt();
        if (count == 0) return Array.Empty<T>();
        int byteCount = count * sizeof(T);
        EnsureReadable(byteCount);
        T[] arr = new T[count];
        fixed (T* dst = arr)
        {
            Buffer.MemoryCopy(_ptr + _rpos, dst, byteCount, byteCount);
        }
        _rpos += byteCount;
        return arr;
    }
    public byte[] ReadByteArray() => ReadArray<byte>();
    public short[] ReadShortArray() => ReadArray<short>();
    public ushort[] ReadUShortArray() => ReadArray<ushort>();
    public int[] ReadIntArray() => ReadArray<int>();
    public uint[] ReadUIntArray() => ReadArray<uint>();
    public long[] ReadLongArray() => ReadArray<long>();
    public float[] ReadFloatArray() => ReadArray<float>();
    public double[] ReadDoubleArray() => ReadArray<double>();
    public string[] ReadStringArray()
    {
        int count = ReadInt();
        if (count == 0) return Array.Empty<string>();
        string[] arr = new string[count];
        for (int i = 0; i < count; i++) arr[i] = ReadString();
        return arr;
    }
    #endregion

    #region List 批量读取
    private List<T> ReadListValues<T>() where T : unmanaged
    {
        int count = ReadInt();
        var list = new List<T>(count);
        if (count == 0) return list;
        int size = sizeof(T);
        int byteCount = count * size;
        EnsureReadable(byteCount);
        for (int i = 0; i < count; i++)
        {
            list.Add(Unsafe.ReadUnaligned<T>(_ptr + _rpos));
            _rpos += size;
        }
        return list;
    }
    public List<byte> ReadByteList() => ReadListValues<byte>();
    public List<short> ReadShortList() => ReadListValues<short>();
    public List<ushort> ReadUShortList() => ReadListValues<ushort>();
    public List<int> ReadIntList() => ReadListValues<int>();
    public List<uint> ReadUIntList() => ReadListValues<uint>();
    public List<long> ReadLongList() => ReadListValues<long>();
    public List<float> ReadFloatList() => ReadListValues<float>();
    public List<double> ReadDoubleList() => ReadListValues<double>();
    public List<string> ReadStringList()
    {
        int count = ReadInt();
        var list = new List<string>(count);
        for (int i = 0; i < count; i++) list.Add(ReadString());
        return list;
    }
    #endregion
}
