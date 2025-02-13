using System.Runtime.InteropServices;

namespace TTSDK
{
    /**
     * 文件存储接口
     * 具体使用说明参考文档：https://bytedance.feishu.cn/docx/JpAMdacnaoyFDdx0fhXcUwL5nAe
     */
    public abstract class TTFileSystemManager
    {
        /// <summary>
        /// 用户数据存储的路径
        /// </summary>
        public const string USER_DATA_PATH = "scfile://user";

        /// <summary>
        /// 将字符串写入文件（同步）
        /// </summary>
        /// <param name="filePath">要写入的文件路径</param>
        /// <param name="data">要写入的文本</param>
        /// <param name="encoding">指定写入文件的字符编码</param>
        /// <returns>成功返回空字符串，失败返回错误信息</returns>
        public abstract string WriteFileSync(string filePath, string data, string encoding = "utf8");

        /// <summary>
        /// 将二进制写入文件（同步）
        /// </summary>
        /// <param name="filePath">要写入的文件路径</param>
        /// <param name="data">要写入的二进制数据</param>
        /// <returns>成功返回空字符串，失败返回错误信息</returns>
        public abstract string WriteFileSync(string filePath, byte[] data);

        /// <summary>
        /// 将二进制写入文件（异步）
        /// </summary>
        /// <param name="param"></param>
        public abstract void WriteFile(WriteFileParam param);

        /// <summary>
        /// 将字符串写入文件（异步）
        /// </summary>
        /// <param name="param"></param>
        public abstract void WriteFile(WriteFileStringParam param);

        /// <summary>
        /// 读取本地文件内容（异步）
        /// </summary>
        /// <param name="param"></param>
        public abstract void ReadFile(ReadFileParam param);

        /// <summary>
        /// 从本地文件读取二进制数据数据（同步）
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns>字节数据，读取失败返回null</returns>
        public abstract byte[] ReadFileSync(string filePath);

        /// <summary>
        /// 从本地文件读取字符串数据（同步）
        /// </summary>
        /// <param name="filePath">要读取的文件的路径</param>
        /// <param name="encoding">指定读取文件的字符编码, 不能为空</param>
        /// <returns>字符串数据，读取失败返回null</returns>
        public abstract string ReadFileSync(string filePath, string encoding);

        /// <summary>
        /// 判断文件/目录是否存在（同步）
        /// </summary>
        /// <param name="path">要判断是否存在的文件/目录路径</param>
        /// <returns>成功返回 true, 失败返回 false</returns>
        public abstract bool AccessSync(string path);

        /// <summary>
        /// 判断文件/目录是否存在（异步）
        /// </summary>
        /// <param name="param"></param>
        public abstract void Access(AccessParam param);

        /// <summary>
        /// 复制文件（同步） 
        /// </summary>
        /// <param name="srcPath">源文件路径</param>
        /// <param name="destPath">目标文件路径</param>
        /// <returns>成功返回空字符串，失败返回错误信息</returns>
        public abstract string CopyFileSync(string srcPath, string destPath);

        /// <summary>
        /// 复制文件（异步）
        /// </summary>
        /// <param name="param"></param>
        public abstract void CopyFile(CopyFileParam param);

        /// <summary>
        /// 重命名文件（异步）
        /// </summary>
        /// <param name="param"></param>
        public abstract void RenameFile(RenameFileParam param);

        /// <summary>
        /// 重命名文件（同步）
        /// </summary>
        /// <param name="srcPath">源文件路径</param>
        /// <param name="destPath">目标文件路径</param>
        /// <returns>成功返回空字符串，失败返回错误信息</returns>
        public abstract string RenameFileSync(string srcPath, string destPath);

        /// <summary>
        /// 删除文件（同步）
        /// </summary>
        /// <param name="filePath">源文件路径，支持本地路径</param>
        /// <returns>成功返回空字符串，失败返回错误信息</returns>
        public abstract string UnlinkSync(string filePath);

