
namespace YooAsset
{
    /// <summary>
    /// 拷贝内置文件服务类
    /// </summary>
    public interface ICopyBuildinBundleServices
    {
        void CopyBuildinFile(string buildinFileURL, string destFilePath);
    }
}