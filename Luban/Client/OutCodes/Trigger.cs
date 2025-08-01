
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
public sealed partial class Trigger : Luban.BeanBase
{
    public Trigger(ByteBuf _buf) 
    {
        ID = _buf.ReadInt();
        TriggerMode = (TriggerMode)_buf.ReadInt();
        Priority = _buf.ReadInt();
        Limit = _buf.ReadInt();
        Count1Type = (CommonHandleType1)_buf.ReadInt();
        Count1 = _buf.ReadInt();
        Count2Type = (CommonHandleType1)_buf.ReadInt();
        Count2 = _buf.ReadInt();
        TotalTime = _buf.ReadFloat();
        CDTime = _buf.ReadFloat();
        Condition = _buf.ReadString();
        {int n0 = _buf.ReadSize(); Action1 = new System.Collections.Generic.List<int>(n0);for(var i0 = 0 ; i0 < n0 ; i0++) { int _e0;  _e0 = _buf.ReadInt(); Action1.Add(_e0);}}
        {int n0 = _buf.ReadSize(); Action2 = new System.Collections.Generic.List<int>(n0);for(var i0 = 0 ; i0 < n0 ; i0++) { int _e0;  _e0 = _buf.ReadInt(); Action2.Add(_e0);}}
    }

    public static Trigger DeserializeTrigger(ByteBuf _buf)
    {
        return new Trigger(_buf);
    }

    public readonly int ID;
    /// <summary>
    /// 触发模式
    /// </summary>
    public readonly TriggerMode TriggerMode;
    /// <summary>
    /// 优先级
    /// </summary>
    public readonly int Priority;
    /// <summary>
    /// 数量限制
    /// </summary>
    public readonly int Limit;
    /// <summary>
    /// 达到次数处理方式
    /// </summary>
    public readonly CommonHandleType1 Count1Type;
    /// <summary>
    /// 可触发次数 正<br/>0无限次触发<br/>-1不触发
    /// </summary>
    public readonly int Count1;
    /// <summary>
    /// 达到次数处理方式
    /// </summary>
    public readonly CommonHandleType1 Count2Type;
    /// <summary>
    /// 可触发次数 反<br/>0无限次触发<br/>-1不触发
    /// </summary>
    public readonly int Count2;
    /// <summary>
    /// 生效时间
    /// </summary>
    public readonly float TotalTime;
    /// <summary>
    /// 冷却时间
    /// </summary>
    public readonly float CDTime;
    /// <summary>
    /// 条件组合
    /// </summary>
    public readonly string Condition;
    /// <summary>
    /// 正触发行为
    /// </summary>
    public readonly System.Collections.Generic.List<int> Action1;
    /// <summary>
    /// 反触发行为
    /// </summary>
    public readonly System.Collections.Generic.List<int> Action2;
   
    public const int __ID__ = 604761496;
    public override int GetTypeId() => __ID__;

    public override string ToString()
    {
        return "{ "
        + "ID:" + ID + ","
        + "TriggerMode:" + TriggerMode + ","
        + "Priority:" + Priority + ","
        + "Limit:" + Limit + ","
        + "Count1Type:" + Count1Type + ","
        + "Count1:" + Count1 + ","
        + "Count2Type:" + Count2Type + ","
        + "Count2:" + Count2 + ","
        + "TotalTime:" + TotalTime + ","
        + "CDTime:" + CDTime + ","
        + "Condition:" + Condition + ","
        + "Action1:" + Luban.StringUtil.CollectionToString(Action1) + ","
        + "Action2:" + Luban.StringUtil.CollectionToString(Action2) + ","
        + "}";
    }
}
}

