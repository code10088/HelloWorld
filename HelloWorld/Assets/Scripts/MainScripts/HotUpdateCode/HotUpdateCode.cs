using System;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
using xasset;
using Object = UnityEngine.Object;

namespace MainAssembly
{
    public class HotUpdateCode : Singletion<HotUpdateCode>
    {
        private Action hotUpdateCodeFinish;
        private Versions latestVersion;
        private DownloadRequestBatch downloadRequestBatch;
        private int timerId = -1;

        public void StartUpdate(Action finish)
        {
            hotUpdateCodeFinish = finish;
            if (Assets.RealtimeMode) CheckUpdateInfo();
            else UpdateFinish();
        }

        #region Version
        private void CheckUpdateInfo()
        {
            var getUpdateInfoAsync = Assets.GetUpdateInfoAsync();
            getUpdateInfoAsync.completed += CheckUpdateVersion;

            UIHotUpdateCode.Instance.SetText("CheckUpdateInfo");
        }
        private void CheckUpdateVersion(Request request)
        {
            var getUpdateInfoAsync = request as GetUpdateInfoRequest;
            if (getUpdateInfoAsync.result == Request.Result.Success)
            {
                var updateVersion = System.Version.Parse(getUpdateInfoAsync.info.version);
                var playerVersion = System.Version.Parse(Assets.PlayerAssets.version);
                if (updateVersion.Minor == playerVersion.Minor)
                {
                    var getVersionsAsync = getUpdateInfoAsync.GetVersionsAsync();
                    getVersionsAsync.completed += RemoveUnusedFile;

                    UIHotUpdateCode.Instance.SetText("CheckUpdateVersion");
                }
                else
                {
                    Application.OpenURL(getUpdateInfoAsync.info.playerURL);
                    Application.Quit();
                }
            }
            else if (getUpdateInfoAsync.error.Contains("Nothing"))
            {
                CheckDownloadHotUpdateConfig();
            }
            else
            {
                UIHotUpdateCode.Instance.OpenMessageBox("Tips", "Retry", CheckUpdateInfo);
            }
        }
        private void RemoveUnusedFile(Request request)
        {
            var getVersionsAsync = request as VersionsRequest;
            if (getVersionsAsync.result == Request.Result.Success)
            {
                latestVersion = getVersionsAsync.versions;
                var bundles = new HashSet<string>();
                foreach (var item in latestVersion.data)
                {
                    bundles.Add(item.file);
                    foreach (var bundle in item.manifest.bundles)
                        bundles.Add(bundle.file);
                }

                var files = new List<string>();
                foreach (var item in Assets.Versions.data)
                {
                    if (!bundles.Contains(item.file))
                        files.Add(item.file);
                    foreach (var bundle in item.manifest.bundles)
                        if (!bundles.Contains(bundle.file))
                            files.Add(item.file);
                }

                var removeAsync = new RemoveRequest();
                foreach (var file in files)
                {
                    var path = Assets.GetDownloadDataPath(file);
                    removeAsync.files.Add(path);
                }
                removeAsync.completed += SaveVersion;
                removeAsync.SendRequest();

                UIHotUpdateCode.Instance.SetText("RemoveUnusedFile");
            }
            else
            {
                UIHotUpdateCode.Instance.OpenMessageBox("Tips", "Retry", CheckUpdateInfo);
            }
        }
        private void SaveVersion(Request request)
        {
            latestVersion.Save(Assets.GetDownloadDataPath(Versions.Filename));
            Assets.Versions = latestVersion;
            latestVersion = null;
            CheckDownloadHotUpdateConfig();
        }
        #endregion

