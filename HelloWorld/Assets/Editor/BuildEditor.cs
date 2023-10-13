using UnityEditor;
using UnityEngine;
using Newtonsoft.Json;
using System.IO;
using xasset.editor;
using xasset;
using HybridCLR.Editor.Commands;
using System;
using System.Collections.Generic;

public class BuildEditor
{
    [MenuItem("Tools/BuildPlayer", false, (int)ToolsMenuSort.BuildPlayer)]
    public static void BuildPlayer()
    {
        string[] args = Environment.GetCommandLineArgs();
        string path = string.Empty;
        BuildOptions options = BuildOptions.None;
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
            else if (args[i].StartsWith("--develop:"))
            {
                bool b = bool.Parse(args[i].Replace("--develop:", string.Empty));
                if (b) options = BuildOptions.Development | BuildOptions.EnableDeepProfilingSupport | BuildOptions.AllowDebugging;
                Debug.Log(args[i]);
            }
        }
        BuildBundles();
        //BuildOptions options = EditorUserBuildSettings.development ? BuildOptions.Development : BuildOptions.None;
        HideSubScripts(true);
        BuildPipeline.BuildPlayer(EditorBuildSettings.scenes, path, EditorUserBuildSettings.activeBuildTarget, options);
        HideSubScripts(false);
    }
    private static void HideSubScripts(bool b)
    {
        string source = Application.dataPath + "\\Scripts\\SubScripts";
        string dest = Application.dataPath + "\\Scripts\\.SubScripts";
        if (b)
        {
            if (Directory.Exists(dest)) Directory.Delete(dest, true);
            Directory.Move(source, dest);
        }
        else
        {
            if (Directory.Exists(source)) Directory.Delete(source, true);
            Directory.Move(dest, source);
        }
        AssetDatabase.Refresh();
    }

    [MenuItem("Tools/BuildBundles", false, (int)ToolsMenuSort.BuildBundles)]
    public static void BuildBundles()
    {
        CopyConfig();
        HotAssemblyCompile.Generate();
        HybridCLRGenerate();
        XAssetBuild();
    }

    [MenuItem("Tools/CopyConfig", false, (int)ToolsMenuSort.CopyConfig)]
    public static void CopyConfig()
    {
        string path = Environment.CurrentDirectory.Substring(0, Environment.CurrentDirectory.LastIndexOf(@"\"));
        List<FileInfo> list = new List<FileInfo>();
        FileUtils.GetAllFilePath($"{path}\\Luban\\Client\\OutCodes", list);
        for (int i = 0; i < list.Count; i++)
        {
            string target = $"{Application.dataPath}\\Scripts\\SubScripts\\Config\\Auto\\{list[i].Name}";
            File.Copy(list[i].FullName, target, true);
        }
        list.Clear();
        FileUtils.GetAllFilePath($"{path}\\Luban\\Client\\OutBytes", list);
        for (int i = 0; i < list.Count; i++)
        {
            string target = $"{Application.dataPath}\\ZRes\\DataConfig\\{list[i].Name}";
            File.Copy(list[i].FullName, target, true);
        }
        AssetDatabase.Refresh();
    }
    [MenuItem("Tools/HybridCLRGenerate", false, (int)ToolsMenuSort.HybridCLRGenerate)]
    public static void HybridCLRGenerate()
    {
        HideSubScripts(true);
        PrebuildCommand.GenerateAll();
        TextAsset ta = AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/ZRes/GameConfig/HotUpdateConfig.txt");
        var config = JsonConvert.DeserializeObject<MainAssembly.HotUpdateConfig>(ta.text);
        string stripDir = HybridCLR.Editor.SettingsUtil.GetAssembliesPostIl2CppStripDir(EditorUserBuildSettings.activeBuildTarget);
        for (int i = 0; i < config.Metadata.Count; i++)
        {
            var name = config.Metadata[i];
            File.Copy($"{stripDir}/{name}.dll", $"{Application.dataPath}\\ZRes\\Assembly\\{name}.bytes", true);
        }
        HideSubScripts(false);
    }
    [MenuItem("Tools/XAssetBuild", false, (int)ToolsMenuSort.XAssetBuild)]
    public static void XAssetBuild()
    {
        MenuItems.BuildBundles();
        var versions = Utility.LoadFromFile<Versions>(Settings.GetCachePath(Versions.BundleFilename));
        //BuildUpdateInfo在BuildBundles时已经更新
        //Builder.BuildUpdateInfo(versions, hash, file.Length);
        Builder.BuildPlayerAssets(versions);
        File.Copy($"{Settings.PlatformCachePath}/updateinfo.json", $"{Settings.PlatformDataPath}/updateinfo.json", true);
    }
}
