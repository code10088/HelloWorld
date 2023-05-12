using System;
using UnityEngine;
using Object = UnityEngine.Object;
using System.Reflection;

namespace HotAssembly
{
    public class ConfigManager : Singletion<ConfigManager>
    {
        private GameConfigs gameConfigs;
        private int configCounter = 0;
        private Action finish;

        public GameConfigs GameConfigs => gameConfigs;

        public void Init(Action finish)
        {
            if (gameConfigs == null) gameConfigs = new GameConfigs();
            this.finish = finish;
            var fis = typeof(GameConfigs).GetFields();
            configCounter = fis.Length;
            for (int i = 0; i < configCounter; i++) new ConfigItem().Load(fis[i], Finish);
        }
        private void Finish()
        {
            if (--configCounter == 0)
            {
                finish?.Invoke();
                GC.Collect();
            }
        }
        public void InitSpecial(string name, Action finish)
        {
            if (gameConfigs == null) gameConfigs = new GameConfigs();
            var fi = typeof(GameConfigs).GetField(name);
            new ConfigItem().Load(fi, finish);
        }

        class ConfigItem
        {
            private Action finish;
            private BytesDecodeInterface bdi;
            private int loadId;
            public void Load(FieldInfo fi, Action _finish)
            {
                finish = _finish;
                string tempPath = fi.Name;
                bdi = fi.GetValue(Instance.GameConfigs) as BytesDecodeInterface;
                loadId = AssetManager.Instance.Load<TextAsset>(tempPath, Deserialize);
            }
            private void Deserialize(int id, Object asset)
            {
                if (asset == null) return;
                byte[] bytes = ((TextAsset)asset).bytes;
                if (bytes == null) return;
                BytesDecode.Deserialize(bdi, bytes, Finish);
            }
            private void Finish()
            {
                bdi = null;
                AssetManager.Instance.Unload(loadId);
                finish?.Invoke();
            }
        }
    }
}