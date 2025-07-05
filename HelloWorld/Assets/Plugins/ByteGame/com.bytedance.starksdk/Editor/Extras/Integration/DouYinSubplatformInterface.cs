#if TUANJIE_1_5_OR_NEWER
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Profile;
using UnityEngine;

namespace TTSDK.Tool
{
    [InitializeOnLoad]
    public static class DouYinSubTargetManager
    {
        static DouYinSubTargetManager()
        {
            MiniGameSubplatformManager.RegisterSubplatform(new DouYinSubplatformInterface());
        }
    }

    public class DouYinSubplatformInterface : MiniGameSubplatformInterface
    {
        
        public override string GetSubplatformName()
        {
            return "DouYin:抖音小游戏";
        }

        public override MiniGameSettings GetSubplatformSettings()
        {
            return new DouYinMiniGameSettings(new DouYinMiniGameSettingsEditor());
        }

        public override string GetSubplatformLink()
        {
            return "https://developer.open-douyin.com/docs/resource/zh-CN/mini-game/guide/overview";
        }

        public override string GetSubplatformTooltip()
        {
            return "点击查看抖音小游戏文档";
        }

        public override BuildMiniGameError Build(BuildProfile buildProfile)
        {
            // 1.Pre-processing
            string buildProfilePath = AssetDatabase.GetAssetPath(buildProfile); // Save the path of the buildProfile for post-processing
            string buildPath = buildProfile.buildPath;
            var douYinMiniGameSettings = buildProfile.miniGameSettings as DouYinMiniGameSettings;
            if (douYinMiniGameSettings is null)
            {
                Debug.LogError("预处理阶段 BuildProfile 不合法");
                return BuildMiniGameError.InvalidInput;
            }
            
            PlayerSettings playerSettings = AssetDatabase.LoadAssetAtPath<PlayerSettings>("ProjectSettings/ProjectSettings.asset"); // Global PlayerSettings
            if (buildProfile.HasOverrridePlayersettings())
                playerSettings = buildProfile.playerSettings; // Override PlayerSettings

            if (!IsBuildSettingsValid(douYinMiniGameSettings, playerSettings))
            {
                return BuildMiniGameError.InvalidInput;
            }
            
            // 2.BuildPlayer
            SyncBuildSettingsToStark(douYinMiniGameSettings);
            var res = API.BuildManager.BuildForTuanjie(buildPath, playerSettings);
            if (string.IsNullOrEmpty(res))
            {
                return BuildMiniGameError.SubplatformConvertFailed;
            }

            // 3.Post-processing
            BuildProfile reloadBuildProfile = AssetDatabase.LoadAssetAtPath<BuildProfile>(buildProfilePath);
            douYinMiniGameSettings = reloadBuildProfile.miniGameSettings as DouYinMiniGameSettings;
            if (douYinMiniGameSettings is null)
            {
                Debug.LogError("后处理阶段 BuildProfile 不合法");
                return BuildMiniGameError.InvalidInput;
            }
            
            Debug.Log("构建成功: " + res);
            return BuildMiniGameError.Succeeded;
        }

        public override BuildMiniGameError Build(BuildProfile buildProfile, BuildOptions options)
        {
            SyncBuildOptionsToStark(options);
            return Build(buildProfile);
        }

        /// <summary>
        /// 构建参数校验
        /// </summary>
        private static bool IsBuildSettingsValid(DouYinMiniGameSettings settings, PlayerSettings playerSettings)
        {
            if (!settings.needCompress)
            {
                Debug.LogWarning("警告：未启用首包资源压缩，包体可能较大。只能用于开发测试，请勿用于版本上线。");
            }

            if (settings.profiling)
            {
                Debug.LogWarning("警告：当前已开启「显示性能面板」（Profiling）。只能用于开发测试，请勿用于版本上线。");
            }

            if (settings.urlCacheList.Length == 0 || (settings.urlCacheList.Length == 1 && string.IsNullOrEmpty(settings.urlCacheList[0])))
            {
                Debug.LogWarning("警告：当前未配置「缓存资源域名」，可能影响游戏启动速度。");
            }

            var scriptingBackend = PlayerSettings.GetScriptingBackend_Internal(playerSettings, NamedBuildTarget.MiniGame);
            if (scriptingBackend != ScriptingImplementation.IL2CPP)
            {
                Debug.LogError($"Scripting Backend {scriptingBackend}(.Net 8) 暂不支持，请选择为 IL2CPP 后重试构建。");
                return false;
            }
            
            return true;
        }

        /// <summary>
        /// 将 BuildProfile 中配置的平台相关参数写回全局配置
        /// </summary>
        private static void SyncBuildSettingsToStark(DouYinMiniGameSettings settings)
        {
            var starkSettings = StarkBuilderSettings.LoadSettings();
            starkSettings.appId = settings.appId;
            starkSettings.wasmMemorySize = settings.wasmMemorySize;
            starkSettings.CDN = settings.CDN;
            starkSettings.preloadFiles = settings.preloadFiles;
            starkSettings.preloadDataListUrl = settings.preloadDataListUrl;
            starkSettings.urlCacheList = settings.urlCacheList;
            starkSettings.dontCacheFileNames = settings.dontCacheFileNames;
            starkSettings.needCompress = settings.needCompress;
            starkSettings.iOSPerformancePlus = settings.iOSPerformancePlus;
            starkSettings.profiling = settings.profiling;
            starkSettings.clearStreamingAssets = settings.clearStreamingAssets;
            starkSettings.orientation = (StarkBuilderSettings.Orientation)settings.orientation;
            starkSettings.Save();
        }

        /// <summary>
        /// 将 BuildProfile 中配置的 BuildOptions 参数写回全局配置
        /// </summary>
        private static void SyncBuildOptionsToStark(BuildOptions buildOptions)
        {
            var starkSettings = StarkBuilderSettings.LoadSettings();
            starkSettings.buildOptions = buildOptions;
            starkSettings.Save();
        }
    }
}
#endif