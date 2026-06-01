using Newtonsoft.Json;
using System;
using System.Text;
using UnityEngine;
using YooAsset;
using Object = UnityEngine.Object;

public class HotUpdateCode : Singleton<HotUpdateCode>
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
        EditorSimulate();
#else
        CheckVersion();
#endif
    }

    #region Editor
    private void EditorSimulate()
    {
        var options = new LoadPackageManifestOptions("Simulate", GameSetting.timeoutS);
        var operation = AssetManager.Instance.Package.LoadPackageManifestAsync(options);
        operation.Completed += EditorSimulate;
    }
    private void EditorSimulate(AsyncOperationBase o)
    {
        UpdateFinish();
    }
    #endregion

    #region Version
    private void CheckVersion()
    {
        Downloader.Instance.Download($"{GameSetting.CDNPlatform}/VersionConfig.txt", string.Empty, CheckVersion);
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
            int index = Array.FindIndex(config.AppVersions, a => a == Application.version);
            if (index < 0)
            {
                Application.OpenURL($"{GameSetting.CDNPlatform}/{GameSetting.AppName}");
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
        var options = new LoadPackageManifestOptions(resVersion, GameSetting.timeoutS);
        var operation = AssetManager.Instance.Package.LoadPackageManifestAsync(options);
        operation.Completed += CheckPackageManifest;
    }
    private void CheckPackageManifest(AsyncOperationBase o)
    {
        UIHotUpdateCode.Instance.SetSlider((float)HotUpdateCodeStep.CheckPackageManifest / (float)HotUpdateCodeStep.Max);

        if (o.Status == EOperationStatus.Succeeded) ClearPackageUnusedCacheFiles();
        else UIHotUpdateCode.Instance.OpenCommonBox("Tips", "Retry", CheckPackageManifest);
    }
    private void ClearPackageUnusedCacheFiles()
    {
        var options = new ClearCacheOptions(ClearCacheMethods.ClearUnusedManifestFiles);
        AssetManager.Instance.Package.ClearCacheAsync(options);
        options = new ClearCacheOptions(ClearCacheMethods.ClearUnusedBundleFiles);
        AssetManager.Instance.Package.ClearCacheAsync(options);
        CheckDownloadHotUpdateConfig();
    }
    #endregion

    #region Config
    private void CheckDownloadHotUpdateConfig()
    {
        var info = AssetManager.Instance.Package.GetAssetInfo(GameSetting.HotUpdateConfigPath);
        var options = new BundleDownloaderOptions(info, true, GameSetting.downloadLimit, GameSetting.retryTime);
        downloaderOperation = AssetManager.Instance.Package.CreateResourceDownloader(options);
        if (downloaderOperation.TotalDownloadBytes > 0)
        {
            downloaderOperation.DownloadCompleted += CheckDownloadHotUpdateConfig;
            downloaderOperation.DownloadError += DownloadError;
            downloaderOperation.StartDownload();
        }
        else
        {
            downloaderOperation.CancelDownload();
            LoadHotUpdateConfig();
        }
    }
    private void CheckDownloadHotUpdateConfig(DownloadCompletedEventArgs args)
    {
        UIHotUpdateCode.Instance.SetSlider((float)HotUpdateCodeStep.DownloadingHotUpdateConfig / (float)HotUpdateCodeStep.Max);

        if (args.Succeeded) LoadHotUpdateConfig();
        else UIHotUpdateCode.Instance.OpenCommonBox("Tips", "Retry", CheckDownloadHotUpdateConfig);
    }
    private void DownloadError(DownloadErrorEventArgs args)
    {
        GameDebug.LogError($"DownloadError：{args.FileName}:{args.ErrorInfo}");
    }
    private void LoadHotUpdateConfig()
    {
        int loadId = -1;
        AssetManager.Instance.Load<TextAsset>(ref loadId, GameSetting.HotUpdateConfigPath, CheckDownloadHotUpdateRes);
    }
    #endregion

    #region Res
    private void CheckDownloadHotUpdateRes(int id, Object asset)
    {
        UIHotUpdateCode.Instance.SetSlider((float)HotUpdateCodeStep.LoadHotUpdateConfig / (float)HotUpdateCodeStep.Max);

        TextAsset ta = asset as TextAsset;
        var config = JsonConvert.DeserializeObject<HotUpdateConfig>(ta.text);
        AssetManager.Instance.Unload(ref id);
        var infos = new AssetInfo[config.HotAssembly.Length + config.HotUpdateRes.Length];
        for (int i = 0; i < config.HotAssembly.Length; i++)
        {
            infos[i] = AssetManager.Instance.Package.GetAssetInfo(config.HotAssembly[i]);
        }
        for (int i = 0; i < config.HotUpdateRes.Length; i++)
        {
            infos[i + config.HotAssembly.Length] = AssetManager.Instance.Package.GetAssetInfo(config.HotUpdateRes[i]);
        }
        var options = new BundleDownloaderOptions(infos, true, GameSetting.downloadLimit, GameSetting.retryTime);
        downloaderOperation = AssetManager.Instance.Package.CreateResourceDownloader(options);
        if (downloaderOperation.TotalDownloadBytes > 0)
        {
            downloaderOperation.DownloadCompleted += CheckDownloadHotUpdateRes;
            downloaderOperation.DownloadProgressChanged += DownloadingHotUpdateRes;
            downloaderOperation.DownloadError += DownloadError;
            downloaderOperation.StartDownload();
        }
        else
        {
            downloaderOperation.CancelDownload();
            UpdateFinish();
        }
    }
    private void CheckDownloadHotUpdateRes(DownloadCompletedEventArgs args)
    {
        UIHotUpdateCode.Instance.SetSlider((float)HotUpdateCodeStep.DownloadingHotUpdateRes / (float)HotUpdateCodeStep.Max);

        if (args.Succeeded) UpdateFinish();
        else UIHotUpdateCode.Instance.OpenCommonBox("Tips", "Retry", LoadHotUpdateConfig);
    }
    private void DownloadingHotUpdateRes(DownloadProgressChangedEventArgs args)
    {
        float f = (float)HotUpdateCodeStep.LoadHotUpdateConfig / (float)HotUpdateCodeStep.Max;
        float w = (HotUpdateCodeStep.DownloadingHotUpdateRes - HotUpdateCodeStep.LoadHotUpdateConfig) / (float)HotUpdateCodeStep.Max;
        f += args.Progress * w;
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