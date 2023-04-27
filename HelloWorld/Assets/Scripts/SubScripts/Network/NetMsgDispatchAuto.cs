using ProtoBuf;
using System.IO;

namespace HotAssembly
{
    public partial class NetMsgDispatch
    {
        public void Deserialize(ushort id, MemoryStream ms)
        {
            IExtensible msg = null;
            switch ((NetMsgId)id)
            {
                case NetMsgId.Test: msg = Serializer.Deserialize<ProtoTest.Person>(ms); break;
            }
            var item = new NetMsgItem();
            item.id = (NetMsgId)id;
            item.msg = msg;
            lock (msgPool) msgPool.Enqueue(item);
        }
    }
}
