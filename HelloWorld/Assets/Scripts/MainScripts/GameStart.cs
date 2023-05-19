using HybridCLR;
using System;
using System.Reflection;
using UnityEngine;
using Object = UnityEngine.Object;
using System.IO;

namespace MainAssembly
{
    public class GameStart : MonoBehaviour
    {
        private int loadId;

        private void Start()
        {
            Application.runInBackground = true;
            AssetManager.Instance.Init(HotUpdate);
        }
        private void HotUpdate()
        {
            HotUpdateCode.Instance.StartUpdate(LoadHotUpdateConfig);
        }
        private void LoadHotUpdateConfig()
        {
            AssetManager.Instance.Load<TextAsset>("HotUpdateConfig", LoadMetadataRes);
        }
        private void LoadMetadataRes(int id, Object asset)
        {
            AssetManager.Instance.Unload(id);
            TextAsset ta = asset as TextAsset;
            var config = JsonUtility.FromJson<HotUpdateConfig>(ta.text);
            string[] path = new string[config.Metadata.Count];
            for (int i = 0; i < path.Length; i++) path[i] = Path.GetFileNameWithoutExtension(config.Metadata[i]);
            loadId = AssetManager.Instance.Load(path, LoadMetadataForAOTAssembly);
        }
        private void LoadMetadataForAOTAssembly(string[] path, Object[] assets)
        {
            for (int i = 0; i < assets.Length; i++)
            {
                if (assets[i] != null)
                {
                    var ta = assets[i] as TextAsset;
                    RuntimeApi.LoadMetadataForAOTAssembly(ta.bytes, HomologousImageMode.SuperSet);
                }
            }
            AssetManager.Instance.Unload(loadId);
            StartHotAssembly();
        }
        private void StartHotAssembly()
        {
#if UNITY_EDITOR
            HotAssembly.GameStart.Instance.Init();
#else
            AssetManager.Instance.Load<TextAsset>("HotAssembly", StartHotAssembly);
#endif
        }
        private void StartHotAssembly(int id, Object asset)
        {
            AssetManager.Instance.Unload(id);
            TextAsset ta = asset as TextAsset;
            var hotAssembly = Assembly.Load(ta.bytes);
            Type t = hotAssembly.GetType("HotAssembly.GameStart");
            PropertyInfo p = t.BaseType.GetProperty("Instance");
            object o = p.GetMethod.Invoke(null, null);
            MethodInfo m = t.GetMethod("Init");
            m.Invoke(o, null);
        }
        private void Update()
        {
            AsyncManager.Instance.Update();
        }
    }
}