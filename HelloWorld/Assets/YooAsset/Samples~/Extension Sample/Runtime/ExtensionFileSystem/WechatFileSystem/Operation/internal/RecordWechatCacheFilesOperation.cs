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
            var statOption = new WXStatOption();
            statOption.path = _fileSystem.FileRoot;
            statOption.recursive = true;
            statOption.success = (WXStatResponse response) =>
            {
                _steps = ESteps.Done;
                Status = EOperationStatus.Succeed;
                foreach (var fileStat in response.stats)
                {
                    //TODO 需要确认存储文件为Bundle文件
                    _fileSystem.RecordBundleFile(_fileSystem.FileRoot + fileStat.path);
                }
            };
            statOption.fail = (WXStatResponse response) =>
            {
                _steps = ESteps.Done;
                Status = EOperationStatus.Failed;
                Error = response.errMsg;
            };
            fileSystemMgr.Stat(statOption);
        }
    }
}
#endif