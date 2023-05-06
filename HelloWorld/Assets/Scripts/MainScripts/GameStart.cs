using UnityEngine;

namespace MainAssembly
{
    public class GameStart : MonoBehaviour
    {
        public static GameStart Instance;

        private void Awake()
        {
            Application.runInBackground = true;
            Instance = this;
            Init();
        }
        private void Init()
        {
            AssetManager.Instance.Init(HotUpdate);
        }
        private void HotUpdate()
        {
            MainAssembly.HotUpdate.Instance.Start();
        }
        private void Update()
        {
            AsyncManager.Instance.Update();
        }
    }
}