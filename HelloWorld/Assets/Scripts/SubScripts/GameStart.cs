using cfg;

namespace HotAssembly
{
    public class GameStart : Singletion<GameStart>
    {
        public void Init()
        {
            InputSystemUIInputModule.EnableTouchSimulation();
            ConfigManager.Instance.InitSpecial("TbUIConfig", OpenUIHotUpdateRes);
        }
        private void OpenUIHotUpdateRes()
        {
            UIManager.Instance.OpenUI(UIType.UIHotUpdateRes, StartHotUpdateRes);
        }
        private void StartHotUpdateRes(bool success)
        {
            UIHotUpdateCode.Instance.Destroy();
            DataManager.Instance.HotUpdateResData.StartUpdate(OpenUILoading);
        }
        private void OpenUILoading()
        {
            UIManager.Instance.OpenUI(UIType.UISceneLoading, InitConfig);
        }
        private void InitConfig(bool success)
        {
            UIManager.Instance.CloseUI(UIType.UIHotUpdateRes);
            ConfigManager.Instance.Init(InitSetting);
        }
        private void InitSetting() 
        {
            DPUtil.Init();
            EnterMainScene();
        }
        private void EnterMainScene()
        {
            DataManager.Instance.GuideData.Init();
            UIManager.Instance.OpenUI(UIType.UITest, CloseSceneLoading);
        }
        private void CloseSceneLoading(bool open)
        {
            UIManager.Instance.CloseUI(UIType.UISceneLoading);
        }
    }
}