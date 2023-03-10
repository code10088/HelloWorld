using System;
using UnityEngine;
public class Data_TestArray : BytesDecodeInterface
{
    public Data_Test[] array;
    public void Deserialize(BytesDecode bd)
    {
        array = bd.ToBDIArray(() => new Data_Test());
    }
    public void Serialize(BytesDecode bd)
    {
        bd.ToBytes(array);
    }
}
#if UNITY_EDITOR
[Serializable]
#endif
public class Data_Test : BytesDecodeInterface
{
    public int ID;
    public string Name;
    public bool Old;
    public float Time1;
    public short Time2;
    public byte Data;
    public Mgb nMgb;
    public Vector3 Data2;
    public Vector3[] Data3;
    public int[] Data4;
    public void Deserialize(BytesDecode bd)
    {
        ID = bd.ToInt();
        Name = bd.ToStr();
        Old = bd.ToBool();
        Time1 = bd.ToFloat();
        Time2 = bd.ToShort();
        Data = bd.ToByte();
        nMgb = (Mgb)bd.ToInt();
        Data2 = bd.ToVector3();
        Data3 = bd.ToVector3Array();
        Data4 = bd.ToIntArray();
    }
    public void Serialize(BytesDecode bd)
    {
        bd.ToBytes(ID);
        bd.ToBytes(Name);
        bd.ToBytes(Old);
        bd.ToBytes(Time1);
        bd.ToBytes(Time2);
        bd.ToBytes(Data);
        bd.ToBytes((int)nMgb);
        bd.ToBytes(Data2);
        bd.ToBytes(Data3);
        bd.ToBytes(Data4);
    }
}
public enum Mgb
{
    aaa,
    bbb,
    ccc,
}
