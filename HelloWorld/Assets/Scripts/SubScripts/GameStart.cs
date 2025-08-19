using cfg;
using UnityEngine;

public class GameStart : Singletion<GameStart>
{
    private enum StartProcess
    {
        InitUIConfig,
        OpenUIHotUpdateRes,
        HotUpdate,
        OpenUILoading,
        InitConfig,
        WarmUpShader,
        InitSetting,
        EnterMainScene,
    }

    private ProcessControl<ProcessItem> Process = new ProcessControl<ProcessItem>();

    public void Init()
    {
        Process.Add((int)StartProcess.InitUIConfig, InitUIConfig);
        Process.Add((int)StartProcess.OpenUIHotUpdateRes, OpenUIHotUpdateRes);
        Process.Add((int)StartProcess.HotUpdate, HotUpdate);
        Process.Add((int)StartProcess.OpenUILoading, OpenUILoading);
        Process.Add((int)StartProcess.InitConfig, InitConfig);
        Process.Add((int)StartProcess.InitSetting, InitSetting);
        Process.Add((int)StartProcess.WarmUpShader, WarmUpShader);
        Process.Add((int)StartProcess.EnterMainScene, EnterMainScene);
        Process.Start();
    }
    private void InitUIConfig(int id)
    {
        ConfigManager.Instance.InitSpecial("TbUIConfig", Process.Next);
    }
    private void OpenUIHotUpdateRes(int id)
    {
        UIManager.Instance.OpenUI(UIType.UIHotUpdateRes, a =>
        {
            UIHotUpdateCode.Instance.Destroy();
            Process.Next();
        });
    }
    private void HotUpdate(int id)
    {
        DataManager.Instance.HotUpdateResData.StartUpdate(Process.Next);
    }
    private void OpenUILoading(int id)
    {
        UIManager.Instance.OpenUI(UIType.UISceneLoading, a => Process.Next());
    }
    private void InitConfig(int id)
    {
        UIManager.Instance.CloseUI(UIType.UIHotUpdateRes);
        ConfigManager.Instance.Init(Process.Next);
    }
    private void InitSetting(int id)
    {
        DPUtil.Init();
        Process.Next();
    }
    private void WarmUpShader(int id)
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
    private void EnterMainScene(int id)
    {
        UIManager.Instance.OpenUI(UIType.UITest, a => UIManager.Instance.CloseUI(UIType.UISceneLoading));
        Process.Next();
        Process = null;
    }
}