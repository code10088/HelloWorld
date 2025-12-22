using cfg;
using Luban;
using System;
using System.Reflection;
using UnityEngine;
using Object = UnityEngine.Object;

public partial class ConfigManager : Singletion<ConfigManager>
{
    private FieldInfo[] fis;
    private int[] loaders;
    private int count = 0;
    private int total = 0;
    private Action finish;

    public void Init(Action finish)
    {
        this.finish = finish;
        fis = typeof(ConfigManager).GetFields(BindingFlags.NonPublic | BindingFlags.Instance);
        loaders = new int[fis.Length];
        for (int i = 0; i < fis.Length; i++)
        {
            if (fis[i].FieldType.Namespace != "cfg") continue;
            if (fis[i].Name.StartsWith("tblanguage")) continue;

            total++;
            string tempPath = $"{ZResConst.ResDataConfigPath}{fis[i].Name}.bytes";
            AssetManager.Instance.Load<TextAsset>(ref loaders[i], tempPath, Deserialize);
        }
    }
    private void Deserialize(int loadId, Object asset)
    {
        byte[] bytes = ((TextAsset)asset).bytes;
        int index = Array.IndexOf(loaders, loadId);
        var fi = fis[index];
        var byteBuf = new ByteBuf(bytes);
        Driver.Instance.StartTask(a => fi.SetValue(Instance, Activator.CreateInstance(fi.FieldType, byteBuf)), Finish);
        AssetManager.Instance.Unload(ref loadId);
    }
    private void Finish()
    {
        if (++count == total)
        {
            finish?.Invoke();
            finish = null;
            fis = null;
            loaders = null;
            Driver.Instance.GCCollect();
        }
        else
        {
            string str = "Loading Config";
            float progress = (float)count / total;
            EventManager.Instance.FireEvent(EventType.SetSceneLoadingProgress, str, progress);
        }
    }
    public void InitUIConfig(Action finish)
    {
        int loadId = -1;
        string tempPath = $"{ZResConst.ResDataConfigPath}tbuiconfig.bytes";
        AssetManager.Instance.Load<TextAsset>(ref loadId, tempPath, (a, b) =>
        {
            var bytes = ((TextAsset)b).bytes;
            var byteBuf = new ByteBuf(bytes);
            Driver.Instance.StartTask(a => tbuiconfig = new TbUIConfig(byteBuf), finish);
            AssetManager.Instance.Unload(ref loadId);
        });
    }
}