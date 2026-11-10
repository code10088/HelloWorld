using cfg;
using UnityEngine;

public class GameStart : Singleton<GameStart>
{
    private ProcessControl Process = new ProcessControl();

    public void Init()
    {
        Process.Add(new ActionProcessItem(InitLanguage));
        Process.Add(new ActionProcessItem(InitUIConfig));
        Process.Add(new ActionProcessItem(HotUpdate));
        Process.Add(new ActionProcessItem(Login));
        Process.Add(new ActionProcessItem(InitConfig));
        Process.Add(new ActionProcessItem(InitSetting));
        Process.Start();
    }
    private void InitLanguage()
    {
        LanguageManager.Instance.Init(Process.Next);
    }
    private void InitUIConfig()
    {
        ConfigManager.Instance.InitUIConfig(Process.Next);
    }
    private void HotUpdate()
    {
        UIManager.Instance.OpenUI(UIType.UIHotUpdateRes, a => UIHotUpdateCode.Instance?.Destroy());
        DataManager.Instance.HotUpdateResData.StartUpdate(Process.Next);
    }
    private void Login()
    {
        UIManager.Instance.OpenUI(UIType.UISceneLoading, a => UIManager.Instance.CloseUI(UIType.UIHotUpdateRes));
        AccountInfo.Instance.SetCallback(Process.Next);
        AccountInfo.Instance.LoginWithAuto();
    }
    private void InitConfig()
    {
        ConfigManager.Instance.Init(Process.Next);
    }
    private void InitSetting()
    {
        DPUtil.Init();
        WarmUpShader();
        Process.Next();
        Process = null;
        Connect();
    }
    private void WarmUpShader()
    {
        int loadId = -1;
        AssetManager.Instance.Load<ShaderVariantCollection>(ref loadId, $"{ZResConst.ResShderPath}MyShaderVariants.shadervariants", (a, b) =>
        {
            var svc = b as ShaderVariantCollection;
            svc.WarmUp();
            AssetManager.Instance.Unload(ref loadId);
        });
    }
    private void Connect()
    {
        EventManager.Instance.Register(EventType.RefreshPlayerData, EnterMainScene);
        var host = AccountInfo.Instance.Host;
        var port = AccountInfo.Instance.Port;
        NetClient.Instance.Create<TcpChannel>(host, port);
        NetClient.Instance.Connect();
    }
    private void EnterMainScene()
    {
        EventManager.Instance.Unregister(EventType.RefreshPlayerData, EnterMainScene);
        UIManager.Instance.OpenUI(UIType.UIMain, a => UIManager.Instance.CloseUI(UIType.UISceneLoading));
    }

    public void Restart()
    {
        NetClient.Instance.Dispose();
        SceneManager.Instance.CloseAll();
        UIManager.Instance.CloseAll();
        DataManager.Instance.Clear();

        AccountInfo.Instance.SetCallback(Connect);
        UIManager.Instance.OpenUI(UIType.UILogin);
    }
}
