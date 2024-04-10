using System;
using UnityEngine;
using Newtonsoft.Json;
using YooAsset;
using Object = UnityEngine.Object;
using System.Text;

public class HotUpdateCode : Singletion<HotUpdateCode>
{
    enum HotUpdateCodeStep
    {
        CheckVersion = 10,
        CheckPackageManifest = 20,
        DownloadingHotUpdateConfig = 30,
        LoadHotUpdateConfig = 35,
        DownloadingHotUpdateRes = 100,
        Max = 100,
    }

    private Action hotUpdateCodeFinish;
    private string resVersion;
    private ResourceDownloaderOperation downloaderOperation;
    private int timerId = -1;

    public void StartUpdate(Action finish)
    {
        hotUpdateCodeFinish = finish;
#if UNITY_EDITOR && !HotUpdateDebug
        UpdateFinish();
#else
        CheckVersion();
#endif
    }

    #region Version
    private void CheckVersion()
    {
        Downloader.Instance.Download($"{GameSetting.Instance.CDNPlatform}/VersionConfig.txt", string.Empty, CheckVersion);

        UIHotUpdateCode.Instance.SetText(HotUpdateCodeStep.CheckVersion.ToString());
    }
    private void CheckVersion(string url, byte[] result)
    {
        UIHotUpdateCode.Instance.SetSlider((float)HotUpdateCodeStep.CheckVersion / (float)HotUpdateCodeStep.Max);

        if (result == null)
        {
            UIHotUpdateCode.Instance.OpenCommonBox("Tips", "Retry", CheckVersion);
        }
        else
        {
            string version = Encoding.UTF8.GetString(result);
            var config = JsonConvert.DeserializeObject<VersionConfig>(version);
            int index = config.AppVersions.FindIndex(a => a == Application.version);
            if (index < 0)
            {
                Application.OpenURL($"{GameSetting.Instance.CDNPlatform}/{GameSetting.AppName}");
                Application.Quit();
            }
            else
            {
                resVersion = config.ResVersions[index];
                CheckPackageManifest();
            }
        }
    }
    private void CheckPackageManifest()
    {
        var operation = AssetManager.Package.UpdatePackageManifestAsync(resVersion, true);
        operation.Completed += CheckPackageManifest;

        UIHotUpdateCode.Instance.SetText(HotUpdateCodeStep.CheckPackageManifest.ToString());
    }
    private void CheckPackageManifest(AsyncOperationBase o)
    {
        UIHotUpdateCode.Instance.SetSlider((float)HotUpdateCodeStep.CheckPackageManifest / (float)HotUpdateCodeStep.Max);

        if (o.Status == EOperationStatus.Succeed) CheckDownloadHotUpdateConfig();
        else UIHotUpdateCode.Instance.OpenCommonBox("Tips", "Retry", CheckPackageManifest);
    }
    #endregion

