using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public partial class GameStart : MonoSingletion<GameStart>
{
    //xasset RuntimeInitializeOnLoad之后
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Game()
    {
        GameStart.Instance.Init();
    }
    private void Init()
    {
        XAssetInit();
        UIManager.Instance.Init();
    }
    private void Update()
    {
        EventManager.Instance.Update();

    }
}
