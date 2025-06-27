
namespace YooAsset
{
    public struct LocalFileInfo
    {
        /// <summary>
        /// 包裹名称
        /// </summary>
        public string PackageName;

        /// <summary>
        /// 资源包名称
        /// </summary>
        public string BundleName;

        /// <summary>
        /// 源文件请求地址
        /// </summary>
        public string SourceFileURL;
    }

    /// <summary>
    /// 本地文件拷贝服务类
    /// </summary>
    public interface ICopyLocalFileServices
    {
        void CopyFile(LocalFileInfo sourceFileInfo, string destFilePath);
    }
}