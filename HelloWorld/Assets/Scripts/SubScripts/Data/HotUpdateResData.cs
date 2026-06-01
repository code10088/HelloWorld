using cfg;
using System;
using YooAsset;

public class HotUpdateResData : DataBase
{
    private Action hotUpdateResFinish;
    private ResourceDownloaderOperation downloaderOperation;

    public void Init()
    {

    }
    public void Clear()
    {

    }

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
        var options = new ResourceDownloaderOptions(GameSetting.downloadLimit, GameSetting.retryTime);
        downloaderOperation = AssetManager.Instance.Package.CreateResourceDownloader(options);
        if (downloaderOperation.TotalDownloadBytes > 0)
        {
            string content = $"更新资源大小 {downloaderOperation.TotalDownloadBytes / 1024f} Kb";
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
        downloaderOperation.DownloadCompleted += CheckDownloadHotUpdateRes;
        downloaderOperation.DownloadProgressChanged += Downloading;
        downloaderOperation.DownloadError += DownloadError;
        downloaderOperation.StartDownload();
    }
    private void CheckDownloadHotUpdateRes(DownloadCompletedEventArgs args)
    {
        if (args.Succeeded)
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
    private void Downloading(DownloadProgressChangedEventArgs args)
    {
        var hotUpdateRes = UIManager.Instance.GetUI(UIType.UIHotUpdateRes) as UIHotUpdateRes;
        hotUpdateRes.SetText($"HotUpdateRes：{args.CurrentDownloadBytes}/{args.TotalDownloadBytes}");
        hotUpdateRes.SetSlider(args.Progress);
    }
    private void DownloadError(DownloadErrorEventArgs args)
    {
        GameDebug.LogError($"DownloadError：{args.FileName}:{args.ErrorInfo}");
    }
    private void UpdateFinish()
    {
        downloaderOperation = null;
        hotUpdateResFinish?.Invoke();
        hotUpdateResFinish = null;
    }
}