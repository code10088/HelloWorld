using UnityEditor;
using UnityEngine;
using Newtonsoft.Json;
using System.IO;
using HybridCLR.Editor.Commands;
using System;
using System.Collections.Generic;
using COSXML;
using COSXML.Auth;
using COSXML.Model.Object;
using YooAsset.Editor;
using System.Text;

public class BuildEditor
{
    private static BuildOptions options = BuildOptions.None;
    private static string appversion = "1.0.0";
    private static string resversion = "1.0.0.001";
    private static string buildPath;

    private static string BuildPath
    {
        get
        {
            if (string.IsNullOrEmpty(buildPath))
            {
                buildPath = Environment.CurrentDirectory.Substring(0, Environment.CurrentDirectory.LastIndexOf(@"\"));
                buildPath = $"{buildPath}\\Build";
                Directory.CreateDirectory(Path.GetDirectoryName(buildPath));
            }
            return buildPath;
        }
    }

    public static void Build()
    {
        BuildBundles();
        BuildPlayer();
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
        CheckArgs();
        CopyConfig();
        HotAssemblyCompile.Generate();
        HybridCLRGenerate();
        YooAssetBuild();
        UploadBundles2CDN();
    }
    private static void CheckArgs()
    {
        options = BuildOptions.Development | BuildOptions.EnableDeepProfilingSupport | BuildOptions.AllowDebugging;
        string[] args = Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i].StartsWith("--appversion:"))
            {
                appversion = args[i].Replace("--appversion:", string.Empty);
                string str = File.ReadAllText($"{BuildPath}/VersionConfig.txt", Encoding.UTF8);
                var config = JsonConvert.DeserializeObject<VersionConfig>(str);
                int index = config.AppVersions.FindIndex(a => a == appversion);
                if (index < 0) index = config.AppVersions.Count - 1;
                appversion = config.AppVersions[index];
                PlayerSettings.bundleVersion = appversion;
                Debug.Log("appversion:" + appversion);
                resversion = config.ResVersions[index];
                Debug.Log("resversion:" + resversion);
            }
            else if (args[i].StartsWith("--release:"))
            {
                bool b = bool.Parse(args[i].Replace("--release:", string.Empty));
                if (b) options = BuildOptions.None;
            }
        }
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
    [MenuItem("Tools/YooAssetBuild", false, (int)ToolsMenuSort.YooAssetBuild)]
    public static void YooAssetBuild()
    {
        var buildParameters = new BuiltinBuildParameters();
        buildParameters.BuildOutputRoot = AssetBundleBuilderHelper.GetDefaultBuildOutputRoot();
        buildParameters.BuildinFileRoot = AssetBundleBuilderHelper.GetStreamingAssetsRoot();
        buildParameters.BuildPipeline = EBuildPipeline.BuiltinBuildPipeline.ToString();
        buildParameters.BuildTarget = EditorUserBuildSettings.activeBuildTarget;
        buildParameters.BuildMode = EBuildMode.IncrementalBuild;
        buildParameters.PackageName = AssetManager.PackageName;
        buildParameters.PackageVersion = resversion;
        buildParameters.EnableSharePackRule = false;
        buildParameters.VerifyBuildingResult = true;
        buildParameters.FileNameStyle = EFileNameStyle.HashName;
        buildParameters.BuildinFileCopyOption = EBuildinFileCopyOption.ClearAndCopyByTags;
        buildParameters.BuildinFileCopyParams = "Builtin";
        buildParameters.EncryptionServices = new EncryptionServices();
        buildParameters.CompressOption = ECompressOption.LZ4;

        BuiltinBuildPipeline pipeline = new BuiltinBuildPipeline();
        var buildResult = pipeline.Run(buildParameters, true);
        if (buildResult.Success) Debug.Log("build success");
        else Debug.LogError($"build fail:{buildResult.ErrorInfo}");
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
        string packageOutputDir = AssetBundleBuilderHelper.GetDefaultBuildOutputRoot();
        packageOutputDir = $"{packageOutputDir}/{EditorUserBuildSettings.activeBuildTarget}/{AssetManager.PackageName}/{resversion}";
        List<FileInfo> fileList = new List<FileInfo>();
        FileUtils.GetAllFilePath(packageOutputDir, fileList);
        for (int i = 0; i < fileList.Count; i++)
        {
            string path = fileList[i].FullName;
            string key = $"{EditorUserBuildSettings.activeBuildTarget}/{appversion}/{fileList[i].Name}";
            PutObjectRequest request = new PutObjectRequest(bucket, key, path);
            PutObjectResult result = cosXml.PutObject(request);
            if (!result.IsSuccessful())
            {
                Debug.LogError("upload bundle fail£º" + path);
                Debug.LogError(result.GetResultInfo());
                return;
            }
        }
        Debug.Log("upload bundle success");

        UploadFile2CDN($"{EditorUserBuildSettings.activeBuildTarget}/VersionConfig.txt", $"{BuildPath}/VersionConfig.txt");
        Debug.Log("upload version config success");
    }
    private static void UploadFile2CDN(string key, string path)
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
            Debug.LogError("upload file fail:" + path);
            Debug.LogError(result.GetResultInfo());
            return;
        }
        Debug.Log("upload file success:" + path);
    }

    private static void BuildPlayer()
    {
        string time = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        string buildPlayerPath = $"{BuildPath}\\{EditorUserBuildSettings.activeBuildTarget}\\{appversion}\\{time}\\{GameSetting.AppName}";
        Directory.CreateDirectory(Path.GetDirectoryName(buildPlayerPath));

        HideSubScripts(true);
        BuildPipeline.BuildPlayer(EditorBuildSettings.scenes, buildPlayerPath, EditorUserBuildSettings.activeBuildTarget, options);
        HideSubScripts(false);
        UploadFile2CDN($"{EditorUserBuildSettings.activeBuildTarget}/{GameSetting.AppName}", buildPlayerPath);
    }
}
