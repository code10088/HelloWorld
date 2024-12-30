using System;
using System.Collections.Generic;

namespace YooAsset
{
    /// <summary>
    /// 文件系统参数
    /// </summary>
    public class FileSystemParameters
    {
        internal readonly Dictionary<string, object> CreateParameters = new Dictionary<string, object>(100);

        /// <summary>
        /// 文件系统类
        /// 格式: "namespace.class,assembly"
        /// 格式: "命名空间.类型名,程序集"
        /// </summary>
        public string FileSystemClass { private set; get; }

        /// <summary>
        /// 文件系统的根目录
        /// </summary>
        public string RootDirectory { private set; get; }


        public FileSystemParameters(string fileSystemClass, string rootDirectory)
        {
            FileSystemClass = fileSystemClass;
            RootDirectory = rootDirectory;
        }

        /// <summary>
        /// 添加自定义参数
        /// </summary>
        public void AddParameter(string name, object value)
        {
            CreateParameters.Add(name, value);
        }

        /// <summary>
        /// 创建文件系统
        /// </summary>
        internal IFileSystem CreateFileSystem(string packageName)
        {
            YooLogger.Log($"The package {packageName} create file system : {FileSystemClass}");

            Type classType = Type.GetType(FileSystemClass);
            if (classType == null)
            {
                YooLogger.Error($"Can not found file system class type {FileSystemClass}");
                return null;
            }

            var instance = (IFileSystem)System.Activator.CreateInstance(classType, true);
            if (instance == null)
            {
                YooLogger.Error($"Failed to create file system instance {FileSystemClass}");
                return null;
            }

            foreach (var param in CreateParameters)
            {
                instance.SetParameter(param.Key, param.Value);
            }
            instance.OnCreate(packageName, RootDirectory);
            return instance;
        }

        #region 创建默认的文件系统类
        /// <summary>
        /// 创建默认的编辑器文件系统参数
        /// <param name="simulateBuildResult">模拟构建结果</param>
        /// </summary>
        public static FileSystemParameters CreateDefaultEditorFileSystemParameters(EditorSimulateBuildResult simulateBuildResult)
        {
            string fileSystemClass = typeof(DefaultEditorFileSystem).FullName;
            var fileSystemParams = new FileSystemParameters(fileSystemClass, simulateBuildResult.PackageRootDirectory);
            return fileSystemParams;
        }

        /// <summary>
        /// 创建默认的内置文件系统参数
        /// </summary>
        /// <param name="decryptionServices">加密文件解密服务类</param>
        /// <param name="verifyLevel">缓存文件的校验等级</param>
        /// <param name="rootDirectory">内置文件的根路径</param>
        public static FileSystemParameters CreateDefaultBuildinFileSystemParameters(IDecryptionServices decryptionServices = null, EFileVerifyLevel verifyLevel = EFileVerifyLevel.Middle, string rootDirectory = null)
        {
            string fileSystemClass = typeof(DefaultBuildinFileSystem).FullName;
            var fileSystemParams = new FileSystemParameters(fileSystemClass, rootDirectory);
            fileSystemParams.AddParameter(FileSystemParametersDefine.DECRYPTION_SERVICES, decryptionServices);
            fileSystemParams.AddParameter(FileSystemParametersDefine.FILE_VERIFY_LEVEL, verifyLevel);
            return fileSystemParams;
        }

        /// <summary>
        /// 创建默认的缓存文件系统参数
        /// </summary>
        /// <param name="remoteServices">远端资源地址查询服务类</param>
        /// <param name="decryptionServices">加密文件解密服务类</param>
        /// <param name="verifyLevel">缓存文件的校验等级</param>
        /// <param name="rootDirectory">文件系统的根目录</param>
        public static FileSystemParameters CreateDefaultCacheFileSystemParameters(IRemoteServices remoteServices, IDecryptionServices decryptionServices = null, EFileVerifyLevel verifyLevel = EFileVerifyLevel.Middle, string rootDirectory = null)
        {
            string fileSystemClass = typeof(DefaultCacheFileSystem).FullName;
            var fileSystemParams = new FileSystemParameters(fileSystemClass, rootDirectory);
            fileSystemParams.AddParameter(FileSystemParametersDefine.REMOTE_SERVICES, remoteServices);
            fileSystemParams.AddParameter(FileSystemParametersDefine.DECRYPTION_SERVICES, decryptionServices);
            fileSystemParams.AddParameter(FileSystemParametersDefine.FILE_VERIFY_LEVEL, verifyLevel);
            return fileSystemParams;
        }

        /// <summary>
        /// 创建默认的WebServer文件系统参数
        /// </summary>
        /// <param name="disableUnityWebCache">禁用Unity的网络缓存</param>
        public static FileSystemParameters CreateDefaultWebServerFileSystemParameters(bool disableUnityWebCache = false)
        {
            string fileSystemClass = typeof(DefaultWebServerFileSystem).FullName;
            var fileSystemParams = new FileSystemParameters(fileSystemClass, null);
            fileSystemParams.AddParameter(FileSystemParametersDefine.DISABLE_UNITY_WEB_CACHE, disableUnityWebCache);
            return fileSystemParams;
        }

        /// <summary>
        /// 创建默认的WebRemote文件系统参数
        /// </summary>
        /// <param name="remoteServices">远端资源地址查询服务类</param>
        /// <param name="disableUnityWebCache">禁用Unity的网络缓存</param>
        public static FileSystemParameters CreateDefaultWebRemoteFileSystemParameters(IRemoteServices remoteServices, bool disableUnityWebCache = false)
        {
            string fileSystemClass = typeof(DefaultWebRemoteFileSystem).FullName;
            var fileSystemParams = new FileSystemParameters(fileSystemClass, null);
            fileSystemParams.AddParameter(FileSystemParametersDefine.REMOTE_SERVICES, remoteServices);
            fileSystemParams.AddParameter(FileSystemParametersDefine.DISABLE_UNITY_WEB_CACHE, disableUnityWebCache);
            return fileSystemParams;
        }
        #endregion
    }
}