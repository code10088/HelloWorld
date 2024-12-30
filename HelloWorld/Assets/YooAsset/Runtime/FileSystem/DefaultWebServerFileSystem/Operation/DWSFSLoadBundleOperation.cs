
namespace YooAsset
{
    internal class DWSFSLoadAssetBundleOperation : FSLoadBundleOperation
    {
        private enum ESteps
        {
            None,
            DownloadFile,
            Done,
        }

        private readonly DefaultWebServerFileSystem _fileSystem;
        private readonly PackageBundle _bundle;
        private DownloadHandlerAssetBundleOperation _downloadhanlderAssetBundleOp;
        private ESteps _steps = ESteps.None;


        internal DWSFSLoadAssetBundleOperation(DefaultWebServerFileSystem fileSystem, PackageBundle bundle)
        {
            _fileSystem = fileSystem;
            _bundle = bundle;
        }
        internal override void InternalOnStart()
        {
            _steps = ESteps.DownloadFile;
        }
        internal override void InternalOnUpdate()
        {
            if (_steps == ESteps.None || _steps == ESteps.Done)
                return;

            if (_steps == ESteps.DownloadFile)
            {
                if (_downloadhanlderAssetBundleOp == null)
                {
                    DownloadParam downloadParam = new DownloadParam(int.MaxValue, 60);
                    string fileLoadPath = _fileSystem.GetWebFileLoadPath(_bundle);
                    downloadParam.MainURL = DownloadSystemHelper.ConvertToWWWPath(fileLoadPath);
                    downloadParam.FallbackURL = downloadParam.MainURL;
                    _downloadhanlderAssetBundleOp = new DownloadHandlerAssetBundleOperation(_fileSystem.DisableUnityWebCache, _bundle, downloadParam);
                    OperationSystem.StartOperation(_fileSystem.PackageName, _downloadhanlderAssetBundleOp);
                }

                DownloadProgress = _downloadhanlderAssetBundleOp.DownloadProgress;
                DownloadedBytes = _downloadhanlderAssetBundleOp.DownloadedBytes;
                Progress = _downloadhanlderAssetBundleOp.Progress;
                if (_downloadhanlderAssetBundleOp.IsDone == false)
                    return;

                if (_downloadhanlderAssetBundleOp.Status == EOperationStatus.Succeed)
                {
                    var assetBundle = _downloadhanlderAssetBundleOp.Result;
                    if (assetBundle == null)
                    {
                        _steps = ESteps.Done;
                        Status = EOperationStatus.Failed;
                        Error = $"{nameof(DownloadHandlerAssetBundleOperation)} loaded asset bundle is null !";
                    }
                    else
                    {
                        _steps = ESteps.Done;
                        Result = new AssetBundleResult(_fileSystem, _bundle, assetBundle, null);
                        Status = EOperationStatus.Succeed;
                    }
                }
                else
                {
                    _steps = ESteps.Done;
                    Status = EOperationStatus.Failed;
                    Error = _downloadhanlderAssetBundleOp.Error;
                }
            }
        }
        internal override void InternalWaitForAsyncComplete()
        {
            if (_steps != ESteps.Done)
            {
                _steps = ESteps.Done;
                Status = EOperationStatus.Failed;
                Error = "WebGL platform not support sync load method !";
                UnityEngine.Debug.LogError(Error);
            }
        }
        public override void AbortDownloadOperation()
        {
            if (_steps == ESteps.DownloadFile)
            {
                if (_downloadhanlderAssetBundleOp != null)
                    _downloadhanlderAssetBundleOp.SetAbort();
            }
        }
    }
}