using cfg;
using UnityEngine;

public class GameStart : Singletion<GameStart>
{
    public void Init()
    {
        StandaloneInputModule.SimulateMouseWithTouches();
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
        ConfigManager.Instance.Init(WarmUpShader);
    }
    private void WarmUpShader()
    {
        int loadId = -1;
        AssetManager.Instance.Load<ShaderVariantCollection>(ref loadId, $"{ZResConst.ResShderPath}MyShaderVariants.shadervariants", InitSetting);
    }
    private void InitSetting(int loadId, Object asset)
    {
        var svc = asset as ShaderVariantCollection;
        svc.WarmUp();
        AssetManager.Instance.Unload(ref loadId);

        DPUtil.Init();
        EnterMainScene();
    }
    private void EnterMainScene()
    {
        UIManager.Instance.OpenUI(UIType.UITest, CloseSceneLoading);
    }
    private void CloseSceneLoading(bool open)
    {
        UIManager.Instance.CloseUI(UIType.UISceneLoading);
    }
}