
namespace YooAsset
{
    internal abstract class FSClearCacheBundleFilesOperation : AsyncOperationBase
    {
    }

    internal sealed class FSClearCacheBundleFilesCompleteOperation : FSClearCacheBundleFilesOperation
    {
        private readonly string _error;

        internal FSClearCacheBundleFilesCompleteOperation(string error)
        {
            _error = error;
        }
        internal override void InternalOnStart()
        {
            if (string.IsNullOrEmpty(_error))
            {
                Status = EOperationStatus.Succeed;
            }
            else
            {
                Status = EOperationStatus.Failed;
                Error = _error;
            }
        }
        internal override void InternalOnUpdate()
        {
        }
    }
}