﻿#if UNITY_WEBGL && DOUYINMINIGAME
using YooAsset;

internal class RequestTiktokPackageVersionOperation : AsyncOperationBase
{
    private enum ESteps
    {
        None,
        RequestPackageVersion,
        Done,
    }

    private readonly TiktokFileSystem _fileSystem;
    private readonly bool _appendTimeTicks;
    private readonly int _timeout;
    private UnityWebTextRequestOperation _webTextRequestOp;
    private int _requestCount = 0;
    private ESteps _steps = ESteps.None;

    /// <summary>
    /// 包裹版本
    /// </summary>
    public string PackageVersion { private set; get; }


    public RequestTiktokPackageVersionOperation(TiktokFileSystem fileSystem, bool appendTimeTicks, int timeout)
    {
        _fileSystem = fileSystem;
        _appendTimeTicks = appendTimeTicks;
        _timeout = timeout;
    }
    internal override void InternalStart()
    {
        _requestCount = WebRequestCounter.GetRequestFailedCount(_fileSystem.PackageName, nameof(RequestTiktokPackageVersionOperation));
        _steps = ESteps.RequestPackageVersion;
    }
    internal override void InternalUpdate()
    {
        if (_steps == ESteps.None || _steps == ESteps.Done)
            return;

        if (_steps == ESteps.RequestPackageVersion)
        {
            if (_webTextRequestOp == null)
            {
                string fileName = YooAssetSettingsData.GetPackageVersionFileName(_fileSystem.PackageName);
                string url = GetRequestURL(fileName);
                _webTextRequestOp = new UnityWebTextRequestOperation(url, _timeout);
                _webTextRequestOp.StartOperation();
                AddChildOperation(_webTextRequestOp);
            }

            _webTextRequestOp.UpdateOperation();
            Progress = _webTextRequestOp.Progress;
            if (_webTextRequestOp.IsDone == false)
                return;

            if (_webTextRequestOp.Status == EOperationStatus.Succeed)
            {
                PackageVersion = _webTextRequestOp.Result;
                if (string.IsNullOrEmpty(PackageVersion))
                {
                    _steps = ESteps.Done;
                    Status = EOperationStatus.Failed;
                    Error = $"Web package version file content is empty !";
                }
                else
                {
                    _steps = ESteps.Done;
                    Status = EOperationStatus.Succeed;
                }
            }
            else
            {
                _steps = ESteps.Done;
                Status = EOperationStatus.Failed;
                Error = _webTextRequestOp.Error;
                WebRequestCounter.RecordRequestFailed(_fileSystem.PackageName, nameof(RequestTiktokPackageVersionOperation));
            }
        }
    }

    private string GetRequestURL(string fileName)
    {
        string url;

        // 轮流返回请求地址
        if (_requestCount % 2 == 0)
            url = _fileSystem.RemoteServices.GetRemoteMainURL(fileName);
        else
            url = _fileSystem.RemoteServices.GetRemoteFallbackURL(fileName);

        // 在URL末尾添加时间戳
        if (_appendTimeTicks)
            return $"{url}?{System.DateTime.UtcNow.Ticks}";
        else
            return url;
    }
}
#endif