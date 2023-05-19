using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using UnityEngine;
using xasset;
using Object = UnityEngine.Object;

namespace MainAssembly
{
    public class HotUpdateCode : Singletion<HotUpdateCode>
    {
        private Action hotUpdateCodeFinish;
        private Versions latestVersion;

        public void StartUpdate(Action finish)
        {
            hotUpdateCodeFinish = finish;
            if (Assets.SimulationMode) UpdateFinish();
            else CheckUpdateInfo();
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
                    var getVersionsAsync = Assets.GetVersionsAsync(getUpdateInfoAsync.info);
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
            var getDownloadSizeAsync = Assets.GetDownloadSizeAsync(Assets.Versions, include);
            getDownloadSizeAsync.completed += StartDownloadHotUpdateConfig;

            UIHotUpdateCode.Instance.SetText("CheckDownloadHotUpdateConfig");
        }
        private void StartDownloadHotUpdateConfig(Request request)
        {
            var getDownloadSizeAsync = request as GetDownloadSizeRequest;
            if (getDownloadSizeAsync.downloadSize > 0)
            {
                var downloadAsync = getDownloadSizeAsync.DownloadAsync();
                var downloadRequestBatch = downloadAsync as DownloadRequestBatch;
                downloadRequestBatch.updated = DownloadingHotUpdateConfig;
                downloadRequestBatch.completed += DownloadHotUpdateConfigFinish;
            }
            else
            {
                LoadHotUpdateConfig();
            }
        }
        private void DownloadingHotUpdateConfig(DownloadRequestBatch download)
        {
            var downloadedBytes = Utility.FormatBytes(download.downloadedBytes);
            var downloadSize = Utility.FormatBytes(download.downloadSize);
            var bandwidth = Utility.FormatBytes(download.bandwidth);

            UIHotUpdateCode.Instance.SetText($"Download：{downloadedBytes}/{downloadSize} {bandwidth}/s");
            UIHotUpdateCode.Instance.SetSlider(download.progress);
        }
        private void DownloadHotUpdateConfigFinish(DownloadRequestBatch download)
        {
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
            var getDownloadSizeAsync = Assets.GetDownloadSizeAsync(Assets.Versions, include.ToArray());
            getDownloadSizeAsync.completed += StartDownloadHotUpdateRes;

            UIHotUpdateCode.Instance.SetText("CheckDownloadHotUpdateRes");
        }
        private void StartDownloadHotUpdateRes(Request request)
        {
            var getDownloadSizeAsync = request as GetDownloadSizeRequest;
            if (getDownloadSizeAsync.downloadSize > 0)
            {
                var downloadAsync = getDownloadSizeAsync.DownloadAsync();
                var downloadRequestBatch = downloadAsync as DownloadRequestBatch;
                downloadRequestBatch.updated = DownloadingHotUpdateRes;
                downloadRequestBatch.completed += DownloadHotUpdateResFinish;
            }
            else
            {
                UpdateFinish();
            }
        }
        private void DownloadingHotUpdateRes(DownloadRequestBatch download)
        {
            var downloadedBytes = Utility.FormatBytes(download.downloadedBytes);
            var downloadSize = Utility.FormatBytes(download.downloadSize);
            var bandwidth = Utility.FormatBytes(download.bandwidth);

            UIHotUpdateCode.Instance.SetText($"Download：{downloadedBytes}/{downloadSize} {bandwidth}/s");
            UIHotUpdateCode.Instance.SetSlider(download.progress);
        }
        private void DownloadHotUpdateResFinish(DownloadRequestBatch download)
        {
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