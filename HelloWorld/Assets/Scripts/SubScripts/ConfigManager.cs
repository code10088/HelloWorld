using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Object = UnityEngine.Object;

public class ConfigManager : Singletion<ConfigManager>
{
    private GameConfigs gameConfigs;
    private int configCounter = 0;
    private Action finish;
    private void Load(string path, BytesDecodeInterface bdi)
    {
        AssetManager.Instance.Load<TextAsset>(path, Deserialize, bdi);
    }
    private void Deserialize(int id, dynamic obj, dynamic param)
    {
        TextAsset ta = obj as TextAsset;
        BytesDecode.Deserialize((BytesDecodeInterface)param, ta.bytes, Finish);
        AssetManager.Instance.Unload(id);
    }
    private void Finish()
    {
        configCounter--;
        if (configCounter == 0)
        {
            finish?.Invoke();
            GC.Collect();
        }
    }

    public void InitConfig(Action finish)
    {
        this.finish = finish;
        //∑¥…‰»°gameConfigs÷–◊÷∂Œ
        PropertyInfo[] pis = typeof(GameConfigs).GetProperties();
        
        for (int i = 0; i < pis.Length; i++)
        {
            Load("Data_Test", (BytesDecodeInterface)pis[i].GetValue(gameConfigs));
        }
    }
}
