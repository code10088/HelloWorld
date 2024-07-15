using cfg;

namespace HotAssembly
{
    public class GameStart : Singletion<GameStart>
    {
        public void Init()
        {
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
            UIManager.Instance.OpenUI(UIType.UIMain);
            SceneManager.Instance.OpenScene(SceneType.BattleScene);
        }
    }
}