namespace HotAssembly
{
    public class GameStart : Singletion<GameStart>
    {
        public void Init()
        {
            UIManager.Instance.Init();
            ConfigManager.Instance.Init(InitSetting);
        }
        private void InitSetting() 
        { 
            DevicePerformanceUtil.Init();
            NetMsgDispatch.Instance.Init();
            SocketManager.Instance.Init(NetMsgDispatch.Instance.Deserialize);
            EnterMainScene();
        }
        private void EnterMainScene()
        {
            UIManager.Instance.OpenUI(UIType.UITest);
        }
    }
}