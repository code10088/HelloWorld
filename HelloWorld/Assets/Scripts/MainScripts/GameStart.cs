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
            GameStart.Instance.Init();
        }
        private void Init()
        {
            AssetManager.Instance.Init(HotUpdate);
        }
        private void HotUpdate(dynamic param)
        {
            MainAssembly.HotUpdate.Instance.Start();
        }

        private void Update()
        {
            AsyncManager.Instance.Update();
        }
    }
}