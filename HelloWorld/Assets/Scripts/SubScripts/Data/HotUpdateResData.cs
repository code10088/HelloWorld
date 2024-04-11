using cfg;
using System;
using YooAsset;

namespace HotAssembly
{
    public class HotUpdateResData : Database
    {
        private Action hotUpdateResFinish;
        private ResourceDownloaderOperation downloaderOperation;
        private int timerId = -1;

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
            downloaderOperation.Completed += CheckDownloadHotUpdateRes;
            downloaderOperation.BeginDownload();

            timerId = TimeManager.Instance.StartTimer(0, 1, Downloading);
        }
        private void CheckDownloadHotUpdateRes(AsyncOperationBase o)
        {
            TimeManager.Instance.StopTimer(timerId);
            if (o.Status == EOperationStatus.Succeed)
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
        private void Downloading(float t)
        {
            var hotUpdateRes = UIManager.Instance.GetUI(UIType.UIHotUpdateRes) as UIHotUpdateRes;
            hotUpdateRes.SetText($"HotUpdateRes：{downloaderOperation.CurrentDownloadBytes}/{downloaderOperation.TotalDownloadBytes}");
            hotUpdateRes.SetSlider(downloaderOperation.Progress);
        }
        private void UpdateFinish()
        {
            downloaderOperation = null;
            hotUpdateResFinish?.Invoke();
            hotUpdateResFinish = null;
        }
    }
}
