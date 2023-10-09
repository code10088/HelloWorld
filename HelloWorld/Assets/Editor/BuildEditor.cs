using UnityEditor;
using UnityEngine;
using Newtonsoft.Json;
using System.IO;
using xasset.editor;
using xasset;
using HybridCLR.Editor.Commands;

public enum ToolsMenuSort
{
    BuildBundlesFast,
    BuildBundles,
    CopyConfig,
    HotAssemblyCompile,
    CopyMetadata,
    XAssetBuild,
}
public class BuildEditor
{
    [MenuItem("Tools/BuildBundlesFast", false, (int)ToolsMenuSort.BuildBundlesFast)]
    public static void BuildBundlesFast()
    {
        CopyConfig();
        HotAssemblyCompile.Generate();
        CopyMetadata();
        XAssetBuild();
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
        string path = System.Environment.CurrentDirectory.Substring(0, System.Environment.CurrentDirectory.LastIndexOf(@"\"));
        FileUtil.ReplaceDirectory($"{path}\\Luban\\Client\\OutCodes", $"{Application.dataPath}\\Scripts\\SubScripts\\Config\\Auto");
        FileUtil.ReplaceDirectory($"{path}\\Luban\\Client\\OutBytes", $"{Application.dataPath}\\ZRes\\DataConfig");
        AssetDatabase.Refresh();
    }
    [MenuItem("Tools/CopyMatedata", false, (int)ToolsMenuSort.CopyMetadata)]
    public static void CopyMetadata()
    {
        string platform = "Windows";
#if UNITY_ANDROID
        platform = "Android";
#elif UNITY_IOS
        platform = "IOS";
#endif
        TextAsset ta = AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/ZRes/GameConfig/HotUpdateConfig.txt");
        var config = JsonConvert.DeserializeObject<MainAssembly.HotUpdateConfig>(ta.text);
        for (int i = 0; i < config.Metadata.Count; i++)
        {
            var name = Path.GetFileNameWithoutExtension(config.Metadata[i]);
            File.Copy($"{System.Environment.CurrentDirectory}\\HybridCLRData\\AssembliesPostIl2CppStrip\\{platform}\\{name}.dll", $"{Application.dataPath}\\ZRes\\Assembly\\{name}.bytes", true);
        }
        File.Copy($"{System.Environment.CurrentDirectory}\\HotAssembly\\obj\\Debug\\HotAssembly.dll", $"{Application.dataPath}\\ZRes\\Assembly\\HotAssembly.bytes", true);
        AssetDatabase.Refresh();
    }
    [MenuItem("Tools/XAssetBuild", false, (int)ToolsMenuSort.XAssetBuild)]
    public static void XAssetBuild()
    {
        MenuItems.BuildBundles();
        var versions = Utility.LoadFromFile<Versions>(Settings.GetCachePath(Versions.BundleFilename));
        //BuildUpdateInfo在BuildBundles时已经更新
        //Builder.BuildUpdateInfo(versions, hash, file.Length);
        Builder.BuildPlayerAssets(versions);
    }
}
