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
    public WriteBuffer Serialize(ushort id, IExtensible msg)
    {
        var wb = new WriteBuffer(256, 6);
        Serializer.Serialize(wb, msg);
        wb.Write(wb.Pos - 4, 0);
        wb.Write(id, 4);
        return wb;
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
                    if (bodyLength < 0 || bodyLength > 0x2800)
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
public class WriteBuffer : Stream
{
    private byte[] buffer;
    private int pos;

    public byte[] Buffer => buffer;
    public int Pos => pos;

    public WriteBuffer(int size = 256, int offset = 0)
    {
        buffer = BufferPool.Rent(size);
        pos = offset;
    }
    public override void Write(byte[] bytes, int offset, int count)
    {
        if (count == 0) return;
        if (pos + count > buffer.Length)
        {
            var _buffer = BufferPool.Rent(Math.Max(pos + count, buffer.Length * 2));
            System.Buffer.BlockCopy(buffer, 0, _buffer, 0, pos);
            buffer.Return();
            buffer = _buffer;
        }
        System.Buffer.BlockCopy(bytes, offset, buffer, pos, count);
        pos += count;
    }
    public void Write(ushort value, int offset)
    {
        BinaryPrimitives.WriteUInt16LittleEndian(buffer.AsSpan(offset, 2), value);
    }
    public void Write(int value, int offset)
    {
        BinaryPrimitives.WriteInt32LittleEndian(buffer.AsSpan(offset, 4), value);
    }
    protected override void Dispose(bool disposing)
    {
        buffer?.Return();
        buffer = null;
        base.Dispose(disposing);
    }

    public override bool CanRead => false;
    public override bool CanSeek => false;
    public override bool CanWrite => true;
    public override long Length => pos;
    public override long Position { get => pos; set => throw new NotSupportedException(); }
    public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
    public override void SetLength(long value) => throw new NotSupportedException();
    public override int ReadByte() => throw new NotSupportedException();
    public override void Flush() { }
}