using System;
using System.Reflection;
using UnityEngine;
using Object = UnityEngine.Object;
using Newtonsoft.Json;
using HybridCLR;

namespace MainAssembly
{
    public class GameStart : MonoBehaviour
    {
        private int loadId;

        private void Start()
        {
            Application.runInBackground = true;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            AssetManager.Instance.Init(OpenDebug);
            ES3.Init();
        }
        private void OpenDebug()
        {
#if DEBUG
            GameDebug.showLog = true;
            int loadId = -1;
            AssetManager.Instance.Load<GameObject>(ref loadId, "Assets/ZRes/Debug/IngameDebugConsole.prefab", (a,b)=>
            {
                Instantiate(b);
                HotUpdate();
            });
#elif RELEASE
            GameDebug.showLog = false;
            HotUpdate()
#endif
        }
        private void HotUpdate()
        {
            HotUpdateCode.Instance.StartUpdate(LoadHotUpdateConfig);
        }
        private void LoadHotUpdateConfig()
        {
            int loadId = -1;
            AssetManager.Instance.Load<TextAsset>(ref loadId, "Assets/ZRes/GameConfig/HotUpdateConfig.txt", LoadMetadataRes);
        }
        private void LoadMetadataRes(int id, Object asset)
        {
            AssetManager.Instance.Unload(id);
            TextAsset ta = asset as TextAsset;
            var config = JsonConvert.DeserializeObject<HotUpdateConfig>(ta.text);
            string[] path = config.Metadata.ToArray();
            AssetManager.Instance.Load(ref loadId, path, LoadMetadataForAOTAssembly);
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
            Type t = Type.GetType("HotAssembly.GameStart");
            PropertyInfo p = t.BaseType.GetProperty("Instance");
            object o = p.GetMethod.Invoke(null, null);
            MethodInfo m = t.GetMethod("Init");
            m.Invoke(o, null);
#else
            int loadId = -1;
            AssetManager.Instance.Load<TextAsset>(ref loadId, "Assets/ZRes/Assembly/HotAssembly.bytes", StartHotAssembly);
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