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
            DataManager.Instance.HotUpdateResData.StartUpdate(InitConfig);
        }
        private void InitConfig()
        {
            ConfigManager.Instance.Init(InitSetting);
        }
        private void InitSetting() 
        {
            UIManager.Instance.CloseUI(UIType.UIHotUpdateRes);
            DPUtil.Init();
            global::NetMsgDispatch.Instance.Init();
            EnterMainScene();
        }
        private void EnterMainScene()
        {
            int id = SceneManager.Instance.OpenScene(SceneType.TestScene, a => GameDebug.Log(a));
            SceneManager.Instance.CloseScene(id);
            SceneManager.Instance.OpenScene(SceneType.TestScene, a => GameDebug.Log(a));
            UIManager.Instance.OpenUI(UIType.UIMain);
            UIManager.Instance.CloseUI(UIType.UIMain);
            UIManager.Instance.OpenUI(UIType.UIMain);
        }
    }
}