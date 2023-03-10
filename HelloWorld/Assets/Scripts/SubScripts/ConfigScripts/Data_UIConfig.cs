using System;
using UnityEngine;
public class Data_UIConfigArray : BytesDecodeInterface
{
    public Data_UIConfig[] array;
    public void Deserialize(BytesDecode bd)
    {
        array = bd.ToBDIArray(() => new Data_UIConfig());
    }
    public void Serialize(BytesDecode bd)
    {
        bd.ToBytes(array);
    }
}
#if UNITY_EDITOR
[Serializable]
#endif
public class Data_UIConfig : BytesDecodeInterface
{
    public int ID;
    public string name;
    public string prefabName;
    public UILayer UILayer;
    public void Deserialize(BytesDecode bd)
    {
        ID = bd.ToInt();
        name = bd.ToStr();
        prefabName = bd.ToStr();
        UILayer = (UILayer)bd.ToInt();
    }
    public void Serialize(BytesDecode bd)
    {
        bd.ToBytes(ID);
        bd.ToBytes(name);
        bd.ToBytes(prefabName);
        bd.ToBytes((int)UILayer);
    }
}
public enum UILayer
{
    Normal,
    Top,
}
