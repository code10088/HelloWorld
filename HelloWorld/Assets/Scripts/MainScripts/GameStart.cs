using UnityEngine;

namespace MainAssembly
{
    public class GameStart : MonoSingletion<GameStart>
    {
        //xasset RuntimeInitializeOnLoad之后
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Game()
        {
            Application.runInBackground = true;
            TimeManager.Instance.StartTimer(5, finish: GameStart.Instance.Init);
            //GameStart.Instance.Init();
        }
        private void Init()
        {
            UIManager.Instance.Init();
            AssetManager.Instance.Init(HotUpdate);
        }
        private void HotUpdate(dynamic param)
        {
            GameDebug.Log("HotUpdate");
            HotUpdateManager.Instance.Start(HotAssembly);
        }
        private void HotAssembly()
        {
            HotAssembleManager.Instance.Init(Finish);
        }
        private void Finish()
        {

        }

        private void Update()
        {
            AsyncManager.Instance.Update();
        }
    }
}