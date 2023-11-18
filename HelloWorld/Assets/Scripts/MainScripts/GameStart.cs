using System;
using System.Reflection;
using UnityEngine;
using Object = UnityEngine.Object;
using Newtonsoft.Json;
using HybridCLR;

public class GameStart : MonoBehaviour
{
    private int loadId;

    private void Start()
    {
        Application.runInBackground = true;
        Screen.sleepTimeout = SleepTimeout.NeverSleep;
        AssetManager.Instance.Init(CheckDebug);
        ES3.Init();
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
        GameDebug.LogError(1);
        for (int i = 0; i < assets.Length; i++)
        {
            if (assets[i] != null)
            {
                var ta = assets[i] as TextAsset;
                RuntimeApi.LoadMetadataForAOTAssembly(ta.bytes, HomologousImageMode.SuperSet);
            }
        }
        GameDebug.LogError(2);
        AssetManager.Instance.Unload(loadId);
        StartHotAssembly();
        GameDebug.LogError(3);
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
        GameDebug.LogError(4);
        int loadId = -1;
        AssetManager.Instance.Load<TextAsset>(ref loadId, "Assets/ZRes/Assembly/HotAssembly.bytes", StartHotAssembly);
#endif
    }
    private void StartHotAssembly(int id, Object asset)
    {
        GameDebug.LogError(5);
        AssetManager.Instance.Unload(id);
        TextAsset ta = asset as TextAsset;
        var hotAssembly = Assembly.Load(ta.bytes);
        Type t = hotAssembly.GetType("HotAssembly.GameStart");
        PropertyInfo p = t.BaseType.GetProperty("Instance");
        object o = p.GetMethod.Invoke(null, null);
        MethodInfo m = t.GetMethod("Init");
        m.Invoke(o, null);
        GameDebug.LogError(6);
    }
    private void Update()
    {
        AsyncManager.Instance.Update();
    }
}