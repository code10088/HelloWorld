using ProtoBuf;

public class NetMsgId
{
    public const ushort CSKcpConnect = 0;
    public const ushort CSHeart = 1;
    public const ushort SCKcpConnect = 10000;
    public const ushort SCHeart = 10001;
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
[ProtoContract]
public class CS_Heart : IExtensible
{
    private IExtension __pbn__extensionData;
    IExtension IExtensible.GetExtensionObject(bool createIfMissing) => Extensible.GetExtensionObject(ref __pbn__extensionData, createIfMissing);
}