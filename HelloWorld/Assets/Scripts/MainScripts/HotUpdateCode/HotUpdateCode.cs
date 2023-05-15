using System;
using System.Collections.Generic;
using UnityEngine;
using xasset;

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
        private void CheckUpdateInfo()
        {
            Assets.UpdateInfoURL = "http://192.168.6.2/BundlesCache/Windows/updateinfo.json";
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
                    getVersionsAsync.completed += CheckDownloadInfo;

                    UIHotUpdateCode.Instance.SetText("CheckUpdateVersion");
                }
                else
                {
                    Application.OpenURL(getUpdateInfoAsync.info.playerDownloadURL);
                    Application.Quit();
                }
            }
            else
            {
                UIHotUpdateCode.Instance.OpenMessageBox("Tips", "Retry", CheckUpdateInfo);
            }
        }
        private void CheckDownloadInfo(Request request)
        {
            var getVersionsAsync = request as VersionsRequest;
            if (getVersionsAsync.result == Request.Result.Success)
            {
                latestVersion = getVersionsAsync.versions;
                //下载代码和更新界面资源
                UpdateFinish();
            }
            else
            {
                UIHotUpdateCode.Instance.OpenMessageBox("Tips", "Retry", CheckUpdateInfo);
            }
        }
        private void RemoveUnusedFile(DownloadRequestBatch download)
        {
            if (download.result == DownloadRequestBase.Result.Success)
            {
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
            }
            else
            {
                UIHotUpdateCode.Instance.OpenMessageBox("Tips", "Retry", download.Retry);
            }
        }
        private void SaveVersion(Request request)
        {
            string tempPath = Assets.GetDownloadDataPath(Versions.Filename);
            latestVersion.Save(tempPath);
            Assets.Versions = latestVersion;
            UpdateFinish();
        }
        private void UpdateFinish()
        {
            UIHotUpdateCode.Instance.Finish();
            hotUpdateCodeFinish?.Invoke();
        }
    }
}