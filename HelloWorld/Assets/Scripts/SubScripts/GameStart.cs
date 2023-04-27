using MainAssembly;

namespace HotAssembly
{
    public class GameStart : MonoSingletion<GameStart>
    {
        public void Init()
        {
            UIManager.Instance.Init();
            NetManager.Instance.Init();
            ConfigManager.Instance.Init(EnterMainScene);
        }
        private void EnterMainScene()
        {
            UIManager.Instance.OpenUI(UIType.UITest);
        }
    }
}