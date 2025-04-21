using ProtoBuf;
using System;
public class NetMsgId
{
    public const ushort ProtoTest_Person = 10001;
}
public partial class NetMsgDispatch
{
    public bool Deserialize(byte[] bytes)
    {
        try
        {
            var id = BitConverter.ToUInt16(bytes, 0);
            var mm = new Memory<byte>(bytes, 2, bytes.Length - 2);
            IExtensible msg = null;
            switch (id)
            {
                case NetMsgId.ProtoTest_Person: msg = Serializer.Deserialize<ProtoTest.Person>(mm); break;
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
