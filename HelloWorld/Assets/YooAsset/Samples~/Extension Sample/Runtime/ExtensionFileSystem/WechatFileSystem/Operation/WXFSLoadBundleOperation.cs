#if UNITY_WEBGL && WEIXINMINIGAME
using UnityEngine;
using UnityEngine.Networking;
using YooAsset;
using WeChatWASM;

internal class WXFSLoadBundleOperation : FSLoadBundleOperation
{
    private enum ESteps
    {
        None,
        LoadBundleFile,
        Done,
    }

    private readonly WechatFileSystem _fileSystem;
    private readonly PackageBundle _bundle;
    private UnityWebRequest _webRequest;
    private ESteps _steps = ESteps.None;

    internal WXFSLoadBundleOperation(WechatFileSystem fileSystem, PackageBundle bundle)
    {
        _fileSystem = fileSystem;
        _bundle = bundle;
    }
    internal override void InternalOnStart()
    {
        _steps = ESteps.LoadBundleFile;
    }
    internal override void InternalOnUpdate()
    {
        if (_steps == ESteps.None || _steps == ESteps.Done)
            return;

        if (_steps == ESteps.LoadBundleFile)
        {
            if (_webRequest == null)
            {
                string mainURL = _fileSystem.RemoteServices.GetRemoteMainURL(_bundle.FileName);
                _webRequest = WXAssetBundle.GetAssetBundle(mainURL);
                _webRequest.SendWebRequest();
            }

            DownloadProgress = _webRequest.downloadProgress;
            DownloadedBytes = (long)_webRequest.downloadedBytes;
            Progress = DownloadProgress;
            if (_webRequest.isDone == false)
                return;

            if (CheckRequestResult())
            {
                if (_bundle.Encrypted && _fileSystem.DecryptionServices == null)
                {
                    _steps = ESteps.Done;
                    Status = EOperationStatus.Failed;
                    Error = $"The {nameof(IWebDecryptionServices)} is null !";
                    YooLogger.Error(Error);
                    return;
                }

                AssetBundle assetBundle;
                var downloadHanlder = _webRequest.downloadHandler as DownloadHandlerWXAssetBundle;
                if (_bundle.Encrypted)
                    assetBundle = _fileSystem.LoadEncryptedAssetBundle(_bundle, downloadHanlder.data);
                else
                    assetBundle = downloadHanlder.assetBundle;

                if (assetBundle == null)
                {
                    _steps = ESteps.Done;
                    Error = $"{nameof(DownloadHandlerWXAssetBundle)} loaded asset bundle is null !";
                    Status = EOperationStatus.Failed;
                }
                else
                {
                    _steps = ESteps.Done;
                    Result = new WXAssetBundleResult(_fileSystem, _bundle, assetBundle);
                    Status = EOperationStatus.Succeed;

                    //TODO 解决微信小游戏插件问题
                    // Issue : https://github.com/wechat-miniprogram/minigame-unity-webgl-transform/issues/108#
                    DownloadProgress = 1f;
                    DownloadedBytes = _bundle.FileSize;
                    Progress = 1f;
                }
            }
            else
            {
                _steps = ESteps.Done;
                Status = EOperationStatus.Failed;
            }
        }
    }
    internal override void InternalWaitForAsyncComplete()
    {
        if (_steps != ESteps.Done)
        {
            _steps = ESteps.Done;
            Status = EOperationStatus.Failed;
            Error = "Wechat platform not support sync load method !";
            UnityEngine.Debug.LogError(Error);
        }
    }
    public override void AbortDownloadOperation()
    {
    }

    private bool CheckRequestResult()
    {
#if UNITY_2020_3_OR_NEWER
        if (_webRequest.result != UnityWebRequest.Result.Success)
        {
            Error = _webRequest.error;
            return false;
        }
        else
        {
            return true;
        }
#else
        if (_webRequest.isNetworkError || _webRequest.isHttpError)
        {
            Error = _webRequest.error;
            return false;
        }
        else
        {
            return true;
        }
#endif
    }
}
#endif