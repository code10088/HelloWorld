#if UNITY_WEBGL && DOUYINMINIGAME
using YooAsset;
using TTSDK;

internal class RecordTiktokCacheFilesOperation : AsyncOperationBase
{
    private enum ESteps
    {
        None,
        RecordCacheFiles,
        WaitResponse,
        Done,
    }

    private readonly TiktokFileSystem _fileSystem;
    private ESteps _steps = ESteps.None;

    public RecordTiktokCacheFilesOperation(TiktokFileSystem fileSystem)
    {
        _fileSystem = fileSystem;
    }
    internal override void InternalOnStart()
    {
        _steps = ESteps.RecordCacheFiles;
    }
    internal override void InternalOnUpdate()
    {
        if (_steps == ESteps.None || _steps == ESteps.Done)
            return;

        if (_steps == ESteps.RecordCacheFiles)
        {
            _steps = ESteps.WaitResponse;

            var fileSystemMgr = _fileSystem.GetFileSystemMgr();
            var getSavedFileListParam = new GetSavedFileListParam();
            getSavedFileListParam.success = (TTGetSavedFileListResponse response) =>
            {
                _steps = ESteps.Done;
                Status = EOperationStatus.Succeed;
                foreach (var fileInfo in response.fileList)
                {
                    //TODO 需要确认存储文件为Bundle文件
                    _fileSystem.RecordBundleFile(fileInfo.filePath);
                }
            };
            getSavedFileListParam.fail = (TTGetSavedFileListResponse response) =>
            {
                _steps = ESteps.Done;
                Status = EOperationStatus.Failed;
                Error = response.errMsg;
            };
            fileSystemMgr.GetSavedFileList(getSavedFileListParam);
        }
    }
}
#endif