
namespace YooAsset
{
    /// <summary>
    /// 文件清理方式
    /// </summary>
    public enum EFileClearMode
    {
        /// <summary>
        /// 清理所有文件
        /// </summary>
        ClearAllBundleFiles = 1,

        /// <summary>
        /// 清理未在使用的文件
        /// </summary>
        ClearUnusedBundleFiles = 2,

        /// <summary>
        /// 清理指定标签的文件
        /// 说明：需要指定参数，可选：string, string[], List<string>
        /// </summary>
        ClearBundleFilesByTags = 3,
    }
}