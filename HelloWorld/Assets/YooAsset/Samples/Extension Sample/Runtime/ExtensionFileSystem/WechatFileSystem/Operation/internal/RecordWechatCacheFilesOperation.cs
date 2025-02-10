#if UNITY_WEBGL && WEIXINMINIGAME
using YooAsset;
using WeChatWASM;
using System.IO;

internal class RecordWechatCacheFilesOperation : AsyncOperationBase
{
    private enum ESteps
    {
        None,
        RecordCacheFiles,
        WaitResponse,
        Done,
    }

    private readonly WechatFileSystem _fileSystem;
    private ESteps _steps = ESteps.None;

    public RecordWechatCacheFilesOperation(WechatFileSystem fileSystem)
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
            var getSavedFileListOption = new GetSavedFileListOption();
            getSavedFileListOption.success = (GetSavedFileListSuccessCallbackResult response) =>
            {
                _steps = ESteps.Done;
                Status = EOperationStatus.Succeed;
                foreach (var fileInfo in response.fileList)
                {
                    //TODO 需要确认存储文件为Bundle文件
                    _fileSystem.RecordBundleFile(fileInfo.filePath);
                }
            };
            getSavedFileListOption.fail = (FileError fileError) =>
            {
                _steps = ESteps.Done;
                Status = EOperationStatus.Failed;
                Error = fileError.errMsg;
            };
            fileSystemMgr.GetSavedFileList(getSavedFileListOption);
        }
    }
}
#endif