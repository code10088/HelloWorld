
namespace YooAsset
{
    public abstract class ClearCacheBundleFilesOperation : AsyncOperationBase
    {
    }
    internal sealed class ClearCacheBundleFilesImplOperation : ClearCacheBundleFilesOperation
    {
        private enum ESteps
        {
            None,
            ClearFileSystemA,
            ClearFileSystemB,
            ClearFileSystemC,
            Done,
        }

        private readonly IPlayMode _impl;
        private readonly IFileSystem _fileSystemA;
        private readonly IFileSystem _fileSystemB;
        private readonly IFileSystem _fileSystemC;
        private readonly string _clearMode;
        private readonly object _clearParam;
        private FSClearCacheBundleFilesOperation _clearCacheBundleFilesOpA;
        private FSClearCacheBundleFilesOperation _clearCacheBundleFilesOpB;
        private FSClearCacheBundleFilesOperation _clearCacheBundleFilesOpC;
        private ESteps _steps = ESteps.None;
        
        internal ClearCacheBundleFilesImplOperation(IPlayMode impl, IFileSystem fileSystemA, IFileSystem fileSystemB, IFileSystem fileSystemC, string clearMode, object clearParam)
        {
            _impl = impl;
            _fileSystemA = fileSystemA;
            _fileSystemB = fileSystemB;
            _fileSystemC = fileSystemC;
            _clearMode = clearMode;
            _clearParam = clearParam;
        }
        internal override void InternalOnStart()
        {
            _steps = ESteps.ClearFileSystemA;
        }
        internal override void InternalOnUpdate()
        {
            if (_steps == ESteps.None || _steps == ESteps.Done)
                return;

            if (_steps == ESteps.ClearFileSystemA)
            {
                if (_fileSystemA == null)
                {
                    _steps = ESteps.ClearFileSystemB;
                    return;
                }

                if (_clearCacheBundleFilesOpA == null)
                    _clearCacheBundleFilesOpA = _fileSystemA.ClearCacheBundleFilesAsync(_impl.ActiveManifest, _clearMode, _clearParam);

                Progress = _clearCacheBundleFilesOpA.Progress;
                if (_clearCacheBundleFilesOpA.IsDone == false)
                    return;

                if (_clearCacheBundleFilesOpA.Status == EOperationStatus.Succeed)
                {
                    _steps = ESteps.ClearFileSystemB;
                }
                else
                {
                    _steps = ESteps.Done;
                    Status = EOperationStatus.Failed;
                    Error = _clearCacheBundleFilesOpA.Error;
                }
            }

            if (_steps == ESteps.ClearFileSystemB)
            {
                if (_fileSystemB == null)
                {
                    _steps = ESteps.ClearFileSystemC;
                    return;
                }

                if (_clearCacheBundleFilesOpB == null)
                    _clearCacheBundleFilesOpB = _fileSystemB.ClearCacheBundleFilesAsync(_impl.ActiveManifest, _clearMode, _clearParam);

                Progress = _clearCacheBundleFilesOpB.Progress;
                if (_clearCacheBundleFilesOpB.IsDone == false)
                    return;

                if (_clearCacheBundleFilesOpB.Status == EOperationStatus.Succeed)
                {
                    _steps = ESteps.ClearFileSystemC;
                }
                else
                {
                    _steps = ESteps.Done;
                    Status = EOperationStatus.Failed;
                    Error = _clearCacheBundleFilesOpB.Error;
                }
            }

            if (_steps == ESteps.ClearFileSystemC)
            {
                if (_fileSystemC == null)
                {
                    _steps = ESteps.Done;
                    Status = EOperationStatus.Succeed;
                    return;
                }

                if (_clearCacheBundleFilesOpC == null)
                    _clearCacheBundleFilesOpC = _fileSystemC.ClearCacheBundleFilesAsync(_impl.ActiveManifest, _clearMode, _clearParam);

                Progress = _clearCacheBundleFilesOpC.Progress;
                if (_clearCacheBundleFilesOpC.IsDone == false)
                    return;

                if (_clearCacheBundleFilesOpC.Status == EOperationStatus.Succeed)
                {
                    _steps = ESteps.Done;
                    Status = EOperationStatus.Succeed;
                }
                else
                {
                    _steps = ESteps.Done;
                    Status = EOperationStatus.Failed;
                    Error = _clearCacheBundleFilesOpC.Error;
                }
            }
        }
    }
}