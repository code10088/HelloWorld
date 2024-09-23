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
#if UNITY_EDITOR && !HotUpdateDebug
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
            downloaderOperation.OnDownloadOverCallback = CheckDownloadHotUpdateRes;
            downloaderOperation.OnDownloadProgressCallback = Downloading;
            downloaderOperation.OnDownloadErrorCallback = DownloadError;
            downloaderOperation.BeginDownload();
        }
        private void CheckDownloadHotUpdateRes(bool isSucceed)
        {
            if (isSucceed)
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
        private void Downloading(int totalDownloadCount, int currentDownloadCount, long totalDownloadBytes, long currentDownloadBytes)
        {
            var hotUpdateRes = UIManager.Instance.GetUI(UIType.UIHotUpdateRes) as UIHotUpdateRes;
            hotUpdateRes.SetText($"HotUpdateRes：{currentDownloadBytes}/{totalDownloadBytes}");
            hotUpdateRes.SetSlider(currentDownloadBytes/totalDownloadBytes);
        }
        private void DownloadError(string fileName, string error)
        {
            GameDebug.LogError($"DownloadError：{fileName}:{error}");
        }
        private void UpdateFinish()
        {
            downloaderOperation = null;
            hotUpdateResFinish?.Invoke();
            hotUpdateResFinish = null;
        }
    }
}
