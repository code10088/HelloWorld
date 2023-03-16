using System;
using UnityEngine;
public class Data_TestArray : BytesDecodeInterface
{
#if UNITY_EDITOR
[SerializeField]
#endif
    private Data_Test[] _array;
    public Data_Test[] array { get => _array; }
    public Data_Test GetDataByID(int id)
    {
        for (int i = 0; i < _array.Length; i++)
        {
            if (_array[i].ID == id) return _array[i];
        }
        return null;
    }
    public void Deserialize(BytesDecode bd)
    {
        _array = bd.ToBDIArray(() => new Data_Test());
    }
    public void Serialize(BytesDecode bd)
    {
        bd.ToBytes(_array);
    }
}
#if UNITY_EDITOR
[Serializable]
#endif
public class Data_Test : BytesDecodeInterface
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
    private bool _Old;
#if UNITY_EDITOR
[SerializeField]
#endif
    private float _Time1;
#if UNITY_EDITOR
[SerializeField]
#endif
    private short _Time2;
#if UNITY_EDITOR
[SerializeField]
#endif
    private byte _Data;
#if UNITY_EDITOR
[SerializeField]
#endif
    private Mgb _nMgb;
#if UNITY_EDITOR
[SerializeField]
#endif
    private Vector3 _Data2;
#if UNITY_EDITOR
[SerializeField]
#endif
    private Vector3[] _Data3;
#if UNITY_EDITOR
[SerializeField]
#endif
    private int[] _Data4;
    public int ID { get => _ID; }
    public string Name { get => _Name; }
    public bool Old { get => _Old; }
    public float Time1 { get => _Time1; }
    public short Time2 { get => _Time2; }
    public byte Data { get => _Data; }
    public Mgb nMgb { get => _nMgb; }
    public Vector3 Data2 { get => _Data2; }
    public Vector3[] Data3 { get => _Data3; }
    public int[] Data4 { get => _Data4; }
    public void Deserialize(BytesDecode bd)
    {
        _ID = bd.ToInt();
        _Name = bd.ToStr();
        _Old = bd.ToBool();
        _Time1 = bd.ToFloat();
        _Time2 = bd.ToShort();
        _Data = bd.ToByte();
        _nMgb = (Mgb)bd.ToInt();
        _Data2 = bd.ToVector3();
        _Data3 = bd.ToVector3Array();
        _Data4 = bd.ToIntArray();
    }
    public void Serialize(BytesDecode bd)
    {
        bd.ToBytes(_ID);
        bd.ToBytes(_Name);
        bd.ToBytes(_Old);
        bd.ToBytes(_Time1);
        bd.ToBytes(_Time2);
        bd.ToBytes(_Data);
        bd.ToBytes((int)_nMgb);
        bd.ToBytes(_Data2);
        bd.ToBytes(_Data3);
        bd.ToBytes(_Data4);
    }
}
public enum Mgb
{
    aaa,
    bbb,
    ccc,
}
