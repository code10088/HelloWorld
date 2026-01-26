using ProtoBuf;
using System;
using System.Buffers.Binary;
using System.IO;

public class SerializeHandle
{
    private Func<byte[], int, bool> receive;
    private byte[] headBuffer = new byte[4];
    private byte[] bodyBuffer = new byte[2048];
    private int headPos = 0;
    private int bodyPos = 0;
    private int headLength = 4;
    private int bodyLength = 0;

    public SerializeHandle(Func<byte[], int, bool> receive)
    {
        this.receive = receive;
    }
    public BufferStream Serialize(ushort id, IExtensible msg)
    {
        var stream = new BufferStream(256, 6);
        Serializer.Serialize(stream, msg);
        stream.WriteAt(stream.WPos - 4, 0);
        stream.WriteAt(id, 4);
        return stream;
    }
    public bool Deserialize(byte[] buffer, int length)
    {
        int pos = 0;
        while (pos < length)
        {
            if (headPos < headLength)
            {
                int l = Math.Min(headLength - headPos, length - pos);
                Buffer.BlockCopy(buffer, pos, headBuffer, headPos, l);
                pos += l;
                headPos += l;
                if (headPos == headLength)
                {
                    bodyLength = BitConverter.ToInt32(headBuffer, 0);
                    if (bodyLength < 0 || bodyLength > bodyBuffer.Length)
                    {
                        headPos = 0;
                        bodyPos = 0;
                        bodyLength = 0;
                        return false;
                    }
                }
            }
            else
            {
                int l = Math.Min(bodyLength - bodyPos, length - pos);
                Buffer.BlockCopy(buffer, pos, bodyBuffer, bodyPos, l);
                pos += l;
                bodyPos += l;
                if (bodyPos == bodyLength)
                {
                    headPos = 0;
                    bodyPos = 0;
                    bool b = receive(bodyBuffer, bodyLength);
                    bodyLength = 0;
                    if (!b) return false;
                }
            }
        }
        return true;
    }
    public void Dispose()
    {
        headPos = 0;
        bodyPos = 0;
        bodyLength = 0;
    }
}
public class BufferStream : Stream
{
    private byte[] buffer;
    private int capacity;
    private int length;
    private bool wr = true;
    private int wPos;
    private int rPos;

    public override bool CanRead => true;
    public override bool CanSeek => true;
    public override bool CanWrite => true;
    public override long Length => length;
    public byte[] Buffer => buffer;
    public int WPos => wPos;
    public int RPos => rPos;

    public BufferStream(int size = 256, int woffset = 0, int roffset = 0)
    {
        buffer = BufferPool.Rent(size);
        capacity = buffer.Length;
        wPos = woffset;
        rPos = roffset;
        length = woffset;
    }
    private void EnsureCapacity(int count)
    {
        if (count <= capacity) return;
        var _buffer = BufferPool.Rent(count);
        System.Buffer.BlockCopy(buffer, 0, _buffer, 0, length);
        buffer?.Return();
        buffer = _buffer;
        capacity = buffer.Length;
    }
    public override long Position
    {
        get
        {
            return wr ? wPos : rPos;
        }
        set
        {
            if (value < 0 || value > length) throw new ArgumentOutOfRangeException();
            if (wr) wPos = (int)value;
            else rPos = (int)value;
        }
    }
    public override void SetLength(long value)
    {
        int _length = (int)value;
        EnsureCapacity(_length);
        length = _length;
        if (wPos > length) wPos = length;
        if (rPos > length) rPos = length;
    }
    public override void Flush() { }
    protected override void Dispose(bool disposing)
    {
        buffer?.Return();
        buffer = null;
        base.Dispose(disposing);
    }

    #region 写
    public void Change2Write()
    {
        wr = true;
    }
    public override void Write(byte[] _buffer, int offset, int count)
    {
        wr = true;
        if (count == 0) return;
        EnsureCapacity(wPos + count);
        System.Buffer.BlockCopy(_buffer, offset, buffer, wPos, count);
        wPos += count;
        if (wPos > length) length = wPos;
    }
    public void WriteAt(ushort value, int offset)
    {
        wr = true;
        EnsureCapacity(offset + 2);
        BinaryPrimitives.WriteUInt16LittleEndian(buffer.AsSpan(offset, 2), value);
        if (offset + 2 > length) length = offset + 2;
    }
    public void WriteAt(int value, int offset)
    {
        wr = true;
        EnsureCapacity(offset + 4);
        BinaryPrimitives.WriteInt32LittleEndian(buffer.AsSpan(offset, 4), value);
        if (offset + 4 > length) length = offset + 4;
    }
    public override void WriteByte(byte value)
    {
        wr = true;
        EnsureCapacity(wPos + 1);
        buffer[wPos++] = value;
        if (wPos > length) length = wPos;
    }
    #endregion

    #region 读
    public void Change2Read()
    {
        wr = false;
    }
    public override int Read(byte[] _buffer, int offset, int count)
    {
        wr = false;
        if (count == 0) return 0;
        int available = length - rPos;
        if (available <= 0) return 0;
        if (count > available) count = available;
        System.Buffer.BlockCopy(buffer, rPos, _buffer, offset, count);
        rPos += count;
        return count;
    }
    public override int ReadByte()
    {
        if (rPos >= length) return -1;
        return buffer[rPos++];
    }
    public override long Seek(long offset, SeekOrigin origin)
    {
        long pos = origin switch
        {
            SeekOrigin.Begin => offset,
            SeekOrigin.Current => Position + offset,
            SeekOrigin.End => length + offset,
        };
        Position = pos;
        return pos;
    }
    #endregion
}