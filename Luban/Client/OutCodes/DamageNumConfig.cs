
//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

using Luban;


namespace cfg
{
public sealed partial class DamageNumConfig : Luban.BeanBase
{
    public DamageNumConfig(ByteBuf _buf) 
    {
        DamageNumType = (DamageNumType)_buf.ReadInt();
        PrefabPath = _buf.ReadString();
    }

    public static DamageNumConfig DeserializeDamageNumConfig(ByteBuf _buf)
    {
        return new DamageNumConfig(_buf);
    }

    public readonly DamageNumType DamageNumType;
    /// <summary>
    /// 资源名字
    /// </summary>
    public readonly string PrefabPath;
   
    public const int __ID__ = -640182375;
    public override int GetTypeId() => __ID__;

    public override string ToString()
    {
        return "{ "
        + "DamageNumType:" + DamageNumType + ","
        + "PrefabPath:" + PrefabPath + ","
        + "}";
    }
}
}

