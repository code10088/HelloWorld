using System.Collections.Generic;

namespace YooAsset
{
    internal interface ICacheSystem
    {
        /// <summary>
        /// 获取缓存文件的根目录
        /// </summary>
        string GetCacheFileRoot();

        /// <summary>
        /// 获取临时缓存文件路径
        /// </summary>
        string GetTempFilePath(PackageBundle bundle);

        /// <summary>
        /// 获取数据文件路径
        /// </summary>
        string GetDataFilePath(PackageBundle bundle);

        /// <summary>
        /// 获取信息文件路径
        /// </summary>
        string GetInfoFilePath(PackageBundle bundle);

        /// <summary>
        /// 获取所有缓存文件的GUID
        /// </summary>
        List<string> GetAllCachedBundleGUIDs();

        /// <summary>
        /// 是否记录了文件
        /// </summary>
        bool IsRecordFile(string bundleGUID);

        /// <summary>
        /// 记录指定文件
        /// </summary>
        bool RecordFile(string bundleGUID, CacheWrapper wrapper);

        /// <summary>
        /// 验证缓存文件
        /// </summary>
        EFileVerifyResult VerifyCacheFile(PackageBundle bundle);

        /// <summary>
        /// 写入缓存文件
        /// </summary>
        bool WriteCacheFile(PackageBundle bundle, string copyPath);

        /// <summary>
        /// 删除缓存文件
        /// </summary>
        bool DeleteCacheFile(string bundleGUID);

        /// <summary>
        /// 写入文件信息
        /// </summary>
        void WriteInfoFile(string filePath, string dataFileCRC, long dataFileSize);

        /// <summary>
        /// 读取文件信息
        /// </summary>
        void ReadInfoFile(string filePath, out string dataFileCRC, out long dataFileSize);
    }
}