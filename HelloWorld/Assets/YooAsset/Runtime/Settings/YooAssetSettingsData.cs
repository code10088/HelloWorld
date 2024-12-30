using System.IO;
using UnityEngine;

namespace YooAsset
{
    internal static class YooAssetSettingsData
    {
        private static YooAssetSettings _setting = null;
        public static YooAssetSettings Setting
        {
            get
            {
                if (_setting == null)
                    LoadSettingData();
                return _setting;
            }
        }

        /// <summary>
        /// 加载配置文件
        /// </summary>
        private static void LoadSettingData()
        {
            _setting = Resources.Load<YooAssetSettings>("YooAssetSettings");
            if (_setting == null)
            {
                YooLogger.Log("YooAsset use default settings.");
                _setting = ScriptableObject.CreateInstance<YooAssetSettings>();
            }
            else
            {
                YooLogger.Log("YooAsset use user settings.");
            }
        }

        /// <summary>
        /// 获取构建报告文件名
        /// </summary>
        public static string GetReportFileName(string packageName, string packageVersion)
        {
            return $"{packageName}_{packageVersion}.report";
        }

        /// <summary>
        /// 获取清单文件完整名称
        /// </summary>
        public static string GetManifestBinaryFileName(string packageName, string packageVersion)
        {
            return $"{packageName}_{packageVersion}.bytes";
        }

        /// <summary>
        /// 获取清单文件完整名称
        /// </summary>
        public static string GetManifestJsonFileName(string packageName, string packageVersion)
        {
            return $"{packageName}_{packageVersion}.json";
        }

        /// <summary>
        /// 获取包裹的哈希文件完整名称
        /// </summary>
        public static string GetPackageHashFileName(string packageName, string packageVersion)
        {
            return $"{packageName}_{packageVersion}.hash";
        }

        /// <summary>
        /// 获取包裹的版本文件完整名称
        /// </summary>
        public static string GetPackageVersionFileName(string packageName)
        {
            return $"{packageName}.version";
        }

        #region 路径相关
        /// <summary>
        /// 获取YOO的Resources目录的加载路径
        /// </summary>
        public static string GetYooResourcesLoadPath(string packageName, string fileName)
        {
            if (string.IsNullOrEmpty(Setting.DefaultYooFolderName))
                return PathUtility.Combine(packageName, fileName);
            else
                return PathUtility.Combine(Setting.DefaultYooFolderName, packageName, fileName);
        }

        /// <summary>
        /// 获取YOO的Resources目录的全路径
        /// </summary>
        public static string GetYooResourcesFullPath()
        {
            if (string.IsNullOrEmpty(Setting.DefaultYooFolderName))
                return $"Assets/YooAsset/Config/Resources";
            else
                return $"Assets/YooAsset/Config/Resources/{Setting.DefaultYooFolderName}";
        }

        /// <summary>
        /// 获取YOO的编辑器下内置文件根目录
        /// </summary>
        public static string GetYooEditorBuildinRoot()
        {
            if (string.IsNullOrEmpty(Setting.DefaultYooFolderName))
                return PathUtility.Combine(Application.dataPath, "StreamingAssets");
            else
                return PathUtility.Combine(Application.dataPath, "StreamingAssets", Setting.DefaultYooFolderName);
        }

        /// <summary>
        /// 获取YOO的PC端内置文件根目录
        /// </summary>
        public static string GetYooStandaloneBuildinRoot()
        {
            if (string.IsNullOrEmpty(Setting.DefaultYooFolderName))
                return Application.streamingAssetsPath;
            else
                return PathUtility.Combine(Application.streamingAssetsPath, Setting.DefaultYooFolderName);
        }

        /// <summary>
        /// 获取YOO的移动端内置文件根目录
        /// </summary>
        public static string GetYooMobileBuildinRoot()
        {
            if (string.IsNullOrEmpty(Setting.DefaultYooFolderName))
                return Application.streamingAssetsPath;
            else
                return PathUtility.Combine(Application.streamingAssetsPath, Setting.DefaultYooFolderName);
        }

        /// <summary>
        /// 获取YOO的Web端内置文件根目录
        /// </summary>
        public static string GetYooWebBuildinRoot()
        {
            if (string.IsNullOrEmpty(Setting.DefaultYooFolderName))
                return Application.streamingAssetsPath;
            else
                return PathUtility.Combine(Application.streamingAssetsPath, Setting.DefaultYooFolderName);
        }

        /// <summary>
        /// 获取YOO的编辑器下缓存文件根目录
        /// </summary>
        public static string GetYooEditorCacheRoot()
        {
            if (string.IsNullOrEmpty(Setting.DefaultYooFolderName))
            {
                string projectPath = Path.GetDirectoryName(Application.dataPath);
                projectPath = PathUtility.RegularPath(projectPath);
                return projectPath;
            }
            else
            {
                // 注意：为了方便调试查看，编辑器下把存储目录放到项目根目录下。
                string projectPath = Path.GetDirectoryName(Application.dataPath);
                projectPath = PathUtility.RegularPath(projectPath);
                return PathUtility.Combine(projectPath, Setting.DefaultYooFolderName);
            }
        }

        /// <summary>
        /// 获取YOO的PC端缓存文件根目录
        /// </summary>
        public static string GetYooStandaloneCacheRoot()
        {
            if (string.IsNullOrEmpty(Setting.DefaultYooFolderName))
                return Application.dataPath;
            else
                return PathUtility.Combine(Application.dataPath, Setting.DefaultYooFolderName);
        }

        /// <summary>
        /// 获取YOO的移动端缓存文件根目录
        /// </summary>
        public static string GetYooMobileCacheRoot()
        {
            if (string.IsNullOrEmpty(Setting.DefaultYooFolderName))
                return Application.persistentDataPath;
            else
                return PathUtility.Combine(Application.persistentDataPath, Setting.DefaultYooFolderName);
        }
        #endregion
    }
}