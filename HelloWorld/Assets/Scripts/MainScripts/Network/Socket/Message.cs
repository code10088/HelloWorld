using ProtoBuf;

public class NetMsgId
{
    public const ushort KcpConnect = 10000;
    public const ushort Heart = 10001;
}
[ProtoContract]
public class CS_Heart : IExtensible
{
    private IExtension __pbn__extensionData;
    IExtension IExtensible.GetExtensionObject(bool createIfMissing) => Extensible.GetExtensionObject(ref __pbn__extensionData, createIfMissing);
}
[ProtoContract]
public class CS_KcpConnect : IExtensible
{
    private IExtension __pbn__extensionData;
    IExtension IExtensible.GetExtensionObject(bool createIfMissing) => Extensible.GetExtensionObject(ref __pbn__extensionData, createIfMissing);
    [ProtoMember(1)]
    public uint playerId { get; set; }
    [ProtoMember(2, Name = @"token")]
    [System.ComponentModel.DefaultValue("")]
    public string Token { get; set; } = "";
}