using UnityEditor;
using UnityEngine;
using Newtonsoft.Json;
using System.IO;
using xasset.editor;
using xasset;
using HybridCLR.Editor.Commands;
using System;

public class BuildEditor
{
    [MenuItem("Tools/BuildPlayer", false, (int)ToolsMenuSort.BuildPlayer)]
    public static void BuildPlayer()
    {
        string[] args = Environment.GetCommandLineArgs();
        string path = string.Empty;
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i].StartsWith("--path:"))
            {
                path = args[i].Replace("--path:", string.Empty);
                Debug.Log(args[i]);
            }
            else if (args[i].StartsWith("--version:"))
            {
                PlayerSettings.bundleVersion = args[i].Replace("--version:", string.Empty);
                Debug.Log(args[i]);
            }
        }
        BuildBundles();
        //BuildOptions options = EditorUserBuildSettings.development ? BuildOptions.Development : BuildOptions.None;
        BuildPipeline.BuildPlayer(EditorBuildSettings.scenes, path, EditorUserBuildSettings.activeBuildTarget, BuildOptions.None);
    }

    [MenuItem("Tools/BuildBundles", false, (int)ToolsMenuSort.BuildBundles)]
    public static void BuildBundles()
    {
        CopyConfig();
        HotAssemblyCompile.Generate();
        PrebuildCommand.GenerateAll();
        CopyMetadata();
        XAssetBuild();
    }

    [MenuItem("Tools/CopyConfig", false, (int)ToolsMenuSort.CopyConfig)]
    public static void CopyConfig()
    {
        string path = Environment.CurrentDirectory.Substring(0, Environment.CurrentDirectory.LastIndexOf(@"\"));
        FileUtil.ReplaceDirectory($"{path}\\Luban\\Client\\OutCodes", $"{Application.dataPath}\\Scripts\\SubScripts\\Config\\Auto");
        FileUtil.ReplaceDirectory($"{path}\\Luban\\Client\\OutBytes", $"{Application.dataPath}\\ZRes\\DataConfig");
        AssetDatabase.Refresh();
    }
    [MenuItem("Tools/CopyMatedata", false, (int)ToolsMenuSort.CopyMetadata)]
    public static void CopyMetadata()
    {
        TextAsset ta = AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/ZRes/GameConfig/HotUpdateConfig.txt");
        var config = JsonConvert.DeserializeObject<MainAssembly.HotUpdateConfig>(ta.text);
        string stripDir = HybridCLR.Editor.SettingsUtil.GetAssembliesPostIl2CppStripDir(EditorUserBuildSettings.activeBuildTarget);
        for (int i = 0; i < config.Metadata.Count; i++)
        {
            var name = config.Metadata[i];
            File.Copy($"{stripDir}/{name}.dll", $"{Application.dataPath}\\ZRes\\Assembly\\{name}.bytes", true);
        }
        File.Copy($"{Environment.CurrentDirectory}\\HotAssembly\\obj\\Debug\\HotAssembly.dll", $"{Application.dataPath}\\ZRes\\Assembly\\HotAssembly.bytes", true);
        AssetDatabase.Refresh();
    }
    [MenuItem("Tools/XAssetBuild", false, (int)ToolsMenuSort.XAssetBuild)]
    public static void XAssetBuild()
    {
        MenuItems.BuildBundles();
        var versions = Utility.LoadFromFile<Versions>(Settings.GetCachePath(Versions.BundleFilename));
        //BuildUpdateInfo��BuildBundlesʱ�Ѿ�����
        //Builder.BuildUpdateInfo(versions, hash, file.Length);
        Builder.BuildPlayerAssets(versions);
    }
}
