using System;
using UnityEngine;
using MainAssembly;
using Object = UnityEngine.Object;

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
            gameConfigs = new GameConfigs();
            this.finish = finish;
            var fis = typeof(GameConfigs).GetFields();
            configCounter = fis.Length;
            for (int i = 0; i < configCounter; i++)
            {
                string tempPath = fis[i].Name;
                var v = fis[i].GetValue(gameConfigs);
                AssetManager.Instance.Load<TextAsset>(tempPath, Deserialize, v);
            }
        }
        private void Deserialize(int id, Object obj, object param)
        {
            TextAsset ta = obj as TextAsset;
            BytesDecode.Deserialize((BytesDecodeInterface)param, ta.bytes, Finish, id);
        }
        private void Finish(object param)
        {
            AssetManager.Instance.Unload((int)param);
            if (--configCounter == 0)
            {
                finish?.Invoke();
                GC.Collect();
            }
        }
    }
}