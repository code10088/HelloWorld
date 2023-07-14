using ProtoBuf;
using System;

namespace HotAssembly
{
    public partial class NetMsgDispatch
    {
        public void Deserialize(byte[] bytes)
        {
            var id = BitConverter.ToUInt16(bytes, 0);
            var mm = new Memory<byte>(bytes, 2, bytes.Length - 2);
            IExtensible msg = null;
            switch (id)
            {
                case NetMsgId.Min: msg = Serializer.Deserialize<ProtoTest.Person>(mm); break;
            }
            var item = new NetMsgItem();
            item.id = id;
            item.msg = msg;
            global::NetMsgDispatch.Instance.Add(item);
        }
    }
}
