namespace HotAssembly
{
    public class GameStart : Singletion<GameStart>
    {
        public void Init()
        {
            ConfigManager.Instance.InitSpecial("Data_UIConfig", StartHotUpdateRes);
        }
        private void StartHotUpdateRes()
        {
            DataManager.Instance.HotUpdateResData.StartUpdate(InitConfig);
        }
        private void InitConfig()
        {
            ConfigManager.Instance.Init(InitSetting);
        }
        private void InitSetting() 
        {
            DevicePerformanceUtil.Init();
            SocketManager.Instance.Init(NetMsgDispatch.Instance.Deserialize);
            EnterMainScene();
        }
        private void EnterMainScene()
        {
            UIManager.Instance.OpenUI(UIType.UITest);
        }
    }
}