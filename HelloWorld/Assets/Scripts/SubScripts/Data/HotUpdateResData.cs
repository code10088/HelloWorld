using System;
using UnityEngine;
using xasset;

namespace HotAssembly
{
    public class HotUpdateResData : Database
    {
        private Action hotUpdateResFinish;

        public void Clear() { }
        public void StartUpdate(Action finish)
        {
            hotUpdateResFinish = finish;
            if (Assets.SimulationMode) UpdateFinish();
            else UIManager.Instance.OpenUI(UIType.UIHotUpdateRes, CheckUpdateInfo);
        }
        private void CheckUpdateInfo(UIBase ui)
        {
            var getDownloadSizeAsync = Assets.GetDownloadSizeAsync(Assets.Versions);
            getDownloadSizeAsync.completed += StartDownload;

            var hotUpdateRes = ui as UIHotUpdateRes;
            hotUpdateRes.SetText("DownloadInfo");
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
                param.sure = a => StartDownload(getDownloadSizeAsync);
                param.cancel = a => Application.Quit();
                UIManager.Instance.OpenMessageBox(param);
            }
            else
            {
                UpdateFinish();
            }
        }
        private void StartDownload(GetDownloadSizeRequest getDownloadSizeAsync)
        {
            var downloadAsync = getDownloadSizeAsync.DownloadAsync();
            var downloadRequestBatch = downloadAsync as DownloadRequestBatch;
            downloadRequestBatch.updated = Downloading;
            downloadRequestBatch.completed += a => UpdateFinish();
        }
        private void Downloading(DownloadRequestBatch download)
        {
            var downloadedBytes = Utility.FormatBytes(download.downloadedBytes);
            var downloadSize = Utility.FormatBytes(download.downloadSize);
            var bandwidth = Utility.FormatBytes(download.bandwidth);

            var hotUpdateRes = UIManager.Instance.GetUI(UIType.UIHotUpdateRes) as UIHotUpdateRes;
            hotUpdateRes.SetText($"Download£º{downloadedBytes}/{downloadSize} {bandwidth}/s");
            hotUpdateRes.SetSlider(download.progress);
        }
        private void UpdateFinish()
        {
            UIManager.Instance.CloseUI(UIType.UIHotUpdateRes);
            hotUpdateResFinish?.Invoke();
        }
    }
}