        #region Config
        private void CheckDownloadHotUpdateConfig()
        {
            string[] include = new string[] { "HotUpdateConfig.txt" };
            var getDownloadSizeAsync = Assets.Versions.GetDownloadSizeAsync(include);
            getDownloadSizeAsync.completed += StartDownloadHotUpdateConfig;

            UIHotUpdateCode.Instance.SetText("CheckDownloadHotUpdateConfig");
        }
        private void StartDownloadHotUpdateConfig(Request request)
        {
            var getDownloadSizeAsync = request as GetDownloadSizeRequest;
            if (getDownloadSizeAsync.downloadSize > 0)
            {
                var downloadAsync = getDownloadSizeAsync.DownloadAsync();
                downloadRequestBatch = downloadAsync as DownloadRequestBatch;
                downloadRequestBatch.completed += DownloadHotUpdateConfigFinish;
                timerId = TimeManager.Instance.StartTimer(0, 1, DownloadingHotUpdateConfig);
            }
            else
            {
                LoadHotUpdateConfig();
            }
        }
        private void DownloadingHotUpdateConfig(float t)
        {
            var downloadedBytes = Utility.FormatBytes(downloadRequestBatch.downloadedBytes);
            var downloadSize = Utility.FormatBytes(downloadRequestBatch.downloadSize);
            var bandwidth = Utility.FormatBytes(downloadRequestBatch.bandwidth);

            UIHotUpdateCode.Instance.SetText($"Download：{downloadedBytes}/{downloadSize} {bandwidth}/s");
            UIHotUpdateCode.Instance.SetSlider(downloadRequestBatch.progress);
        }
        private void DownloadHotUpdateConfigFinish(DownloadRequestBatch download)
        {
            TimeManager.Instance.StopTimer(timerId);
            if (download.result == DownloadRequestBase.Result.Success) LoadHotUpdateConfig();
            else UIHotUpdateCode.Instance.OpenMessageBox("Tips", "Retry", CheckDownloadHotUpdateConfig);
        }
        private void LoadHotUpdateConfig()
        {
            AssetManager.Instance.Load<TextAsset>("HotUpdateConfig", CheckDownloadHotUpdateRes);
            UIHotUpdateCode.Instance.SetText("LoadHotUpdateConfig");
        }
        #endregion

        #region Res
        private void CheckDownloadHotUpdateRes(int id, Object asset)
        {
            AssetManager.Instance.Unload(id);
            TextAsset ta = asset as TextAsset;
            var config = JsonConvert.DeserializeObject<HotUpdateConfig>(ta.text);
            List<string> include = new List<string>();
            include.AddRange(config.Metadata);
            include.AddRange(config.HotUpdateRes);
            var getDownloadSizeAsync = Assets.Versions.GetDownloadSizeAsync(include.ToArray());
            getDownloadSizeAsync.completed += StartDownloadHotUpdateRes;

            UIHotUpdateCode.Instance.SetText("CheckDownloadHotUpdateRes");
        }
        private void StartDownloadHotUpdateRes(Request request)
        {
            var getDownloadSizeAsync = request as GetDownloadSizeRequest;
            if (getDownloadSizeAsync.downloadSize > 0)
            {
                var downloadAsync = getDownloadSizeAsync.DownloadAsync();
                downloadRequestBatch = downloadAsync as DownloadRequestBatch;
                downloadRequestBatch.completed += DownloadHotUpdateResFinish;
                timerId = TimeManager.Instance.StartTimer(0, 1, DownloadingHotUpdateRes);
            }
            else
            {
                UpdateFinish();
            }
        }
        private void DownloadingHotUpdateRes(float t)
        {
            var downloadedBytes = Utility.FormatBytes(downloadRequestBatch.downloadedBytes);
            var downloadSize = Utility.FormatBytes(downloadRequestBatch.downloadSize);
            var bandwidth = Utility.FormatBytes(downloadRequestBatch.bandwidth);

            UIHotUpdateCode.Instance.SetText($"Download：{downloadedBytes}/{downloadSize} {bandwidth}/s");
            UIHotUpdateCode.Instance.SetSlider(downloadRequestBatch.progress);
        }
        private void DownloadHotUpdateResFinish(DownloadRequestBatch download)
        {
            TimeManager.Instance.StopTimer(timerId);
            if (download.result == DownloadRequestBase.Result.Success) UpdateFinish();
            else UIHotUpdateCode.Instance.OpenMessageBox("Tips", "Retry", LoadHotUpdateConfig);
        }
        #endregion

        private void UpdateFinish()
        {
            UIHotUpdateCode.Instance.Finish();
            hotUpdateCodeFinish?.Invoke();
            hotUpdateCodeFinish = null;
        }
    }
}