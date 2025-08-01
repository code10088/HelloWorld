
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
public partial class TbSceneConfig : TbBase
{
    private readonly System.Collections.Generic.Dictionary<SceneType, SceneConfig> _dataMap = new System.Collections.Generic.Dictionary<SceneType, SceneConfig>();
    private readonly System.Collections.Generic.List<SceneConfig> _dataList = new System.Collections.Generic.List<SceneConfig>();
    
    public void Deserialize(byte[] bytes)
    {
        _dataMap.Clear();
        _dataList.Clear();
        ByteBuf _buf = new ByteBuf(bytes);
        int n = _buf.ReadSize();
        for(int i = n ; i > 0 ; --i)
        {
            SceneConfig _v;
            _v = global::cfg.SceneConfig.DeserializeSceneConfig(_buf);
            _dataList.Add(_v);
            _dataMap.Add(_v.SceneType, _v);
        }
    }

    public System.Collections.Generic.Dictionary<SceneType, SceneConfig> DataMap => _dataMap;
    public System.Collections.Generic.List<SceneConfig> DataList => _dataList;

    public SceneConfig GetOrDefault(SceneType key) => _dataMap.TryGetValue(key, out var v) ? v : null;
    public SceneConfig Get(SceneType key) => _dataMap[key];
    public SceneConfig this[SceneType key] => _dataMap[key];


}

}

