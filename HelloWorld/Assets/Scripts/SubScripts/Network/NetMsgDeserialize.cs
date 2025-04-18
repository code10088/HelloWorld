﻿using ProtoBuf;
using System;

public partial class NetMsgDispatch
{
    public bool Deserialize(byte[] bytes)
    {
        try
        {
            var id = BitConverter.ToUInt16(bytes, 0);
            var mm = new Memory<byte>(bytes, 2, bytes.Length - 2);
            IExtensible msg = null;
            switch ((MessageType)id)
            {
                case MessageType.Person: msg = Serializer.Deserialize<ProtoTest.Person>(mm); break;
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