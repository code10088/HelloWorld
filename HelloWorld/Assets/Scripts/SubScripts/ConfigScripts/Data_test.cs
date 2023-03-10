using UnityEngine;
public class Data_testArray : BytesDecodeInterface
{
    public Data_test[] array;
    public void Deserialize(BytesDecode bd)
    {
        array = bd.ToBDIArray(() => new Data_test());
    }
    public void Serialize(BytesDecode bd)
    {
        bd.ToBytes(array);
    }
}
public class Data_test : BytesDecodeInterface
{
    public int ID;
    public string Name;
    public bool Old;
    public float Time1;
    public short Time2;
    public byte Data;
    public Mgb mMgb;
    public Vector3 CmdV;
    public Vector3[] Cmd2;
    public int[] Cmd;
    public void Deserialize(BytesDecode bd)
    {
        ID = bd.ToInt();
        Name = bd.ToStr();
        Old = bd.ToBool();
        Time1 = bd.ToFloat();
        Time2 = bd.ToShort();
        Data = bd.ToByte();
        mMgb = (Mgb)bd.ToInt();
        CmdV = bd.ToVector3();
        Cmd2 = bd.ToVector3Array();
        Cmd = bd.ToIntArray();
    }
    public void Serialize(BytesDecode bd)
    {
        bd.ToBytes(ID);
        bd.ToBytes(Name);
        bd.ToBytes(Old);
        bd.ToBytes(Time1);
        bd.ToBytes(Time2);
        bd.ToBytes(Data);
        bd.ToBytes((int)mMgb);
        bd.ToBytes(CmdV);
        bd.ToBytes(Cmd2);
        bd.ToBytes(Cmd);
    }
}
public enum Mgb
{
    aaa,
    bbb,
    ccc,
}
