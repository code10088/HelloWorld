using cfg;
using System;
using YooAsset;

namespace HotAssembly
{
    public class HotUpdateResData : DataBase
    {
        private Action hotUpdateResFinish;
        private ResourceDownloaderOperation downloaderOperation;

        public void Clear() { }
        public void StartUpdate(Action finish)
        {
            hotUpdateResFinish = finish;
#if (UNITY_EDITOR || UNITY_WEBGL) && !HotUpdateDebug
            UpdateFinish();
#else
            CheckDownloadHotUpdateRes();
#endif
        }
        private void CheckDownloadHotUpdateRes()
        {
            downloaderOperation = AssetManager.Package.CreateResourceDownloader(GameSetting.downloadLimit, GameSetting.retryTime, GameSetting.timeoutS);
            if (downloaderOperation.TotalDownloadBytes > 0)
            {
                string content = $"更新资源大小 {downloaderOperation.TotalDownloadBytes/1024f} Kb";
                UICommonBoxParam param = new UICommonBoxParam();
                param.type = UICommonBoxType.Sure;
                param.title = "Tips";
                param.content = content;
                param.sure = a => StartDownload();
                UICommonBox.OpenCommonBox(param);
            }
            else
            {
                downloaderOperation.CancelDownload();
                UpdateFinish();
            }
        }
        private void StartDownload()
        {
            downloaderOperation.DownloadFinishCallback = CheckDownloadHotUpdateRes;
            downloaderOperation.DownloadUpdateCallback = Downloading;
            downloaderOperation.DownloadErrorCallback = DownloadError;
            downloaderOperation.BeginDownload();
        }
        private void CheckDownloadHotUpdateRes(DownloaderFinishData data)
        {
            if (data.Succeed)
            {
                UpdateFinish();
            }
            else
            {
                UICommonBoxParam param = new UICommonBoxParam();
                param.type = UICommonBoxType.Sure;
                param.title = "Tips";
                param.content = "网络异常请重试";
                param.sure = a => CheckDownloadHotUpdateRes();
                UICommonBox.OpenCommonBox(param);
            }
        }
        private void Downloading(DownloadUpdateData data)
        {
            var hotUpdateRes = UIManager.Instance.GetUI(UIType.UIHotUpdateRes) as UIHotUpdateRes;
            hotUpdateRes.SetText($"HotUpdateRes：{data.CurrentDownloadBytes}/{data.TotalDownloadBytes}");
            hotUpdateRes.SetSlider(data.Progress);
        }
        private void DownloadError(DownloadErrorData data)
        {
            GameDebug.LogError($"DownloadError：{data.FileName}:{data.ErrorInfo}");
        }
        private void UpdateFinish()
        {
            downloaderOperation = null;
            hotUpdateResFinish?.Invoke();
            hotUpdateResFinish = null;
        }
    }
}
