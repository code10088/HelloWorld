using System;
using UnityEngine;
using MainAssembly;

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
        private void Deserialize(int id, dynamic obj, dynamic param)
        {
            BytesDecode.Deserialize((BytesDecodeInterface)param, (byte[])obj.bytes, Finish, id);
        }
        private void Finish(dynamic param)
        {
            GameDebug.Log(param);
            AssetManager.Instance.Unload(param);
            if (--configCounter == 0)
            {
                MainAssembly.ConfigManager.Instance.GetConfigAction = GetConfig;
                MainAssembly.ConfigManager.Instance.GetUIConfigAction = GetUIConfig;
                finish?.Invoke();
                GC.Collect();
            }
        }
        private dynamic GetConfig(string name, int id)
        {
            var fis = typeof(GameConfigs).GetFields();
            configCounter = fis.Length;
            for (int i = 0; i < configCounter; i++)
            {
                string temp = fis[i].Name;
                if (temp == name) return fis[i].GetValue(gameConfigs);
            }
            return null;
        }
        private dynamic GetUIConfig(int id)
        {
            return gameConfigs.Data_UIConfig.GetDataByID(id);
        }
    }
}