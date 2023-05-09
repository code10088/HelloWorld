using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using xasset;
using Object = UnityEngine.Object;

namespace MainAssembly
{
    public class HotUpdate : Singletion<HotUpdate>
    {
        private Versions versions;
        private Assembly hotAssembly;
        private int updateId = -1;
        private Request request;

        public void Start()
        {
            updateId = UpdateManager.Instance.StartUpdate(Update);
            CheckUpdateInfo();
        }
        public void Update()
        {
            if (request != null) UIHotUpdate.Instance.SetSlider(request.progress);
        }
        private void CheckUpdateInfo()
        {
            if (Assets.SimulationMode)
            {
                UpdateFinish_LoadHotAssembly();
            }
            else
            {
                Assets.UpdateInfoURL = "http://192.168.6.2/BundlesCache/Windows/updateinfo.json";
                var getUpdateInfoAsync = Assets.GetUpdateInfoAsync();
                getUpdateInfoAsync.completed += CheckUpdateVersion;

                UIHotUpdate.Instance.SetText("UpdateInfo");
                request = getUpdateInfoAsync;
            }
        }
        private void CheckUpdateVersion(Request request)
        {
            var getUpdateInfoAsync = request as GetUpdateInfoRequest;
            if (getUpdateInfoAsync.result == Request.Result.Success)
            {
                var updateVersion = System.Version.Parse(getUpdateInfoAsync.info.version);
                var playerVersion = System.Version.Parse(Assets.PlayerAssets.version);
                if (updateVersion.Minor != playerVersion.Minor)
                {
                    Application.OpenURL(getUpdateInfoAsync.info.playerDownloadURL);
                    Application.Quit();
                    return;
                }
                var getVersionsAsync = Assets.GetVersionsAsync(getUpdateInfoAsync.info);
                getVersionsAsync.completed += CheckDownloadInfo;

                UIHotUpdate.Instance.SetText("Versions");
                this.request = getVersionsAsync;
            }
            else
            {
                UIHotUpdate.Instance.OpenMessageBox("Tips", "Retry", CheckUpdateInfo);
            }
        }
        private void CheckDownloadInfo(Request request)
        {
            var getVersionsAsync = request as VersionsRequest;
            if (getVersionsAsync.result == Request.Result.Success)
            {
                versions = getVersionsAsync.versions;
                var getDownloadSizeAsync = Assets.GetDownloadSizeAsync(versions);
                getDownloadSizeAsync.completed += StartDownload;

                UIHotUpdate.Instance.SetText("DownloadInfo");
                this.request = getDownloadSizeAsync;
            }
            else
            {
                UIHotUpdate.Instance.OpenMessageBox("Tips", "Retry", CheckUpdateInfo);
            }
        }
        private void StartDownload(Request request)
        {
            var getDownloadSizeAsync = request as GetDownloadSizeRequest;
            if (getDownloadSizeAsync.downloadSize > 0)
            {
                var downloadSize = Utility.FormatBytes(getDownloadSizeAsync.downloadSize);
                UIHotUpdate.Instance.OpenMessageBox("Tips", downloadSize, StartDownload);
            }
            else
            {
                UpdateFinish_LoadHotAssembly();
            }
        }
        private void StartDownload()
        {
            var getDownloadSizeAsync = request as GetDownloadSizeRequest;
            var downloadAsync = getDownloadSizeAsync.DownloadAsync();
            var downloadRequestBatch = downloadAsync as DownloadRequestBatch;
            downloadRequestBatch.updated = Downloading;
            downloadRequestBatch.completed += RemoveUnusedFile;
            request = null;
        }
        private void Downloading(DownloadRequestBatch download)
        {
            var downloadedBytes = Utility.FormatBytes(download.downloadedBytes);
            var downloadSize = Utility.FormatBytes(download.downloadSize);
            var bandwidth = Utility.FormatBytes(download.bandwidth);

            UIHotUpdate.Instance.SetText($"Download：{downloadedBytes}/{downloadSize} {bandwidth}/s");
            UIHotUpdate.Instance.SetSlider(download.progress);
        }
        private void RemoveUnusedFile(DownloadRequestBatch download)
        {
            if (download.result == DownloadRequestBase.Result.Success)
            {
                var bundles = new HashSet<string>();
                foreach (var item in versions.data)
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

                UIHotUpdate.Instance.SetText("清理资源");
                request = removeAsync;
            }
            else
            {
                UIHotUpdate.Instance.OpenMessageBox("Tips", "Retry", download.Retry);
            }
        }
        private void SaveVersion(Request request)
        {
            Assets.Versions = versions;
            versions.Save(Assets.GetDownloadDataPath(Versions.Filename));
            UpdateFinish_LoadHotAssembly();
        }
        private void UpdateFinish_LoadHotAssembly()
        {
            UpdateManager.Instance.StopUpdate(updateId);
            UIHotUpdate.Instance.Finish();
#if UNITY_EDITOR
            HotAssembly.GameStart.Instance.Init();
#else
            AssetManager.Instance.Load<TextAsset>("HotAssembly", StartHotAssembly);
#endif
        }
        private void StartHotAssembly(int id, Object asset)
        {
            AssetManager.Instance.Unload(id);
            if (asset == null) return;
            byte[] bytes = ((TextAsset)asset).bytes;
            if (bytes == null) return;
            hotAssembly = Assembly.Load(bytes);
            Type t = hotAssembly.GetType("HotAssembly.GameStart");
            PropertyInfo p = t.BaseType.GetProperty("Instance");
            object o = p.GetMethod.Invoke(null, null);
            MethodInfo m = t.GetMethod("Init");
            m.Invoke(o, null);
        }
    }
}