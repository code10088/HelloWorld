using cfg;
using System;
using UnityEngine;
using xasset;

namespace HotAssembly
{
    public class HotUpdateResData : Database
    {
        private Action hotUpdateResFinish;
        private DownloadRequestBatch downloadRequestBatch;
        private int timerId = -1;

        public void Clear() { }
        public void StartUpdate(Action finish)
        {
            hotUpdateResFinish = finish;
            if (Assets.RealtimeMode) UIManager.Instance.OpenUI(UIType.UIHotUpdateRes, CheckUpdateInfo);
            else UpdateFinish();
        }
        private void CheckUpdateInfo(bool success = true)
        {
            var getDownloadSizeAsync = Assets.Versions.GetDownloadSizeAsync();
            getDownloadSizeAsync.completed += StartDownload;
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
            downloadRequestBatch = downloadAsync as DownloadRequestBatch;
            downloadRequestBatch.completed += UpdateFinish;
            timerId = TimeManager.Instance.StartTimer(0, 1, Downloading);
        }
        private void Downloading(float t)
        {
            var downloadedBytes = Utility.FormatBytes(downloadRequestBatch.downloadedBytes);
            var downloadSize = Utility.FormatBytes(downloadRequestBatch.downloadSize);
            var bandwidth = Utility.FormatBytes(downloadRequestBatch.bandwidth);

            var hotUpdateRes = UIManager.Instance.GetUI(UIType.UIHotUpdateRes) as UIHotUpdateRes;
            hotUpdateRes.SetText($"Download£º{downloadedBytes}/{downloadSize} {bandwidth}/s");
            hotUpdateRes.SetSlider(downloadRequestBatch.progress);
        }
        private void UpdateFinish(DownloadRequestBatch download)
        {
            TimeManager.Instance.StopTimer(timerId);
            if (download.result == DownloadRequestBase.Result.Success)
            {
                UpdateFinish();
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
        private void UpdateFinish()
        {
            UIManager.Instance.CloseUI(UIType.UIHotUpdateRes);
            hotUpdateResFinish?.Invoke();
            hotUpdateResFinish = null;
        }
    }
}
