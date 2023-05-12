using UnityEngine;

namespace MainAssembly
{
    public class GameStart : MonoBehaviour
    {
        private void Awake()
        {
            Application.runInBackground = true;
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