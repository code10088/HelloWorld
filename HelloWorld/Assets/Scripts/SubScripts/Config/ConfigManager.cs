using System;
using UnityEngine;
using Object = UnityEngine.Object;
using System.Reflection;
using cfg;

public class ConfigManager : Singletion<ConfigManager>
{
    private Tables gameConfigs;
    private ConstConfig constConfig;
    private int count = 0;
    private int total = 0;
    private Action finish;

    public Tables GameConfigs => gameConfigs;
    public ConstConfig ConstConfig => constConfig;

    public void Init(Action finish)
    {
        if (gameConfigs == null) gameConfigs = new Tables();
        this.finish = finish;
        var fis = typeof(Tables).GetFields();
        total = fis.Length - (int)LanguageType.Max;
        for (int i = 0; i < fis.Length; i++)
        {
            if (fis[i].Name.StartsWith("Language")) continue;
            new ConfigItem().Load(fis[i], Finish);
        }
    }
    private void Finish()
    {
        if (++count == total)
        {
            finish?.Invoke();
            constConfig = gameConfigs.TbConstConfig[0];
            Driver.Instance.GCCollect();
        }
        else
        {
            string str = "Loading Config";
            float progress = (float)count / total;
            EventManager.Instance.FireEvent(EventType.SetSceneLoadingProgress, str, progress);
        }
    }
    public void InitSpecial(string name, Action finish)
    {
        if (gameConfigs == null) gameConfigs = new Tables();
        var fi = typeof(Tables).GetField(name);
        new ConfigItem().Load(fi, finish);
    }

    class ConfigItem
    {
        private Action finish;
        private TbBase tb;
        private int loadId;
        public void Load(FieldInfo fi, Action _finish)
        {
            finish = _finish;
            tb = fi.GetValue(Instance.GameConfigs) as TbBase;
            string tempPath = $"{ZResConst.ResDataConfigPath}{fi.Name.ToLower()}.bytes";
            AssetManager.Instance.Load<TextAsset>(ref loadId, tempPath, Deserialize);
        }
        private void Deserialize(int id, Object asset)
        {
            if (asset == null) return;
            byte[] bytes = ((TextAsset)asset).bytes;
            if (bytes == null) return;
            Driver.Instance.StartTask(a => tb.Deserialize(bytes), Finish);
        }
        private void Finish()
        {
            tb = null;
            AssetManager.Instance.Unload(ref loadId);
            finish?.Invoke();
        }
    }
}