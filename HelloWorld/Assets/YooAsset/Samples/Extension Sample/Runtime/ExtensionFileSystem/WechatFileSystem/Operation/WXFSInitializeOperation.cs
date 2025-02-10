#if UNITY_WEBGL && WEIXINMINIGAME
using YooAsset;

internal partial class WXFSInitializeOperation : FSInitializeFileSystemOperation
{
    private enum ESteps
    {
        None,
        RecordCacheFiles,
        Done,
    }

    private readonly WechatFileSystem _fileSystem;
    private RecordWechatCacheFilesOperation _recordWechatCacheFilesOp;
    private ESteps _steps = ESteps.None;

    public WXFSInitializeOperation(WechatFileSystem fileSystem)
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
            if (_recordWechatCacheFilesOp == null)
            {
                _recordWechatCacheFilesOp = new RecordWechatCacheFilesOperation(_fileSystem);
                OperationSystem.StartOperation(_fileSystem.PackageName, _recordWechatCacheFilesOp);
            }

            if (_recordWechatCacheFilesOp.IsDone == false)
                return;

            if (_recordWechatCacheFilesOp.Status == EOperationStatus.Succeed)
            {
                _steps = ESteps.Done;
                Status = EOperationStatus.Succeed;
            }
            else
            {
                _steps = ESteps.Done;
                Status = EOperationStatus.Failed;
                Error = _recordWechatCacheFilesOp.Error;
            }
        }
    }
}
#endif