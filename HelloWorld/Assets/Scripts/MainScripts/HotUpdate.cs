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

        public void Start()
        {
            CheckUpdateInfo();
        }
        private void CheckUpdateInfo()
        {
            if (Assets.SimulationMode)
            {
                UpdateFinish_LoadHotAssembly();
            }
            else
            {
                Assets.UpdateInfoURL = "http://192.168.101.51/BundlesCache/Android/updateinfo.json";
                var getUpdateInfoAsync = Assets.GetUpdateInfoAsync();
                getUpdateInfoAsync.completed += CheckUpdateVersion;
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
            }
            else
            {
                //重试按钮
                CheckUpdateInfo();
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
            }
            else
            {
                //重试按钮
                CheckUpdateInfo();
            }
        }
        private void StartDownload(Request request)
        {
            var getDownloadSizeAsync = request as GetDownloadSizeRequest;
            if (getDownloadSizeAsync.downloadSize > 0)
            {
                var downloadSize = Utility.FormatBytes(getDownloadSizeAsync.downloadSize);
                //确定下载
                var downloadAsync = getDownloadSizeAsync.DownloadAsync();
                var downloadRequestBatch = downloadAsync as DownloadRequestBatch;
                downloadRequestBatch.updated = Downloading;
                downloadRequestBatch.completed += RemoveUnusedFile;
            }
            else
            {
                UpdateFinish_LoadHotAssembly();
            }
        }
        private void Downloading(DownloadRequestBatch download)
        {
            var downloadedBytes = Utility.FormatBytes(download.downloadedBytes);
            var downloadSize = Utility.FormatBytes(download.downloadSize);
            var bandwidth = Utility.FormatBytes(download.bandwidth);

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
            }
            else
            {
                //重试按钮
                download.Retry();
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
#if UNITY_EDITOR
            HotAssembly.GameStart.Instance.Init();
#else
            AssetManager.Instance.Load<TextAsset>("HotAssembly", StartHotAssembly);
#endif
        }
        private void StartHotAssembly(int id, Object asset, object param = null)
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