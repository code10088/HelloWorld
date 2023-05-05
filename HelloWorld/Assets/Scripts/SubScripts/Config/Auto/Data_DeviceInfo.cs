using System;
using UnityEngine;
public class Data_DeviceInfoArray : BytesDecodeInterface
{
#if UNITY_EDITOR
[SerializeField]
#endif
    private Data_DeviceInfo[] _array;
    public Data_DeviceInfo[] array { get => _array; }
    public Data_DeviceInfo GetDataByID(int id)
    {
        for (int i = 0; i < _array.Length; i++)
        {
            if (_array[i].ID == id) return _array[i];
        }
        return null;
    }
    public void Deserialize(BytesDecode bd)
    {
        _array = bd.ToBDIArray(() => new Data_DeviceInfo());
    }
    public void Serialize(BytesDecode bd)
    {
        bd.ToBytes(_array);
    }
}
#if UNITY_EDITOR
[Serializable]
#endif
public class Data_DeviceInfo : BytesDecodeInterface
{
#if UNITY_EDITOR
[SerializeField]
#endif
    private int _ID;
#if UNITY_EDITOR
[SerializeField]
#endif
    private string _Name;
#if UNITY_EDITOR
[SerializeField]
#endif
    private int _Lv;
#if UNITY_EDITOR
[SerializeField]
#endif
    private bool _SupportES3;
    public int ID { get => _ID; }
    public string Name { get => _Name; }
    public int Lv { get => _Lv; }
    public bool SupportES3 { get => _SupportES3; }
    public void Deserialize(BytesDecode bd)
    {
        _ID = bd.ToInt();
        _Name = bd.ToStr();
        _Lv = bd.ToInt();
        _SupportES3 = bd.ToBool();
    }
    public void Serialize(BytesDecode bd)
    {
        bd.ToBytes(_ID);
        bd.ToBytes(_Name);
        bd.ToBytes(_Lv);
        bd.ToBytes(_SupportES3);
    }
}
