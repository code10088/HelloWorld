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
        Downloader.Instance.Download($"{GameSetting.Instance.CDNPlatform}VersionConfig.txt", string.Empty, CheckVersion);

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
                Application.OpenURL($"{GameSetting.Instance.CDNPlatform}{GameSetting.AppName}");
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
        var operation = AssetManager.Package.UpdatePackageManifestAsync(resVersion);
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
        downloaderOperation = AssetManager.Package.CreateBundleDownloader(GameSetting.HotUpdateConfigPath, 1, GameSetting.retryTime, GameSetting.timeoutS);
        if (downloaderOperation.TotalDownloadBytes > 0)
        {
            downloaderOperation.OnDownloadOverCallback = CheckDownloadHotUpdateConfig;
            downloaderOperation.OnDownloadErrorCallback = DownloadError;
            downloaderOperation.BeginDownload();

            UIHotUpdateCode.Instance.SetText(HotUpdateCodeStep.DownloadingHotUpdateConfig.ToString());
        }
        else
        {
            downloaderOperation.CancelDownload();
            LoadHotUpdateConfig();
        }
    }
    private void CheckDownloadHotUpdateConfig(bool isSucceed)
    {
        UIHotUpdateCode.Instance.SetSlider((float)HotUpdateCodeStep.DownloadingHotUpdateConfig / (float)HotUpdateCodeStep.Max);

        if (isSucceed) LoadHotUpdateConfig();
        else UIHotUpdateCode.Instance.OpenCommonBox("Tips", "Retry", CheckDownloadHotUpdateConfig);
    }
    private void DownloadError(string fileName, string error)
    {
        GameDebug.LogError($"DownloadError：{fileName}:{error}");
    }
    private void LoadHotUpdateConfig()
    {
        int loadId = -1;
        AssetManager.Instance.Load<TextAsset>(ref loadId, GameSetting.HotUpdateConfigPath, CheckDownloadHotUpdateRes);

        UIHotUpdateCode.Instance.SetText(HotUpdateCodeStep.LoadHotUpdateConfig.ToString());
    }
    #endregion

    #region Res
    private void CheckDownloadHotUpdateRes(int id, Object asset)
    {
        UIHotUpdateCode.Instance.SetSlider((float)HotUpdateCodeStep.LoadHotUpdateConfig / (float)HotUpdateCodeStep.Max);

        TextAsset ta = asset as TextAsset;
        var config = JsonConvert.DeserializeObject<HotUpdateConfig>(ta.text);
        AssetManager.Instance.Unload(ref id);
        int count1 = config.Metadata.Count;
        int count2 = count1 + config.HotUpdateRes.Count;
        string[] paths = new string[count2];
        for (int i = 0; i < count2; i++)
        {
            paths[i] = i < count1 ? config.Metadata[i] : config.HotUpdateRes[i - count1];
        }
        downloaderOperation = AssetManager.Package.CreateBundleDownloader(paths, GameSetting.downloadLimit, GameSetting.retryTime, GameSetting.timeoutS);
        if (downloaderOperation.TotalDownloadBytes > 0)
        {
            downloaderOperation.OnDownloadOverCallback = CheckDownloadHotUpdateRes;
            downloaderOperation.OnDownloadProgressCallback = DownloadingHotUpdateRes;
            downloaderOperation.OnDownloadErrorCallback = DownloadError;
            downloaderOperation.BeginDownload();

            UIHotUpdateCode.Instance.SetText(HotUpdateCodeStep.DownloadingHotUpdateRes.ToString());
        }
        else
        {
            downloaderOperation.CancelDownload();
            UpdateFinish();
        }
    }
    private void CheckDownloadHotUpdateRes(bool isSucceed)
    {
        UIHotUpdateCode.Instance.SetSlider((float)HotUpdateCodeStep.DownloadingHotUpdateRes / (float)HotUpdateCodeStep.Max);

        if (isSucceed) UpdateFinish();
        else UIHotUpdateCode.Instance.OpenCommonBox("Tips", "Retry", LoadHotUpdateConfig);
    }
    private void DownloadingHotUpdateRes(int totalDownloadCount, int currentDownloadCount, long totalDownloadBytes, long currentDownloadBytes)
    {
        UIHotUpdateCode.Instance.SetText($"{HotUpdateCodeStep.DownloadingHotUpdateRes}：{currentDownloadBytes}/{totalDownloadBytes}");
        float f = (float)HotUpdateCodeStep.LoadHotUpdateConfig / (float)HotUpdateCodeStep.Max;
        float w = (HotUpdateCodeStep.DownloadingHotUpdateRes - HotUpdateCodeStep.LoadHotUpdateConfig) / (float)HotUpdateCodeStep.Max;
        f += currentDownloadBytes / totalDownloadBytes * w;
        UIHotUpdateCode.Instance.SetSlider(f);
    }
    #endregion

    private void UpdateFinish()
    {
        downloaderOperation = null;
        hotUpdateCodeFinish?.Invoke();
        hotUpdateCodeFinish = null;
    }
}