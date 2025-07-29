using System;
using System.Reflection;
using UnityEngine;
using Object = UnityEngine.Object;
using Newtonsoft.Json;
using HybridCLR;
using UnityEngine.SceneManagement;
using System.Linq;

#if WEIXINMINIGAME
using WeChatWASM;
#elif DOUYINMINIGAME
using TTSDK;
#endif

public class GameStart : MonoSingletion<GameStart>
{
    private int loadId = -1;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void _()
    {
        var scene = SceneManager.GetActiveScene();
        if (scene.buildIndex == 0) Instance.__();
    }
    private void __()
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
        AssetManager.Instance.Load<TextAsset>(ref loadId, GameSetting.HotUpdateConfigPath, LoadMetadataRes);
    }
    private void LoadMetadataRes(int id, Object asset)
    {
        TextAsset ta = asset as TextAsset;
        var config = JsonConvert.DeserializeObject<HotUpdateConfig>(ta.text);
        AssetManager.Instance.Unload(ref loadId);
        AssetManager.Instance.Load(ref loadId, config.HotAssembly, LoadMetadataForAOTAssembly);
    }
    private void LoadMetadataForAOTAssembly(string[] path, Object[] assets)
    {
        for (int i = 2; i < assets.Length; i++)
        {
            var ta = assets[i] as TextAsset;
            RuntimeApi.LoadMetadataForAOTAssembly(ta.bytes, HomologousImageMode.SuperSet);
        }
        var dll = assets[0] as TextAsset;
        var pdb = assets[1] as TextAsset;
        StartHotAssembly(dll.bytes, pdb.bytes);
        AssetManager.Instance.Unload(ref loadId);
    }
    private void StartHotAssembly(byte[] dll, byte[] pdb)
    {
#if UNITY_EDITOR && !HotUpdateDebug
        var hotAssembly = AppDomain.CurrentDomain.GetAssemblies().First(a => a.FullName.Contains("HotAssembly"));
        Type t = hotAssembly.GetType("GameStart");
        PropertyInfo p = t.BaseType.GetProperty("Instance");
        object o = p.GetMethod.Invoke(null, null);
        MethodInfo m = t.GetMethod("Init");
        m.Invoke(o, null);
#else
        var hotAssembly = GameDebug.GDebug ? Assembly.Load(dll, pdb) : Assembly.Load(dll);
        Type t = hotAssembly.GetType("GameStart");
        PropertyInfo p = t.BaseType.GetProperty("Instance");
        object o = p.GetMethod.Invoke(null, null);
        MethodInfo m = t.GetMethod("Init");
        m.Invoke(o, null);
#endif
    }
    private void Update()
    {
        AsyncManager.Instance.Update();
    }
}