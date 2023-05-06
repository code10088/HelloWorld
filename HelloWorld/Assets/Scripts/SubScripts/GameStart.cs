using MainAssembly;

namespace HotAssembly
{
    public class GameStart : Singletion<GameStart>
    {
        public void Init()
        {
            UIManager.Instance.Init();
            NetManager.Instance.Init();
            ConfigManager.Instance.Init(InitSetting);
        }
        private void InitSetting() 
        { 
            DevicePerformanceUtil.Init();
            EnterMainScene();
        }
        private void EnterMainScene()
        {
            UIManager.Instance.OpenUI(UIType.UITest);
        }
    }
}