using System;
using UnityEngine;
public class ConfigManager : Singletion<ConfigManager>
{
    private GameConfigs gameConfigs;
    private int configCounter = 0;
    private Action finish;

    public GameConfigs GameConfigs => gameConfigs;

    public void InitConfig(Action finish)
    {
        gameConfigs = new GameConfigs();
        this.finish = finish;
        var fis = typeof(GameConfigs).GetFields();
        configCounter = fis.Length;
        for (int i = 0; i < configCounter; i++)
        {
            string tempPath = fis[i].Name;
            var v = fis[i].GetValue(gameConfigs);
            int a = AssetManager.Instance.Load<TextAsset>(tempPath, Deserialize, v);
        }
    }
    private void Deserialize(int id, dynamic obj, dynamic param)
    {
        TextAsset ta = obj as TextAsset;
        BytesDecode.Deserialize((BytesDecodeInterface)param, ta.bytes, Finish, id);
    }
    private void Finish(dynamic param)
    {
        AssetManager.Instance.Unload(param);
        if (--configCounter == 0)
        {
            finish?.Invoke();
            GC.Collect();
        }
    }
}
