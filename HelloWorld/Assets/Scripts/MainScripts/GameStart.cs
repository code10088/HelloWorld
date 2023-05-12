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
            GameVersion.Instance.Init(HotUpdate);
        }
        private void HotUpdate()
        {
            HotUpdateCode.Instance.Start();
        }
        private void Update()
        {
            AsyncManager.Instance.Update();
        }
    }
}