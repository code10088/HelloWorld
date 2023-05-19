using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace xasset.editor
{
    [Serializable]
    public class PlayerConfiguration
    {
        /// <summary>
        ///     编辑器仿真模式下，是否采集所有资源，需要更长的初始化耗时，但是可以及时对未采集的内容进行预警。
        /// </summary>
        [Tooltip("编辑器仿真模式下，是否采集所有资源，需要更长的初始化耗时，但是可以及时对未采集的内容进行预警。")]
        public bool collectAllAssets;

        /// <summary>
        ///     编辑器仿真模式，未开启更新时，开启后，无需打包可以进入播放模式，开启更新时，开启后，无需把资源部署到服务器，就行运行真机的更新过程。
        /// </summary>
        [Tooltip("编辑器仿真模式，未开启更新时，开启后，无需打包可以进入播放模式，开启更新时，开启后，无需把资源部署到服务器，就行运行真机的更新过程。")]
        public bool simulationMode = true;

        /// <summary>
        ///     是否开启更新，运行时有效。
        /// </summary>
        [Tooltip("是否开启更新，运行时有效。")] public bool updatable;

        /// <summary>
        ///     更新信息地址
        /// </summary>
        [Tooltip("更新信息地址")] public string updateInfoURL = "http://127.0.0.1/";

        /// <summary>
        ///     资源下载地址
        /// </summary>
        [Tooltip("资源下载地址")] public string downloadURL = "http://127.0.0.1/Bundles";

        /// <summary>
        ///     安装包下载地址
        /// </summary>
        [Tooltip("安装包下载地址")] public string playerURL = "http://127.0.0.1/Build/xasset.apk";

        /// <summary>
        ///     安装包资源分包模式
        /// </summary>
        [Tooltip("安装包资源分包模式")] public PlayerAssetsSplitMode splitMode = PlayerAssetsSplitMode.IncludeAllAssets;

        /// <summary>
        ///     是否将安装包内的资源二次打包，Android 设备上开启这个选项可以优化 IO 效率。
        /// </summary>
        [Tooltip("是否将安装包内的资源二次打包，Android 设备上开启这个选项可以优化 IO 效率")]
        public bool packed = true;

        /// <summary>
        ///     日志级别
        /// </summary>
        [Tooltip("日志级别")] public LogLevel logLevel = LogLevel.Debug;

        /// <summary>
        ///     最大并行下载数量
        /// </summary>
        [Range(1, 10)] [Tooltip("最大并行下载数量")] public byte maxDownloads = 5;

        /// <summary>
        ///     最大错误重试次数
        /// </summary>
        [Range(0, 5)] [Tooltip("最大错误重试次数")] public byte maxRetryTimes = 3;

        /// <summary>
        ///     每个队列最大单帧更新数量。
        /// </summary>
        [Range(0, 30)] [Tooltip("每个队列最大单帧更新数量")]
        public byte maxRequests = 10;

        /// <summary>
        ///     是否开启自动切片
        /// </summary>
        [Tooltip("是否开启自动切片")] public bool autoSlicing = true;

        /// <summary>
        ///     自动切片时间，值越大处理的请求数量越多，值越小处理请求的数量越小，可以根据目标帧率分配。
        /// </summary>
        [Tooltip("自动切片时间，值越大处理的请求数量越多，值越小处理请求的数量越小，可以根据目标帧率分配")]
        public float autoSliceTimestep = 1 / 16f;

        /// <summary>
        ///     自动回收的时间步长
        /// </summary>
        [Tooltip("自动回收的时间步长")] public float autoRecycleTimestep = 0.7f;
    }

    [Serializable]
    public class BundleConfiguration
    {
        /// <summary>
        ///     对打包的数据进行混淆
        /// </summary>
        [Tooltip("对打包的数据进行混淆")] public bool encryption = true;

        /// <summary>
        ///     打包时先检查引用关系，如果有异常会弹窗提示。
        /// </summary>
        [Tooltip("打包时先检查引用关系，如果有异常会弹窗提示。")] public bool checkReference = true;

        /// <summary>
        ///     保留 bundle 的名字，开启后 Bundles 目录输出的 bundle 的文件名更直观，否则文件名将只保留 hash。
        /// </summary>
        [Tooltip("保留 bundle 的名字，开启后 Bundles 目录输出的 bundle 的文件名更直观，否则文件名将只保留 hash。")]
        public bool saveBundleName = true;

        /// <summary>
        ///     按 build 名字分割 bundle 名字，不同 build 的资源打包后会输出到不同文件夹下 当开启 saveBundleName 时有效。
        /// </summary>
        [Tooltip("按 build 名字分割 bundle 名字，不同 build 的资源打包后会输出到不同文件夹下 当开启 saveBundleName 时有效。")]
        public bool splitBundleNameWithBuild = true;

        /// <summary>
        ///     将所有场景按文件为单位打包。
        /// </summary>
        [Tooltip("将所有场景按文件为单位打包。")] public bool packByFileForAllScenes = true;

        /// <summary>
        ///     将所有 Shader 打包到一起。
        /// </summary>
        [Tooltip("将所有 Shader 打包到一起。")] public bool packTogetherForAllShaders = true;

        /// <summary>
        ///     是否对 AssetPack 中的资源进行二次打包，Android 可以优化加载性能。
        /// </summary>
        [Tooltip("是否对 AssetPack 中的资源进行二次打包，Android 可以优化加载性能。")]
        public bool buildAssetPackAssets;

        /// <summary>
        ///     强制使用内置管线。
        /// </summary>
        [Tooltip("强制使用内置管线。")] public bool forceUseBuiltinPipeline;

        /// <summary>
        ///     二次打包时，单个 pack 最大的大小。
        /// </summary>
        [Tooltip("二次打包时，单个 pack 最大的大小。")] public ulong maxAssetPackSize = 1024 * 1024 * 20;

        /// <summary>
        ///     bundle 的扩展名
        /// </summary>
        [Tooltip("bundle 的扩展名")] public string extension = ".bundle";

        /// <summary>
        ///     Shader 的后缀
        /// </summary>
        [Tooltip("Shader 的后缀")] public List<string> shaders = new List<string>
            { ".shader", ".shadervariants", ".compute" };

        /// <summary>
        ///     不参与打包的文件
        /// </summary>
        [Tooltip("不参与打包的文件")] public List<string> excludeFiles = new List<string>
        {
            ".cs",
            ".cginc",
            ".hlsl",
            ".spriteatlas",
            ".dll"
        };
    }


    [CreateAssetMenu(fileName = nameof(Settings), menuName = "xasset/" + nameof(Settings))]
    public class Settings : ScriptableObject
    { 
        /// <summary>
        ///     播放器设置
        /// </summary>
        [Tooltip("播放器设置")] public PlayerConfiguration player = new PlayerConfiguration();

        /// <summary>
        ///     打包 bundle 的设置
        /// </summary>
        [Tooltip("打包 bundle 的设置")] public BundleConfiguration bundle = new BundleConfiguration();
        private static string Filename => $"Assets/xasset/Config/{nameof(Settings)}.asset";

        public static Group GetAutoGroup()
        {
            var group = GetOrCreateAsset<Group>($"Assets/xasset/Config/Auto.asset");
            group.bundleMode = BundleMode.PackByCustom;
            group.addressMode = AddressMode.LoadByDependencies;
            return group;
        }

        public PlayerAssets GetPlayerAssets()
        {
            var assets = CreateInstance<PlayerAssets>();
            assets.version = PlayerSettings.bundleVersion;
            assets.updateInfoURL = $"{player.updateInfoURL}/{Platform}/{UpdateInfo.Filename}";
            assets.downloadURL = $"{player.downloadURL}/{Platform}";
            assets.updatable = player.updatable;
            assets.packed = player.packed;
            assets.maxDownloads = player.maxDownloads;
            assets.maxRetryTimes = player.maxRetryTimes;
            assets.splitMode = player.splitMode;
            assets.logLevel = player.logLevel;
            assets.maxRequests = player.maxRequests;
            assets.autoSliceTimestep = player.autoSliceTimestep;
            assets.autoSlicing = player.autoSlicing;
            assets.autoRecycleTimestep = player.autoRecycleTimestep;
            if (Platform != Platform.WebGL) return assets;
            assets.packed = false;
            assets.splitMode = PlayerAssetsSplitMode.IncludeAllAssets;
            return assets; 
        }

        public static string PlatformCachePath =>
            $"{Environment.CurrentDirectory}/{Assets.Bundles}Cache/{Platform}".Replace('\\', '/');

        public static string PlatformDataPath =>
            $"{Environment.CurrentDirectory.Replace('\\', '/')}/{Assets.Bundles}/{Platform}";

        public static Platform Platform => GetPlatform();

        private static Settings defaultSettings;

        public static Settings GetDefaultSettings()
        {
            if (defaultSettings != null) return defaultSettings;
            var assets = FindAssets<Settings>();
            defaultSettings = assets.Length > 0
                ? assets[0]
                : GetOrCreateAsset<Settings>(Filename);
            return defaultSettings;
        }

        public static Versions GetDefaultVersions()
        {
            var versions = Utility.LoadFromFile<Versions>(GetCachePath(Versions.Filename));
            return versions;
        }

        public static string GetDataPath(string filename)
        {
            return $"{PlatformDataPath}/{filename}";
        }

        public static string GetCachePath(string filename)
        {
            return $"{PlatformCachePath}/{filename}";
        }

        private static Platform GetPlatform()
        {
            // ReSharper disable once SwitchStatementHandlesSomeKnownEnumValuesWithDefault
            switch (EditorUserBuildSettings.activeBuildTarget)
            {
                case BuildTarget.Android:
                    return Platform.Android;
                case BuildTarget.StandaloneOSX:
                    return Platform.OSX;
                case BuildTarget.StandaloneWindows:
                case BuildTarget.StandaloneWindows64:
                    return Platform.Windows;
                case BuildTarget.iOS:
                    return Platform.iOS;
                case BuildTarget.WebGL:
                    return Platform.WebGL;
                case BuildTarget.StandaloneLinux64:
                    return Platform.Linux;
                default:
                    return Platform.Default;
            }
        }

        public static long GetLastWriteTime(string path)
        {
            var file = new FileInfo(path);
            return file.Exists ? file.LastAccessTime.ToFileTime() : 0;
        }

        public static string[] GetDependenciesWithoutCache(string assetPath)
        {
            var set = new HashSet<string>();
            set.UnionWith(AssetDatabase.GetDependencies(assetPath));
            set.Remove(assetPath);
            var exclude = GetDefaultSettings().bundle.excludeFiles;
            // Unity 会存在场景依赖场景的情况。
            set.RemoveWhere(s => s.EndsWith(".unity") || exclude.Exists(s.EndsWith));
            return set.ToArray();
        }

        public static T GetOrCreateAsset<T>(string path) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null) return asset;
            Utility.CreateDirectoryIfNecessary(path);
            asset = CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        public static T[] FindAssets<T>() where T : ScriptableObject
        {
            var builds = new List<T>();
            var guilds = AssetDatabase.FindAssets("t:" + typeof(T).FullName);
            foreach (var guild in guilds)
            {
                var assetPath = AssetDatabase.GUIDToAssetPath(guild);
                if (string.IsNullOrEmpty(assetPath)) continue;

                var asset = AssetDatabase.LoadAssetAtPath<T>(assetPath);
                if (asset == null) continue;

                builds.Add(asset);
            }

            return builds.ToArray();
        }

        /// <summary>
        ///     [path, entry, bundle, group, return(bundle)]
        /// </summary>
        public static Func<string, string, string, Group, string> customPacker { get; set; } = null;

        public static Func<string, bool> customFilter { get; set; } = s => true;

        private static string GetDirectoryName(string path)
        {
            var dir = Path.GetDirectoryName(path);
            return dir?.Replace("\\", "/");
        }

        public static string PackAsset(BuildAsset asset)
        {
            var assetPath = asset.path;
            var group = asset.group;
            if (group == null)
            {
                asset.addressMode = AddressMode.LoadByDependencies;
                return "auto";
            }

            var entry = asset.entry;
            var bundle = group.name.ToLower();

            var dir = GetDirectoryName(entry) + "/";
            assetPath = assetPath.Replace(dir, "");
            entry = entry.Replace(dir, "");

            switch (group.bundleMode)
            {
                case BundleMode.PackTogether:
                    bundle = group.name;
                    break;
                case BundleMode.PackByFolder:
                    bundle = GetDirectoryName(assetPath);
                    break;
                case BundleMode.PackByFile:
                    bundle = assetPath;
                    break;
                case BundleMode.PackByTopSubFolder:
                    if (!string.IsNullOrEmpty(entry))
                    {
                        var pos = assetPath.IndexOf("/", entry.Length + 1, StringComparison.Ordinal);
                        bundle = pos != -1 ? assetPath.Substring(0, pos) : entry;
                    }
                    else
                    {
                        Logger.E($"invalid rootPath {assetPath}");
                    }

                    break;
                case BundleMode.PackByEntry:
                    bundle = Path.GetFileNameWithoutExtension(entry);
                    break;
                case BundleMode.PackByCustom:
                    if (customPacker != null) bundle = customPacker?.Invoke(assetPath, entry, bundle, group);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return PackAsset(assetPath, bundle, group.build);
        }

        public static string PackAsset(string assetPath, string bundle, string build)
        {
            var settings = GetDefaultSettings().bundle;

            if (settings.packTogetherForAllShaders && settings.shaders.Exists(assetPath.EndsWith))
                bundle = "shaders";

            if (settings.packByFileForAllScenes && assetPath.EndsWith(".unity"))
                bundle = assetPath;

            bundle = $"{FixedName(bundle)}{settings.extension}";

            if (!string.IsNullOrEmpty(build) && settings.splitBundleNameWithBuild)
                return $"{build.ToLower()}/{bundle}";

            return $"{bundle}";
        }

        private static string FixedName(string bundle)
        {
            return bundle.Replace(" ", "").Replace("/", "_").Replace("-", "_").Replace(".", "_").ToLower();
        }

        public static bool FindReferences(BuildAsset asset)
        {
            var type = asset.type;
            if (type == null) return false;
            return !type.Contains("TextAsset") && !type.Contains("Texture");
        }

        public static BuildAsset GetAsset(string path)
        {
            return GetBuildAssetCache().GetAsset(path);
        }

        private static BuildAssetCache BuildAssetCache;

        public static string[] GetDependencies(string assetPath)
        {
            return GetBuildAssetCache().GetDependencies(assetPath);
        }

        public static BuildAssetCache GetBuildAssetCache()
        {
            if (BuildAssetCache == null)
            {
                BuildAssetCache = GetOrCreateAsset<BuildAssetCache>(BuildAssetCache.Filename);
            }

            return BuildAssetCache;
        } 

        public static BuildAsset[] Collect(Group group)
        {
            var assets = new List<BuildAsset>();
            if (group.entries == null) return assets.ToArray();
            foreach (var entry in group.entries)
            {
                GetAssets(group, entry, assets);
            }

            return assets.ToArray();
        } 
        
        private static void AddAsset(string assetPath, string entry, Group group, ICollection<BuildAsset> assets)
        {
            if (customFilter != null && !customFilter(assetPath)) return;
            var asset = GetAsset(assetPath);
            asset.entry = entry;
            asset.group = group;
            asset.addressMode = group.addressMode;
            assets.Add(asset);
        }

        private static void GetAssets(Group group, Object entry, ICollection<BuildAsset> assets)
        {
            var path = AssetDatabase.GetAssetPath(entry);
            if (string.IsNullOrEmpty(path)) return;
            if (Directory.Exists(path))
            {
                var guilds = AssetDatabase.FindAssets(group.filter, new[] { path });
                var set = new HashSet<string>();
                var exclude = GetDefaultSettings().bundle.excludeFiles;
                foreach (var guild in guilds)
                {
                    var child = AssetDatabase.GUIDToAssetPath(guild);
                    if (string.IsNullOrEmpty(child) || exclude.Exists(child.EndsWith)
                                                    || Directory.Exists(child)
                                                    || set.Contains(child)) continue;
                    set.Add(child);
                    AddAsset(child, path, group, assets);
                }
            }
            else
            {
                AddAsset(path, path, group, assets);
            }
        }
    }
}