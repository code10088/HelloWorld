using ProtoBuf;
using System;
using System.IO;

namespace HotAssembly
{
    public partial class NetMsgDispatch
    {
        public bool Deserialize(byte[] bytes)
        {
            try
            {
                var id = BitConverter.ToUInt16(bytes, 0);
                //protobuf-net/net462/System.Memory.dll和unity内置mscorlib.dll的Memory版本不一致
                //无法添加对protobuf-net/net462/System.Memory.dll的引用
                //var mm = new Memory<byte>(bytes, 2, bytes.Length - 2);
                IExtensible msg = null;
                var mm = new MemoryStream(bytes, 2, bytes.Length - 2);
                switch ((NetMsgId)id)
                {
                    case NetMsgId.Person: msg = Serializer.Deserialize<ProtoTest.Person>(mm); break;
                }
                mm.Dispose();
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
}
