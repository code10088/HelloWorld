
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
public partial class TbSimpleFightConfig : TbBase
{
    private readonly System.Collections.Generic.Dictionary<int, SimpleFightConfig> _dataMap = new System.Collections.Generic.Dictionary<int, SimpleFightConfig>();
    private readonly System.Collections.Generic.List<SimpleFightConfig> _dataList = new System.Collections.Generic.List<SimpleFightConfig>();
    
    public void Deserialize(byte[] bytes)
    {
        _dataMap.Clear();
        _dataList.Clear();
        ByteBuf _buf = new ByteBuf(bytes);
        int n = _buf.ReadSize();
        for(int i = n ; i > 0 ; --i)
        {
            SimpleFightConfig _v;
            _v = global::cfg.SimpleFightConfig.DeserializeSimpleFightConfig(_buf);
            _dataList.Add(_v);
            _dataMap.Add(_v.ID, _v);
        }
    }

    public System.Collections.Generic.Dictionary<int, SimpleFightConfig> DataMap => _dataMap;
    public System.Collections.Generic.List<SimpleFightConfig> DataList => _dataList;

    public SimpleFightConfig GetOrDefault(int key) => _dataMap.TryGetValue(key, out var v) ? v : null;
    public SimpleFightConfig Get(int key) => _dataMap[key];
    public SimpleFightConfig this[int key] => _dataMap[key];


}

}

