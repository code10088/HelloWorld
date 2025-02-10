#if UNITY_WEBGL && DOUYINMINIGAME
using YooAsset;

internal partial class TTFSInitializeOperation : FSInitializeFileSystemOperation
{
    private enum ESteps
    {
        None,
        RecordCacheFiles,
        Done,
    }

    private readonly TiktokFileSystem _fileSystem;
    private RecordTiktokCacheFilesOperation _recordTiktokCacheFilesOp;
    private ESteps _steps = ESteps.None;

    public TTFSInitializeOperation(TiktokFileSystem fileSystem)
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
            if (_recordTiktokCacheFilesOp == null)
            {
                _recordTiktokCacheFilesOp = new RecordTiktokCacheFilesOperation(_fileSystem);
                OperationSystem.StartOperation(_fileSystem.PackageName, _recordTiktokCacheFilesOp);
            }

            if (_recordTiktokCacheFilesOp.IsDone == false)
                return;

            if (_recordTiktokCacheFilesOp.Status == EOperationStatus.Succeed)
            {
                _steps = ESteps.Done;
                Status = EOperationStatus.Succeed;
            }
            else
            {
                _steps = ESteps.Done;
                Status = EOperationStatus.Failed;
                Error = _recordTiktokCacheFilesOp.Error;
            }
        }
    }
}
#endif