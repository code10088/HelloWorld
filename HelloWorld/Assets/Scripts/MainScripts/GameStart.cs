using System;
using System.Reflection;
using UnityEngine;
using Object = UnityEngine.Object;
using Newtonsoft.Json;
using HybridCLR;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;

#if WEIXINMINIGAME
using WeChatWASM;
#elif DOUYINMINIGAME
using TTSDK;
#endif

public class GameStart : MonoSingletion<GameStart>
{
    private int loadId;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSplashScreen)]
    private static void _()
    {
        SplashScreen.Stop(SplashScreen.StopBehavior.StopImmediate);
    }
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void __()
    {
        var scene = SceneManager.GetActiveScene();
        if (scene.buildIndex == 0) Instance.___();
    }
    private void ___()
    {
        Application.runInBackground = true;
        Screen.sleepTimeout = SleepTimeout.NeverSleep;
#if WEIXINMINIGAME
        var callback = new SetKeepScreenOnOption();
        callback.keepScreenOn = true;
        WX.SetKeepScreenOn(callback);
#elif DOUYINMINIGAME
        SDK.Instance.InitSDK();
        TT.SetKeepScreenOn(true);
#endif
        if (GameDebug.GDebug == false) GameObject.DestroyImmediate(GameObject.FindWithTag("Debug"));
        AssetManager.Instance.Init(HotUpdate);
    }
    private void HotUpdate()
    {
        HotUpdateCode.Instance.StartUpdate(LoadHotUpdateConfig);
    }
    private void LoadHotUpdateConfig()
    {
        loadId = -1;
        AssetManager.Instance.Load<TextAsset>(ref loadId, GameSetting.HotUpdateConfigPath, LoadMetadataRes);
    }
    private void LoadMetadataRes(int id, Object asset)
    {
        TextAsset ta = asset as TextAsset;
        var config = JsonConvert.DeserializeObject<HotUpdateConfig>(ta.text);
        AssetManager.Instance.Unload(ref loadId);
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
        AssetManager.Instance.Unload(ref loadId);
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
        AssetManager.Instance.Load<TextAsset>(ref loadId, "Assets/ZRes/Assembly/HotAssembly.bytes", StartHotAssembly);
#endif
    }
    private void StartHotAssembly(int id, Object asset)
    {
        TextAsset ta = asset as TextAsset;
        var hotAssembly = Assembly.Load(ta.bytes);
        AssetManager.Instance.Unload(ref loadId);
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