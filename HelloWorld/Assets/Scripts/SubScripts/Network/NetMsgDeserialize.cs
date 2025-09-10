using ProtoBuf;
using System;
public class NetMsgId
{
    public const ushort CSHeart = 0;
    public const ushort ProtoTest_Person = 1;
    public const ushort SCHeart = 10000;
}
public partial class NetMsgDispatch
{
    private bool Deserialize(ushort id, Memory<byte> memory)
    {
        try
        {
            IExtensible msg = null;
            switch (id)
            {
                case NetMsgId.SCHeart: msg = Serializer.Deserialize<SCHeart>(memory); break;
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
