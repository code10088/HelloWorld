using System;
using System.Collections.Generic;
using UnityEngine;
using xasset;

namespace HotAssembly
{
    public class HotUpdateResData : Database
    {
        private UIHotUpdateRes hotUpdateRes;
        private Action hotUpdateResFinish;
        private Versions versions;
        private int updateId = -1;
        private Request request;


        public void StartUpdate(Action finish)
        {
            hotUpdateResFinish = finish;
            hotUpdateRes = UIManager.Instance.GetUI(UIType.UIHotUpdateRes) as UIHotUpdateRes;
            updateId = Updater.Instance.StartUpdate(Update);
            CheckUpdateInfo();
        }
        public void Update()
        {
            if (request != null) hotUpdateRes.SetSlider(request.progress);
        }
        private void CheckUpdateInfo()
        {
            if (Assets.SimulationMode)
            {
                UpdateFinish();
            }
            else
            {
                Assets.UpdateInfoURL = "http://192.168.6.2/BundlesCache/Windows/updateinfo.json";
                var getUpdateInfoAsync = Assets.GetUpdateInfoAsync();
                getUpdateInfoAsync.completed += CheckUpdateVersion;

                hotUpdateRes.SetText("UpdateInfo");
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

                hotUpdateRes.SetText("Versions");
                this.request = getVersionsAsync;
            }
            else
            {
                UIMessageBoxParam param = new UIMessageBoxParam();
                param.type = UIMessageBoxType.SureAndCancel;
                param.title = "Tips";
                param.content = "Retry";
                param.sure = a => CheckUpdateInfo();
                param.cancel = a => Application.Quit();
                UIManager.Instance.OpenMessageBox(param);
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

                hotUpdateRes.SetText("DownloadInfo");
                this.request = getDownloadSizeAsync;
            }
            else
            {
                UIMessageBoxParam param = new UIMessageBoxParam();
                param.type = UIMessageBoxType.SureAndCancel;
                param.title = "Tips";
                param.content = "Retry";
                param.sure = a => CheckUpdateInfo();
                param.cancel = a => Application.Quit();
                UIManager.Instance.OpenMessageBox(param);
            }
        }
        private void StartDownload(Request request)
        {
            var getDownloadSizeAsync = request as GetDownloadSizeRequest;
            if (getDownloadSizeAsync.downloadSize > 0)
            {
                var downloadSize = Utility.FormatBytes(getDownloadSizeAsync.downloadSize);
                UIMessageBoxParam param = new UIMessageBoxParam();
                param.type = UIMessageBoxType.SureAndCancel;
                param.title = "Tips";
                param.content = downloadSize;
                param.sure = a => StartDownload();
                param.cancel = a => Application.Quit();
                UIManager.Instance.OpenMessageBox(param);
            }
            else
            {
                UpdateFinish();
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

            hotUpdateRes.SetText($"Download：{downloadedBytes}/{downloadSize} {bandwidth}/s");
            hotUpdateRes.SetSlider(download.progress);
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

                hotUpdateRes.SetText("清理资源");
                request = removeAsync;
            }
            else
            {
                UIMessageBoxParam param = new UIMessageBoxParam();
                param.type = UIMessageBoxType.SureAndCancel;
                param.title = "Tips";
                param.content = "Retry";
                param.sure = a => download.Retry();
                param.cancel = a => Application.Quit();
                UIManager.Instance.OpenMessageBox(param);
            }
        }
        private void SaveVersion(Request request)
        {
            Assets.Versions = versions;
            versions.Save(Assets.GetDownloadDataPath(Versions.Filename));
            UpdateFinish();
        }
        private void UpdateFinish()
        {
            UIManager.Instance.CloseUI(UIType.UIHotUpdateRes);
            Updater.Instance.StopUpdate(updateId);
            hotUpdateResFinish?.Invoke();
        }
        public void Clear()
        {
            
        }
    }
}
