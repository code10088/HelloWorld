using UnityEditor;
using UnityEngine;
using Newtonsoft.Json;
using System.IO;
using xasset.editor;
using xasset;
using HybridCLR.Editor.Commands;
using System;
using System.Collections.Generic;
using COSXML;
using COSXML.Auth;
using COSXML.Model.Object;

public class BuildEditor
{
    [MenuItem("Tools/BuildPlayer", false, (int)ToolsMenuSort.BuildPlayer)]
    public static void BuildPlayer()
    {
        string path = Environment.CurrentDirectory.Substring(0, Environment.CurrentDirectory.LastIndexOf(@"\"));
        path = $"{path}\\Build\\{DateTime.Now.ToString("yyyyMMdd_HHmmss")}\\HelloWorld.apk";
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        PlayerSettings.bundleVersion = "1.0.0";
        BuildOptions options = BuildOptions.Development | BuildOptions.EnableDeepProfilingSupport | BuildOptions.AllowDebugging;
        string[] args = Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i].StartsWith("--version:"))
            {
                PlayerSettings.bundleVersion = args[i].Replace("--version:", string.Empty);
                Debug.Log(args[i]);
            }
            else if (args[i].StartsWith("--release:"))
            {
                bool b = bool.Parse(args[i].Replace("--release:", string.Empty));
                if (b) options = BuildOptions.None;
                Debug.Log(args[i]);
            }
        }
        BuildBundles();
        HideSubScripts(true);
        BuildPipeline.BuildPlayer(EditorBuildSettings.scenes, path, EditorUserBuildSettings.activeBuildTarget, options);
        HideSubScripts(false);
        UploadFile2CDN(string.Empty, path);
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
        UploadBundles2CDN();
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
        var config = JsonConvert.DeserializeObject<HotUpdateConfig>(ta.text);
        string stripDir = HybridCLR.Editor.SettingsUtil.GetAssembliesPostIl2CppStripDir(EditorUserBuildSettings.activeBuildTarget);
        for (int i = 0; i < config.Metadata.Count; i++)
        {
            var path = config.Metadata[i];
            var name = Path.GetFileNameWithoutExtension(path);
            File.Copy($"{stripDir}/{name}.dll", $"{Environment.CurrentDirectory}/{path}", true);
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
    [MenuItem("Tools/UploadBundles2CDN", false, (int)ToolsMenuSort.UploadBundles2CDN)]
    public static void UploadBundles2CDN()
    {
        CosXmlConfig config = new CosXmlConfig.Builder().SetRegion("ap-beijing").Build();
        string secretId = "AKIDHSjP5iQG1byLl3mNnnolv3879KYj2OpJ";
        string secretKey = "jKnx21ADT1OfkpyGRSLHPVXgjkhllfS6";
        long durationSecond = 600;
        QCloudCredentialProvider qCloudCredentialProvider = new DefaultQCloudCredentialProvider(secretId, secretKey, durationSecond);
        CosXml cosXml = new CosXmlServer(config, qCloudCredentialProvider);

        string bucket = "assets-1321503079";
        int dir = Settings.PlatformDataPath.IndexOf("Bundles");
        List<FileInfo> fileList = new List<FileInfo>();
        FileUtils.GetAllFilePath(Settings.PlatformDataPath, fileList);
        for (int i = 0; i < fileList.Count; i++)
        {
            string path = fileList[i].FullName;
            string key = path.Substring(dir).Replace("\\", "/");
            PutObjectRequest request = new PutObjectRequest(bucket, key, path);
            PutObjectResult result = cosXml.PutObject(request);
            if (!result.IsSuccessful())
            {
                Debug.Log("UploadBundleFail：" + path);
                Debug.Log(result.GetResultInfo());
                return;
            }
        }
        Debug.Log("UploadBundleSuccess");
    }
    public static void UploadFile2CDN(string key, string path)
    {
        CosXmlConfig config = new CosXmlConfig.Builder().SetRegion("ap-beijing").Build();
        string secretId = "AKIDHSjP5iQG1byLl3mNnnolv3879KYj2OpJ";
        string secretKey = "jKnx21ADT1OfkpyGRSLHPVXgjkhllfS6";
        long durationSecond = 600;
        QCloudCredentialProvider qCloudCredentialProvider = new DefaultQCloudCredentialProvider(secretId, secretKey, durationSecond);
        CosXml cosXml = new CosXmlServer(config, qCloudCredentialProvider);

        string bucket = "assets-1321503079";
        key = key.Replace("\\", "/");
        PutObjectRequest request = new PutObjectRequest(bucket, key, path);
        PutObjectResult result = cosXml.PutObject(request);
        if (!result.IsSuccessful())
        {
            Debug.Log("UploadFileFail：" + path);
            Debug.Log(result.GetResultInfo());
            return;
        }
        Debug.Log("UploadFileSuccess：" + path);
    }
}
