using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using xasset;

public class ConfigManager : Singletion<ConfigManager>
{
    private Data_TestArray data_Test = new Data_TestArray();
    public void Deserialize()
    {
        var ar = Asset.LoadAsync("Data_Test", typeof(TextAsset));
        ar.completed += a =>
        {
            var ta = ar.asset as TextAsset;
            byte[] bytes = ta.bytes;
            BytesDecode.Deserialize(data_Test, bytes);
        };
    }
}
