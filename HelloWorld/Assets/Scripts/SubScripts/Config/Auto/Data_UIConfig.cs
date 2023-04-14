using System;
using UnityEngine;
public class Data_UIConfigArray : BytesDecodeInterface
{
#if UNITY_EDITOR
[SerializeField]
#endif
    private Data_UIConfig[] _array;
    public Data_UIConfig[] array { get => _array; }
    public Data_UIConfig GetDataByID(int id)
    {
        for (int i = 0; i < _array.Length; i++)
        {
            if (_array[i].ID == id) return _array[i];
        }
        return null;
    }
    public void Deserialize(BytesDecode bd)
    {
        _array = bd.ToBDIArray(() => new Data_UIConfig());
    }
    public void Serialize(BytesDecode bd)
    {
        bd.ToBytes(_array);
    }
}
#if UNITY_EDITOR
[Serializable]
#endif
public class Data_UIConfig : BytesDecodeInterface
{
#if UNITY_EDITOR
[SerializeField]
#endif
    private int _ID;
#if UNITY_EDITOR
[SerializeField]
#endif
    private string _name;
#if UNITY_EDITOR
[SerializeField]
#endif
    private string _prefabName;
    public int ID { get => _ID; }
    public string name { get => _name; }
    public string prefabName { get => _prefabName; }
    public void Deserialize(BytesDecode bd)
    {
        _ID = bd.ToInt();
        _name = bd.ToStr();
        _prefabName = bd.ToStr();
    }
    public void Serialize(BytesDecode bd)
    {
        bd.ToBytes(_ID);
        bd.ToBytes(_name);
        bd.ToBytes(_prefabName);
    }
}
