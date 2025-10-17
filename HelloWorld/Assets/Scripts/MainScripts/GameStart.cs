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
    private enum StartProcess
    {
        Init,
        HotUpdate,
        LoadConfig,
        StartHotAssembly,
    }

    private ProcessControl<ProcessItem> Process = new ProcessControl<ProcessItem>();
    private HotUpdateConfig config;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void _()
    {
        var scene = SceneManager.GetActiveScene();
        if (scene.buildIndex == 0) Instance.__();
    }
    private void __()
    {
        Process.Add((int)StartProcess.Init, Init);
        Process.Add((int)StartProcess.HotUpdate, HotUpdate);
        Process.Add((int)StartProcess.LoadConfig, LoadConfig);
        Process.Add((int)StartProcess.StartHotAssembly, StartHotAssembly);
        Process.Start();
    }

    private void Init(int id)
    {
        Application.runInBackground = true;
        Screen.sleepTimeout = SleepTimeout.NeverSleep;
        StandaloneInputModule.SimulateMouseWithTouches();
#if WEIXINMINIGAME
        var callback = new SetKeepScreenOnOption();
        callback.keepScreenOn = true;
        WX.SetKeepScreenOn(callback);
#elif DOUYINMINIGAME
        SDK.Instance.InitSDK();
        TT.SetKeepScreenOn(true);
#endif
        if (GameDebug.GDebug == false) GameObject.DestroyImmediate(GameObject.FindWithTag("Debug"));
        AssetManager.Instance.Init(Process.Next);
    }
    private void HotUpdate(int id)
    {
        HotUpdateCode.Instance.StartUpdate(Process.Next);
    }
    private void LoadConfig(int id)
    {
        int loadId = -1;
        AssetManager.Instance.Load<TextAsset>(ref loadId, GameSetting.HotUpdateConfigPath, (a, b) =>
        {
            TextAsset ta = b as TextAsset;
            config = JsonConvert.DeserializeObject<HotUpdateConfig>(ta.text);
            AssetManager.Instance.Unload(ref loadId);
            Process.Next();
        });
    }
    private void StartHotAssembly(int id)
    {
#if UNITY_EDITOR && !HotUpdateDebug
        var hotAssembly = AppDomain.CurrentDomain.GetAssemblies().First(a => a.FullName.Contains("HotAssembly"));
        var obj = hotAssembly.CreateInstance("GameStart");
        var t = hotAssembly.GetType("GameStart");
        t.GetMethod("Init").Invoke(obj, null);
#else
        int loadId = -1;
        AssetManager.Instance.Load(ref loadId, config.HotAssembly, (a, b) =>
        {
            config = null;
            for (int i = 2; i < b.Length; i++)
            {
                var ta = b[i] as TextAsset;
                RuntimeApi.LoadMetadataForAOTAssembly(ta.bytes, HomologousImageMode.SuperSet);
            }
            var dll = (b[0] as TextAsset).bytes;
            var pdb = (b[1] as TextAsset).bytes;
            AssetManager.Instance.Unload(ref loadId);

            var hotAssembly = GameDebug.GDebug ? Assembly.Load(dll, pdb) : Assembly.Load(dll);
            var obj = hotAssembly.CreateInstance("GameStart");
            var t = hotAssembly.GetType("GameStart");
            t.GetMethod("Init").Invoke(obj, null);

            Process.Next();
            Process = null;
        });
#endif
    }
    private void Update()
    {
        AsyncManager.Instance.Update();
    }
}