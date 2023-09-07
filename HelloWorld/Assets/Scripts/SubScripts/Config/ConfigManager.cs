using System;
using UnityEngine;
using Object = UnityEngine.Object;
using System.Reflection;
using cfg;

namespace HotAssembly
{
    public class ConfigManager : Singletion<ConfigManager>
    {
        private Tables gameConfigs;
        private int configCounter = 0;
        private Action finish;

        public Tables GameConfigs => gameConfigs;

        public void Init(Action finish)
        {
            if (gameConfigs == null) gameConfigs = new Tables();
            this.finish = finish;
            var fis = typeof(Tables).GetFields();
            configCounter = fis.Length;
            for (int i = 0; i < configCounter; i++) new ConfigItem().Load(fis[i], Finish);
        }
        private void Finish()
        {
            if (--configCounter == 0)
            {
                finish?.Invoke();
                AsyncManager.Instance.GCCollect();
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
                string tempPath = fi.Name.ToLower();
                tb = fi.GetValue(Instance.GameConfigs) as TbBase;
                AssetManager.Instance.Load<TextAsset>(ref loadId, tempPath, Deserialize);
            }
            private void Deserialize(int id, Object asset)
            {
                if (asset == null) return;
                byte[] bytes = ((TextAsset)asset).bytes;
                if (bytes == null) return;
                ThreadManager.Instance.StartThread(a => tb.Deserialize(bytes), Finish);
            }
            private void Finish()
            {
                tb = null;
                AssetManager.Instance.Unload(loadId);
                finish?.Invoke();
            }
        }
    }
}