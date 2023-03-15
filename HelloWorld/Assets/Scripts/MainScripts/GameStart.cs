using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class GameStart : MonoSingletion<GameStart>
{
    //xasset RuntimeInitializeOnLoad之后
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Game()
    {
        Application.runInBackground = true;
        GameStart.Instance.Init();
    }
    private void Init()
    {
        UIManager.Instance.Init();
        AssetManager.Instance.Init(HotUpdate);
    }
    private void HotUpdate(dynamic param)
    {
        GameDebug.Log("HotUpdate");
        HotUpdateManager.Instance.Start(InitConfig);
    }
    private void InitConfig()
    {
        GameDebug.Log("InitConfig");
        ConfigManager.Instance.InitConfig(EnterMainScene);
    }
    private void EnterMainScene()
    {
        GameDebug.Log("EnterMainScene");
    }

    private void Update()
    {
        AsyncManager.Instance.Update();
    }
}