        /// <summary>
        /// 删除文件（异步）
        /// </summary>
        /// <param name="param"></param>
        public abstract void Unlink(UnlinkParam param);

        /// <summary>
        /// 创建目录（异步）
        /// </summary>
        /// <param name="param"></param>
        public abstract void Mkdir(MkdirParam param);

        /// <summary>
        /// 创建目录（同步）
        /// </summary>
        /// <param name="dirPath">创建的目录路径</param>
        /// <param name="recursive">是否在递归创建该目录的上级目录后再创建该目录。如果对应的上级目录已经存在，则不创建该上级目录。如 dirPath 为 a/b/c/d 且 recursive 为 true，将创建 a 目录，再在 a 目录下创建 b 目录，以此类推直至创建 a/b/c 目录下的 d 目录。</param>
        /// <returns>成功返回空字符串，失败返回错误信息</returns>
        public abstract string MkdirSync(string dirPath, bool recursive = false);

        /// <summary>
        /// 删除目录（异步）
        /// </summary>
        /// <param name="param"></param>
        public abstract void Rmdir(RmdirParam param);

        /// <summary>
        /// 删除目录（同步）
        /// </summary>
        /// <param name="dirPath">创建的目录路径</param>
        /// <param name="recursive">是否递归删除目录。如果为 true，则删除该目录和该目录下的所有子目录以及文件	。</param>
        /// <returns>成功返回空字符串，失败返回错误信息</returns>
        public abstract string RmdirSync(string dirPath, bool recursive = false);

        /// <summary>
        /// 读取文件描述信息（同步）
        /// </summary>
        /// <param name="path">文件/目录路径</param>
        /// <param name="recursive">是否递归获取目录下的每个文件的 Stat 信息	</param>
        /// <param name="throwException">是否抛出错误信息，如果抛出错误信息，当文件不存在时则会抛出异常，错误信息从异常中获取。</param>
        /// <returns>返回文件信息，如果访问失败则返回null</returns>
        public abstract TTStatInfo StatSync(string path, bool throwException = false);

        /// <summary>
        /// 读取文件描述信息（异步）
        /// </summary>
        /// <param name="param"></param>
        public abstract void Stat(StatParam param);

        /// <summary>
        /// 获取保存的用户目录文件列表（仅WebGL平台可用）
        /// </summary>
        public abstract void GetSavedFileList(GetSavedFileListParam param);

        /// <summary>
        /// 根据url链接获取本地缓存文件路径（仅WebGL平台可用）
        /// </summary>
        /// <param name="url">输入文件下载链接url</param>
        /// <returns>返回本地缓存文件路径，以scfile://user开头的路径，可以直接用这个路径访问该文件</returns>
        public abstract string GetLocalCachedPathForUrl(string url);

        /// <summary>
        /// 判断该url是否有本地缓存文件（仅WebGL平台可用）
        /// </summary>
        /// <param name="url">输入文件下载链接url</param>
        /// <returns>如果存在缓存文件则返回true，不存在缓存文件则返回false</returns>
        public abstract bool IsUrlCached(string url);
        
        private static int _isDataMigrated = -1;

#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern bool StarkCanUseLocalStorage();
#else
        private static bool StarkCanUseLocalStorage()
        {
            return false;
        }
#endif

#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern bool StarkIsDataMigrated();
#else
        private static bool StarkIsDataMigrated()
        {
            return false;
        }
#endif

#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern void StarkMigratingData();
#else
        private static void StarkMigratingData()
        {
        }
#endif

        public static bool CanUseLocalStorage()
        {
            return StarkCanUseLocalStorage();
        }

        public static bool IsDataMigrated()
        {
            if (_isDataMigrated == 1)
            {
                return true;
            }
            else if (_isDataMigrated == 0)
            {
                return false;
            }

            var migrated = StarkIsDataMigrated();
            _isDataMigrated = migrated ? 1 : 0;
            return migrated;
        }

        public static void MigratingData()
        {
            if (!IsDataMigrated())
            {
                StarkMigratingData();
                _isDataMigrated = 1;
            }
        }
    }
}