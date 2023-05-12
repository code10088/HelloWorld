namespace HotAssembly
{
    public class GameStart : Singletion<GameStart>
    {
        public void Init()
        {
            AssetManager.Instance.Init(InitUIConfig);
        }
        private void InitUIConfig()
        {
            ConfigManager.Instance.InitSpecial("Data_UIConfig", OpenUIHotUpdateRes);
        }
        private void OpenUIHotUpdateRes()
        {
            UIManager.Instance.OpenUI(UIType.UIHotUpdateRes, StartHotUpdateRes);
        }
        private void StartHotUpdateRes()
        {
            DataManager.Instance.HotUpdateResData.StartUpdate(InitSetting);
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