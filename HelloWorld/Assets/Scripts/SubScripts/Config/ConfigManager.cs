using System;
using UnityEngine;
using MainAssembly;
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

        class ConfigItem
        {
            private BytesDecodeInterface bdi;
            private int loadId;
            public void Load(FieldInfo fi)
            {
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
                Instance.Finish();
            }
        }
        public void Init(Action finish)
        {
            gameConfigs = new GameConfigs();
            this.finish = finish;
            var fis = typeof(GameConfigs).GetFields();
            configCounter = fis.Length;
            for (int i = 0; i < configCounter; i++) new ConfigItem().Load(fis[i]);
        }
        private void Finish()
        {
            if (--configCounter == 0)
            {
                finish?.Invoke();
                GC.Collect();
            }
        }
    }
}