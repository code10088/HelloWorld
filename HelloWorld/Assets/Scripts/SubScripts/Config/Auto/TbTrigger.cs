
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
public partial class TbTrigger : TbBase
{
    private readonly System.Collections.Generic.Dictionary<int, Trigger> _dataMap = new System.Collections.Generic.Dictionary<int, Trigger>();
    private readonly System.Collections.Generic.List<Trigger> _dataList = new System.Collections.Generic.List<Trigger>();
    
    public void Deserialize(byte[] bytes)
    {
		_dataMap.Clear();
		_dataList.Clear();
        ByteBuf _buf = new ByteBuf(bytes);
        for(int n = _buf.ReadSize() ; n > 0 ; --n)
        {
            Trigger _v;
            _v = Trigger.DeserializeTrigger(_buf);
            _dataList.Add(_v);
            _dataMap.Add(_v.ID, _v);
        }
    }

    public System.Collections.Generic.Dictionary<int, Trigger> DataMap => _dataMap;
    public System.Collections.Generic.List<Trigger> DataList => _dataList;

    public Trigger GetOrDefault(int key) => _dataMap.TryGetValue(key, out var v) ? v : null;
    public Trigger Get(int key) => _dataMap[key];
    public Trigger this[int key] => _dataMap[key];


}

}

