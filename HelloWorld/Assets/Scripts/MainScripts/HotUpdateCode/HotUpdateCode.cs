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
        EditorSimulate();
#else
        CheckVersion();
#endif
    }

    #region Editor
    private void EditorSimulate()
    {
        var operation = AssetManager.Instance.Package.UpdatePackageManifestAsync("Simulate");
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
        var operation = AssetManager.Instance.Package.UpdatePackageManifestAsync(resVersion);
        operation.Completed += CheckPackageManifest;
    }
    private void CheckPackageManifest(AsyncOperationBase o)
    {
        UIHotUpdateCode.Instance.SetSlider((float)HotUpdateCodeStep.CheckPackageManifest / (float)HotUpdateCodeStep.Max);

        if (o.Status == EOperationStatus.Succeed) ClearPackageUnusedCacheFiles();
        else UIHotUpdateCode.Instance.OpenCommonBox("Tips", "Retry", CheckPackageManifest);
    }
    private void ClearPackageUnusedCacheFiles()
    {
        AssetManager.Instance.Package.ClearCacheFilesAsync(EFileClearMode.ClearUnusedManifestFiles);
        AssetManager.Instance.Package.ClearCacheFilesAsync(EFileClearMode.ClearUnusedBundleFiles);
        CheckDownloadHotUpdateConfig();
    }
    #endregion

    #region Config
    private void CheckDownloadHotUpdateConfig()
    {
        downloaderOperation = AssetManager.Instance.Package.CreateBundleDownloader(GameSetting.HotUpdateConfigPath, 1, GameSetting.retryTime);
        if (downloaderOperation.TotalDownloadBytes > 0)
        {
            downloaderOperation.DownloadFinishCallback = CheckDownloadHotUpdateConfig;
            downloaderOperation.DownloadErrorCallback = DownloadError;
            downloaderOperation.BeginDownload();
        }
        else
        {
            downloaderOperation.CancelDownload();
            LoadHotUpdateConfig();
        }
    }
    private void CheckDownloadHotUpdateConfig(DownloaderFinishData data)
    {
        UIHotUpdateCode.Instance.SetSlider((float)HotUpdateCodeStep.DownloadingHotUpdateConfig / (float)HotUpdateCodeStep.Max);

        if (data.Succeed) LoadHotUpdateConfig();
        else UIHotUpdateCode.Instance.OpenCommonBox("Tips", "Retry", CheckDownloadHotUpdateConfig);
    }
    private void DownloadError(DownloadErrorData data)
    {
        GameDebug.LogError($"DownloadError：{data.FileName}:{data.ErrorInfo}");
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
        string[] paths = new string[config.HotAssembly.Length + config.HotUpdateRes.Length];
        Array.Copy(config.HotAssembly, 0, paths, 0, config.HotAssembly.Length);
        Array.Copy(config.HotUpdateRes, 0, paths, config.HotAssembly.Length, config.HotUpdateRes.Length);
        downloaderOperation = AssetManager.Instance.Package.CreateBundleDownloader(paths, GameSetting.downloadLimit, GameSetting.retryTime);
        if (downloaderOperation.TotalDownloadBytes > 0)
        {
            downloaderOperation.DownloadFinishCallback = CheckDownloadHotUpdateRes;
            downloaderOperation.DownloadUpdateCallback = DownloadingHotUpdateRes;
            downloaderOperation.DownloadErrorCallback = DownloadError;
            downloaderOperation.BeginDownload();
        }
        else
        {
            downloaderOperation.CancelDownload();
            UpdateFinish();
        }
    }
    private void CheckDownloadHotUpdateRes(DownloaderFinishData data)
    {
        UIHotUpdateCode.Instance.SetSlider((float)HotUpdateCodeStep.DownloadingHotUpdateRes / (float)HotUpdateCodeStep.Max);

        if (data.Succeed) UpdateFinish();
        else UIHotUpdateCode.Instance.OpenCommonBox("Tips", "Retry", LoadHotUpdateConfig);
    }
    private void DownloadingHotUpdateRes(DownloadUpdateData data)
    {
        float f = (float)HotUpdateCodeStep.LoadHotUpdateConfig / (float)HotUpdateCodeStep.Max;
        float w = (HotUpdateCodeStep.DownloadingHotUpdateRes - HotUpdateCodeStep.LoadHotUpdateConfig) / (float)HotUpdateCodeStep.Max;
        f += data.Progress * w;
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