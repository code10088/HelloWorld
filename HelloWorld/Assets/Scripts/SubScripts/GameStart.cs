using cfg;
using UnityEngine;

public class GameStart
{
    private ProcessControl Process = new ProcessControl();

    public void Init()
    {
        Process.Add(new ActionProcessItem(InitLanguage));
        Process.Add(new ActionProcessItem(InitUIConfig));
        Process.Add(new ActionProcessItem(OpenUIHotUpdateRes));
        Process.Add(new ActionProcessItem(HotUpdate));
        Process.Add(new ActionProcessItem(OpenUILoading));
        Process.Add(new ActionProcessItem(InitConfig));
        Process.Add(new ActionProcessItem(InitSetting));
        Process.Add(new ActionProcessItem(WarmUpShader));
        Process.Add(new ActionProcessItem(EnterMainScene));
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
    private void OpenUIHotUpdateRes()
    {
        UIManager.Instance.OpenUI(UIType.UIHotUpdateRes, a =>
        {
            UIHotUpdateCode.Instance.Destroy();
            Process.Next();
        });
    }
    private void HotUpdate()
    {
        DataManager.Instance.HotUpdateResData.StartUpdate(Process.Next);
    }
    private void OpenUILoading()
    {
        UIManager.Instance.OpenUI(UIType.UISceneLoading, a => Process.Next());
    }
    private void InitConfig()
    {
        UIManager.Instance.CloseUI(UIType.UIHotUpdateRes);
        ConfigManager.Instance.Init(Process.Next);
    }
    private void InitSetting()
    {
        DPUtil.Init();
        Process.Next();
    }
    private void WarmUpShader()
    {
        int loadId = -1;
        AssetManager.Instance.Load<ShaderVariantCollection>(ref loadId, $"{ZResConst.ResShderPath}MyShaderVariants.shadervariants", (a, b) =>
        {
            var svc = b as ShaderVariantCollection;
            svc.WarmUp();
            AssetManager.Instance.Unload(ref loadId);
            Process.Next();
        });
    }
    private void EnterMainScene()
    {
        UIManager.Instance.OpenUI(UIType.UIMain, a => UIManager.Instance.CloseUI(UIType.UISceneLoading));
        Process.Next();
        Process = null;
    }
}