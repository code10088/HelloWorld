
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
public sealed partial class ConditionConfig : Luban.BeanBase
{
    public ConditionConfig(ByteBuf _buf) 
    {
        ID = _buf.ReadInt();
        ConditionType = (ConditionType)_buf.ReadInt();
        {int n0 = _buf.ReadSize(); IntParam = new System.Collections.Generic.List<int>(n0);for(var i0 = 0 ; i0 < n0 ; i0++) { int _e0;  _e0 = _buf.ReadInt(); IntParam.Add(_e0);}}
        {int n0 = _buf.ReadSize(); FloatParam = new System.Collections.Generic.List<float>(n0);for(var i0 = 0 ; i0 < n0 ; i0++) { float _e0;  _e0 = _buf.ReadFloat(); FloatParam.Add(_e0);}}
        {int n0 = _buf.ReadSize(); StrParam = new System.Collections.Generic.List<string>(n0);for(var i0 = 0 ; i0 < n0 ; i0++) { string _e0;  _e0 = _buf.ReadString(); StrParam.Add(_e0);}}
    }

    public static ConditionConfig DeserializeConditionConfig(ByteBuf _buf)
    {
        return new ConditionConfig(_buf);
    }

    /// <summary>
    /// 条件组合()!&amp;|0 <br/>最外层必须为括号<br/>非括号内的执行优先级从后往前
    /// </summary>
    public readonly int ID;
    /// <summary>
    /// 触发器类型
    /// </summary>
    public readonly ConditionType ConditionType;
    /// <summary>
    /// 触发条件参数
    /// </summary>
    public readonly System.Collections.Generic.List<int> IntParam;
    /// <summary>
    /// 触发条件参数
    /// </summary>
    public readonly System.Collections.Generic.List<float> FloatParam;
    /// <summary>
    /// 触发条件参数
    /// </summary>
    public readonly System.Collections.Generic.List<string> StrParam;
   
    public const int __ID__ = -1029499875;
    public override int GetTypeId() => __ID__;

    public override string ToString()
    {
        return "{ "
        + "ID:" + ID + ","
        + "ConditionType:" + ConditionType + ","
        + "IntParam:" + Luban.StringUtil.CollectionToString(IntParam) + ","
        + "FloatParam:" + Luban.StringUtil.CollectionToString(FloatParam) + ","
        + "StrParam:" + Luban.StringUtil.CollectionToString(StrParam) + ","
        + "}";
    }
}
}

