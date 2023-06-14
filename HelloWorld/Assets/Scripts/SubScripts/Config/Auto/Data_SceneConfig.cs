using System;
using UnityEngine;
public class Data_SceneConfigArray : BytesDecodeInterface
{
#if UNITY_EDITOR
[SerializeField]
#endif
    private Data_SceneConfig[] _array;
    public Data_SceneConfig[] array { get => _array; }
    public Data_SceneConfig GetDataByID(int id)
    {
        for (int i = 0; i < _array.Length; i++)
        {
            if (_array[i].ID == id) return _array[i];
        }
        return null;
    }
    public void Deserialize(BytesDecode bd)
    {
        _array = bd.ToBDIArray(() => new Data_SceneConfig());
    }
    public void Serialize(BytesDecode bd)
    {
        bd.ToBytes(_array);
    }
}
#if UNITY_EDITOR
[Serializable]
#endif
public class Data_SceneConfig : BytesDecodeInterface
{
#if UNITY_EDITOR
[SerializeField]
#endif
    private int _ID;
#if UNITY_EDITOR
[SerializeField]
#endif
    private SceneType _type;
#if UNITY_EDITOR
[SerializeField]
#endif
    private string _name;
#if UNITY_EDITOR
[SerializeField]
#endif
    private string _prefabName;
    public int ID { get => _ID; }
    public SceneType type { get => _type; }
    public string name { get => _name; }
    public string prefabName { get => _prefabName; }
    public void Deserialize(BytesDecode bd)
    {
        _ID = bd.ToInt();
        _type = (SceneType)bd.ToInt();
        _name = bd.ToStr();
        _prefabName = bd.ToStr();
    }
    public void Serialize(BytesDecode bd)
    {
        bd.ToBytes(_ID);
        bd.ToBytes((int)_type);
        bd.ToBytes(_name);
        bd.ToBytes(_prefabName);
    }
}
public enum SceneType
{
    SceneBase,
    TestScene,
}