    #region Config
    private void CheckDownloadHotUpdateConfig()
    {
        string path = "Assets/ZRes/GameConfig/HotUpdateConfig.txt";
        downloaderOperation = AssetManager.Package.CreateBundleDownloader(path, 1, GameSetting.retryTime, GameSetting.timeout);
        downloaderOperation.Completed += CheckDownloadHotUpdateConfig;
        downloaderOperation.BeginDownload();
        timerId = TimeManager.Instance.StartTimer(0, 1, DownloadingHotUpdateConfig);

        UIHotUpdateCode.Instance.SetText(HotUpdateCodeStep.DownloadingHotUpdateConfig.ToString());
    }
    private void CheckDownloadHotUpdateConfig(AsyncOperationBase o)
    {
        UIHotUpdateCode.Instance.SetSlider((float)HotUpdateCodeStep.DownloadingHotUpdateConfig / (float)HotUpdateCodeStep.Max);

        TimeManager.Instance.StopTimer(timerId);
        if (o.Status == EOperationStatus.Succeed) LoadHotUpdateConfig();
        else UIHotUpdateCode.Instance.OpenCommonBox("Tips", "Retry", CheckDownloadHotUpdateConfig);
    }
    private void DownloadingHotUpdateConfig(float t)
    {
        UIHotUpdateCode.Instance.SetText($"{HotUpdateCodeStep.DownloadingHotUpdateConfig}：{downloaderOperation.CurrentDownloadBytes}/{downloaderOperation.TotalDownloadBytes}");
        float f = (float)HotUpdateCodeStep.CheckPackageManifest / (float)HotUpdateCodeStep.Max;
        float w = (HotUpdateCodeStep.DownloadingHotUpdateConfig - HotUpdateCodeStep.CheckPackageManifest) / (float)HotUpdateCodeStep.Max;
        f += downloaderOperation.Progress * w;
        UIHotUpdateCode.Instance.SetSlider(f);
    }
    private void LoadHotUpdateConfig()
    {
        int loadId = -1;
        AssetManager.Instance.Load<TextAsset>(ref loadId, "Assets/ZRes/GameConfig/HotUpdateConfig.txt", CheckDownloadHotUpdateRes);
        UIHotUpdateCode.Instance.SetText(HotUpdateCodeStep.LoadHotUpdateConfig.ToString());
    }
    #endregion

    #region Res
    private void CheckDownloadHotUpdateRes(int id, Object asset)
    {
        UIHotUpdateCode.Instance.SetSlider((float)HotUpdateCodeStep.LoadHotUpdateConfig / (float)HotUpdateCodeStep.Max);

        AssetManager.Instance.Unload(id);
        TextAsset ta = asset as TextAsset;
        var config = JsonConvert.DeserializeObject<HotUpdateConfig>(ta.text);
        int count1 = config.Metadata.Count;
        int count2 = count1 + config.HotUpdateRes.Count;
        string[] paths = new string[count2];
        for (int i = 0; i < count2; i++)
        {
            paths[i] = i < count1 ? config.Metadata[i] : config.HotUpdateRes[i - count1];
        }
        downloaderOperation = AssetManager.Package.CreateBundleDownloader(paths, GameSetting.downloadLimit, GameSetting.retryTime, GameSetting.timeout);
        downloaderOperation.Completed += CheckDownloadHotUpdateRes;
        downloaderOperation.BeginDownload();
        timerId = TimeManager.Instance.StartTimer(0, 1, DownloadingHotUpdateRes);

        UIHotUpdateCode.Instance.SetText(HotUpdateCodeStep.DownloadingHotUpdateRes.ToString());
    }
    private void CheckDownloadHotUpdateRes(AsyncOperationBase o)
    {
        UIHotUpdateCode.Instance.SetSlider((float)HotUpdateCodeStep.DownloadingHotUpdateRes / (float)HotUpdateCodeStep.Max);

        TimeManager.Instance.StopTimer(timerId);
        if (o.Status == EOperationStatus.Succeed) UpdateFinish();
        else UIHotUpdateCode.Instance.OpenCommonBox("Tips", "Retry", LoadHotUpdateConfig);
    }
    private void DownloadingHotUpdateRes(float t)
    {
        UIHotUpdateCode.Instance.SetText($"{HotUpdateCodeStep.DownloadingHotUpdateRes}：{downloaderOperation.CurrentDownloadBytes}/{downloaderOperation.TotalDownloadBytes}");
        float f = (float)HotUpdateCodeStep.LoadHotUpdateConfig / (float)HotUpdateCodeStep.Max;
        float w = (HotUpdateCodeStep.DownloadingHotUpdateRes - HotUpdateCodeStep.LoadHotUpdateConfig) / (float)HotUpdateCodeStep.Max;
        f += downloaderOperation.Progress * w;
        UIHotUpdateCode.Instance.SetSlider(f);
    }
    #endregion

    private void UpdateFinish()
    {
        UIHotUpdateCode.Instance.Finish();
        hotUpdateCodeFinish?.Invoke();
        hotUpdateCodeFinish = null;
    }
}