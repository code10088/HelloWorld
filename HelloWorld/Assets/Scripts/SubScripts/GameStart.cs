using cfg;

namespace HotAssembly
{
    public class GameStart : Singletion<GameStart>
    {
        public void Init()
        {
            ConfigManager.Instance.InitSpecial("TbUIConfig", StartHotUpdateRes);
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
            global::NetMsgDispatch.Instance.Init();
            EnterMainScene();
        }
        private void EnterMainScene()
        {
            SceneManager.Instance.OpenScene(1, a => GameDebug.Log(a));
            SceneManager.Instance.CloseScene(1);
            SceneManager.Instance.OpenScene(1, a => GameDebug.Log(a));
            UIManager.Instance.OpenUI(UIType.UIMain);
            UIManager.Instance.CloseUI(UIType.UIMain);
            UIManager.Instance.OpenUI(UIType.UIMain);
        }
    }
}