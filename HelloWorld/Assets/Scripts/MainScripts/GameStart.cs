using System;
using System.Reflection;
using UnityEngine;
using Object = UnityEngine.Object;
using Newtonsoft.Json;
using HybridCLR;

public class GameStart : MonoSingletion<GameStart>
{
    private int loadId;

    [RuntimeInitializeOnLoadMethod]
    private static void _()
    {
        Instance.__();
    }
    public void __()
    {
        Application.runInBackground = true;
        Screen.sleepTimeout = SleepTimeout.NeverSleep;
    }
    private void Start()
    {
        AssetManager.Instance.Init(CheckDebug);
    }
    private void CheckDebug()
    {
        if (GameDebug.GDebug)
        {
            int loadId = -1;
            AssetManager.Instance.Load<GameObject>(ref loadId, "Assets/ZRes/Debug/IngameDebugConsole.prefab", (a, b) => Instantiate(b));
            loadId = -1;
            AssetManager.Instance.Load<GameObject>(ref loadId, "Assets/ZRes/Debug/AdvancedFPSCounter.prefab", (a, b) => Instantiate(b));
        }
        HotUpdate();
    }
    private void HotUpdate()
    {
        HotUpdateCode.Instance.StartUpdate(LoadHotUpdateConfig);
    }
    private void LoadHotUpdateConfig()
    {
        int loadId = -1;
        AssetManager.Instance.Load<TextAsset>(ref loadId, GameSetting.HotUpdateConfigPath, LoadMetadataRes);
    }
    private void LoadMetadataRes(int id, Object asset)
    {
        TextAsset ta = asset as TextAsset;
        var config = JsonConvert.DeserializeObject<HotUpdateConfig>(ta.text);
        AssetManager.Instance.Unload(id);
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
#if UNITY_EDITOR && !HotUpdateDebug
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
        TextAsset ta = asset as TextAsset;
        var hotAssembly = Assembly.Load(ta.bytes);
        AssetManager.Instance.Unload(id);
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