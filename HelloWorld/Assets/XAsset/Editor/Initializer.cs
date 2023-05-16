using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace xasset.editor
{
    public static class Initializer
    {
        private static readonly HashSet<string> collectedAssets = new HashSet<string>();

        [RuntimeInitializeOnLoadMethod]
        private static void RuntimeInitializeOnLoad()
        {
            var settings = Settings.GetDefaultSettings();
            Assets.Platform = Settings.Platform;
            Assets.SimulationMode = settings.player.simulationMode;
            Assets.MaxRetryTimes = settings.player.maxRetryTimes;
            Assets.MaxDownloads = settings.player.maxDownloads;

            if (Assets.SimulationMode && !settings.player.updatable)
            {
                Assets.Updatable = false;
                InitializeRequest.Initializer = InitializeAsync;
                AssetRequest.CreateHandler = EditorAssetHandler.CreateInstance;
                SceneRequest.CreateHandler = EditorSceneHandler.CreateInstance;
                // 编辑器仿真模式开启 通过引用计数回收资源优化内存 可能对性能有影响 
                References.GetFunc = Settings.GetDependencies;
                References.Enabled = true;
            }
            else
            {
                Assets.Updatable = settings.player.updatable;
                if (Assets.Updatable)
                {
                    if (Assets.SimulationMode)
                    {
                        Assets.UpdateInfoURL = $"{Assets.Protocol}{Settings.GetCachePath(UpdateInfo.Filename)}";
                        Assets.DownloadURL = $"{Assets.Protocol}{Settings.PlatformDataPath}";
                    }

                    InitializeRequest.Initializer = request => request.RuntimeInitializeAsync();
                }
                else
                    InitializeRequest.Initializer = InitializeAsyncWithOfflineMode;

                AssetRequest.CreateHandler = RuntimeAssetHandler.CreateInstance;
                SceneRequest.CreateHandler = RuntimeSceneHandler.CreateInstance;
            }
        }

        private static IEnumerator InitializeAsyncWithOfflineMode(InitializeRequest request)
        {
            Assets.DownloadDataPath = Settings.PlatformDataPath;
            Assets.PlayerAssets = Settings.GetDefaultSettings().GetPlayerAssets();
            Assets.PlayerAssets.packed = false;
            yield return null;
            Assets.Versions = Utility.LoadFromFile<Versions>(Settings.GetCachePath(Versions.Filename));
            yield return null;
            foreach (var version in Assets.Versions.data)
                version.Load(Settings.GetDataPath(version.file));
            request.SetResult(Request.Result.Success);
        }

        private static IEnumerator InitializeAsync(InitializeRequest request)
        {
            Assets.Versions = ScriptableObject.CreateInstance<Versions>();
            Assets.PlayerAssets = ScriptableObject.CreateInstance<PlayerAssets>();
            var groups = Settings.FindAssets<Group>();
            var settings = Settings.GetDefaultSettings();
            if (settings.player.collectAllAssets)
            {
                Assets.ContainsFunc = ContainsFuncAll;
                foreach (var group in groups)
                {
                    CollectAll(group);
                    yield return null;
                }
            }
            else
            {
                Assets.ContainsFunc = ContainsFuncFast;
                foreach (var group in groups)
                {
                    if (group.addressMode == AddressMode.LoadByName ||
                        group.addressMode == AddressMode.LoadByNameWithoutExtension)
                        CollectAll(group);
                    yield return null;
                }
            }


            request.SetResult(Request.Result.Success);
        }

        private static void CollectAll(Group group)
        {
            switch (group.addressMode)
            {
                case AddressMode.LoadByDependencies:
                case AddressMode.LoadByPath:

                {
                    var assets = Settings.Collect(group);
                    collectedAssets.UnionWith(Array.ConvertAll(assets, input => input.path));
                }

                    break;
                case AddressMode.LoadByName:
                {
                    var assets = Settings.Collect(group);
                    foreach (var asset in assets) Assets.SetAddress(asset.path, Path.GetFileName(asset.path));

                    collectedAssets.UnionWith(Array.ConvertAll(assets, input => input.path));
                }
                    break;
                case AddressMode.LoadByNameWithoutExtension:
                {
                    var assets = Settings.Collect(group);
                    foreach (var asset in assets)
                        Assets.SetAddress(asset.path, Path.GetFileNameWithoutExtension(asset.path));

                    collectedAssets.UnionWith(Array.ConvertAll(assets, input => input.path));
                }
                    break;
            }
        }

        private static bool ContainsFuncAll(string path)
        {
            if (!ContainsFuncFast(path))
                return false;
            if (collectedAssets.Contains(path)) return true;
            EditorUtility.DisplayDialog("错误", $"资源没有被采集:{path}", "确定");
            return false;
        }

        private static bool ContainsFuncFast(string path)
        {
            var result = File.Exists(path);
            if (!result)
                EditorUtility.DisplayDialog("错误", $"工程中找不到文件:{path}", "确定");
            return result;
        }
    }
}