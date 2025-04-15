using ProtoBuf;
using System;

public partial class NetMsgDispatch
{
    /// <summary>
    /// ProtoBuf < FlatBuffers < MemoryBuffer
    /// ProtoBuf:       序列化快1，反序列化慢100，体积小1
    /// FlatBuffers:    序列化慢2，反序列化快1，体积大2
    /// MemoryBuffer:   只支持C#
    /// </summary>
    public bool Deserialize(byte[] bytes)
    {
        try
        {
            var id = BitConverter.ToUInt16(bytes, 0);
            var mm = new Memory<byte>(bytes, 2, bytes.Length - 2);
            IExtensible msg = null;
            switch ((NetMsgId)id)
            {
                case NetMsgId.Person: msg = Serializer.Deserialize<ProtoTest.Person>(mm); break;
            }
            Add(id, msg);
            return true;
        }
        catch (Exception e)
        {
            GameDebug.LogError(e.Message);
            return false;
        }
    }
}