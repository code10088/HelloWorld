using UnityEditor;
using UnityEngine;
using Newtonsoft.Json;
using System.IO;
using System;
using System.Collections.Generic;
using COSXML;
using COSXML.Auth;
using COSXML.Model.Object;
using YooAsset.Editor;
using System.Text;
using YooAsset;
using Obfuz4HybridCLR;
using Obfuz.Settings;

#if WEIXINMINIGAME
using WeChatWASM;
#elif DOUYINMINIGAME
using TTSDK.Tool;
using TTSDK.Tool.API;
#endif

public class BuildEditor
{
    private static BuildOptions options = BuildOptions.None;
    private static string appversion = string.Empty;
    private static string resversion = string.Empty;
    private static bool latestAppVersion = false;

    public static void Build()
    {
        BuildBundles();
        BuildPlayer();
    }
    [MenuItem("Tools/Build/BuildBundles", false, (int)ToolsMenuSort.BuildBundles)]
    public static void BuildBundles()
    {
        CheckArgs();
        HybridCLRGenerate();
        YooAssetBuild();
        UploadBundles2CDN();
    }

    private static void CheckArgs()
    {
        string[] args = Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i].StartsWith("--platform:"))
            {
                string platform = args[i].Replace("--platform:", string.Empty);
                switch (platform)
                {
                    case "Android":
                        break;
                    case "iOS":
                        break;
                    case "WX":
                        GameEditorTools.AddScriptingDefineSymbols("WEIXINMINIGAME");
                        GameEditorTools.RemoveScriptingDefineSymbols("DOUYINMINIGAME");
                        break;
                    case "TT":
                        GameEditorTools.RemoveScriptingDefineSymbols("WEIXINMINIGAME");
                        GameEditorTools.AddScriptingDefineSymbols("DOUYINMINIGAME");
                        break;
                }
            }
            else if (args[i].StartsWith("--appversion:"))
            {
                appversion = args[i].Replace("--appversion:", string.Empty);
                string path = CustomerPreference.GetConfig<string>(CustomerPreferenceEnum.BuildPlayerPath);
                string str = File.ReadAllText($"{path}/VersionConfig.txt", Encoding.UTF8);
                var config = JsonConvert.DeserializeObject<VersionConfig>(str);
                int index = Array.FindIndex(config.AppVersions, a => a == appversion);
                index = Math.Max(index, 0);
                latestAppVersion = index == 0;
                appversion = config.AppVersions[index];
                PlayerSettings.bundleVersion = appversion;
                GameDebug.Log("appversion:" + appversion);
                resversion = config.ResVersions[index];
                GameDebug.Log("resversion:" + resversion);
            }
            else if (args[i].StartsWith("--development:"))
            {
                bool b = bool.Parse(args[i].Replace("--development:", string.Empty));
                if (b) options = BuildOptions.Development | BuildOptions.EnableDeepProfilingSupport | BuildOptions.AllowDebugging;
            }
            else if (args[i].StartsWith("--debug:"))
            {
                bool b = bool.Parse(args[i].Replace("--debug:", string.Empty));
                if (b) GameEditorTools.AddScriptingDefineSymbols("Debug");
                else GameEditorTools.RemoveScriptingDefineSymbols("Debug");
                ObfuzSettings.Instance.buildPipelineSettings.enable = !b;
                ObfuzSettings.Save();
            }
        }
        AssetDatabase.Refresh();
    }

    [MenuItem("Tools/Build/HybridCLRGenerate", false, (int)ToolsMenuSort.HybridCLRGenerate)]
    public static void HybridCLRGenerate()
    {
        try
        {
            PrebuildCommandExt.GenerateAll();
            TextAsset ta = AssetDatabase.LoadAssetAtPath<TextAsset>(GameSetting.HotUpdateConfigPath);
            var config = JsonConvert.DeserializeObject<HotUpdateConfig>(ta.text);
            var obfuscatedHotUpdateDir = PrebuildCommandExt.GetObfuscatedHotUpdateAssemblyOutputPath(EditorUserBuildSettings.activeBuildTarget);
            var stripDir = HybridCLR.Editor.SettingsUtil.GetAssembliesPostIl2CppStripDir(EditorUserBuildSettings.activeBuildTarget);
            var hotUpdateDir = HybridCLR.Editor.SettingsUtil.GetHotUpdateDllsOutputDirByTarget(EditorUserBuildSettings.activeBuildTarget);
            for (int i = 0; i < config.HotAssembly.Length; i++)
            {
                var dest = config.HotAssembly[i];
                var name = Path.GetFileNameWithoutExtension(dest);
                var source = string.Empty;
                if (File.Exists($"{obfuscatedHotUpdateDir}/{name}")) source = $"{obfuscatedHotUpdateDir}/{name}";
                else if (File.Exists($"{stripDir}/{name}")) source = $"{stripDir}/{name}";
                else source = $"{hotUpdateDir}/{name}";
                File.Copy(source, $"{Environment.CurrentDirectory}/{dest}", true);
            }
            AssetDatabase.Refresh();
        }
        catch (Exception e)
        {
            GameDebug.LogError(e.Message);
            throw e;
        }
    }

    [MenuItem("Tools/Build/YooAssetBuild", false, (int)ToolsMenuSort.YooAssetBuild)]
    public static void YooAssetBuild()
    {
        CheckAppVersion();
        var buildParameters = new ScriptableBuildParameters();
        buildParameters.BuildOutputRoot = AssetBundleBuilderHelper.GetDefaultBuildOutputRoot();
        buildParameters.BuildinFileRoot = AssetBundleBuilderHelper.GetStreamingAssetsRoot();
        buildParameters.BuildPipeline = EBuildPipeline.ScriptableBuildPipeline.ToString();
        buildParameters.BuildBundleType = (int)EBuildBundleType.AssetBundle;
        buildParameters.BuildTarget = EditorUserBuildSettings.activeBuildTarget;
        buildParameters.PackageName = AssetManager.PackageName;
        buildParameters.PackageVersion = resversion;
        buildParameters.PackageNote = string.Empty;
        buildParameters.ClearBuildCacheFiles = false;
        buildParameters.UseAssetDependencyDB = true;
        buildParameters.EnableSharePackRule = false;
        buildParameters.SingleReferencedPackAlone = true;
        buildParameters.VerifyBuildingResult = true;
        buildParameters.FileNameStyle = EFileNameStyle.BundleName_HashName;
        buildParameters.BuildinFileCopyOption = EBuildinFileCopyOption.ClearAndCopyByTags;
        buildParameters.BuildinFileCopyParams = "Builtin";
        buildParameters.EncryptionServices = new EncryptionServices();
        buildParameters.ManifestProcessServices = null;
        buildParameters.ManifestRestoreServices = null;
        buildParameters.CompressOption = ECompressOption.LZ4;
        buildParameters.StripUnityVersion = false;
        buildParameters.DisableWriteTypeTree = false;
        buildParameters.IgnoreTypeTreeChanges = true;
        buildParameters.TrackSpriteAtlasDependencies = true;
        buildParameters.WriteLinkXML = true;

        try
        {
            ScriptableBuildPipeline pipeline = new ScriptableBuildPipeline();
            var buildResult = pipeline.Run(buildParameters, true);
            if (buildResult.Success) GameDebug.Log("build success");
            else throw new Exception($"build fail:{buildResult.ErrorInfo}");
        }
        catch (Exception e)
        {
            GameDebug.LogError(e.Message);
            throw e;
        }
    }
    private static void CheckAppVersion()
    {
        if (string.IsNullOrEmpty(appversion))
        {
            string path = CustomerPreference.GetConfig<string>(CustomerPreferenceEnum.BuildPlayerPath);
            string str = File.ReadAllText($"{path}/VersionConfig.txt", Encoding.UTF8);
            var config = JsonConvert.DeserializeObject<VersionConfig>(str);
            appversion = config.AppVersions[0];
            resversion = config.ResVersions[0];
        }
    }

    [MenuItem("Tools/Build/UploadBundles2CDN", false, (int)ToolsMenuSort.UploadBundles2CDN)]
    public static void UploadBundles2CDN()
    {
        CheckAppVersion();
        CosBucketConfig cosBucketConfig = CustomerPreference.GetConfig<CosBucketConfig>(CustomerPreferenceEnum.CosBucketConfig);
        CosXmlConfig cosXmlConfig = new CosXmlConfig.Builder().SetRegion("ap-beijing").Build();
        long durationSecond = 600;
        QCloudCredentialProvider qCloudCredentialProvider = new DefaultQCloudCredentialProvider(cosBucketConfig.SecretId, cosBucketConfig.SecretKey, durationSecond);
        CosXml cosXml = new CosXmlServer(cosXmlConfig, qCloudCredentialProvider);

        string packageOutputDir = AssetBundleBuilderHelper.GetDefaultBuildOutputRoot();
        packageOutputDir = $"{packageOutputDir}/{EditorUserBuildSettings.activeBuildTarget}/{AssetManager.PackageName}/{resversion}";
        List<FileInfo> fileList = new List<FileInfo>();
        FileUtils.GetAllFilePath(packageOutputDir, fileList);
        for (int i = 0; i < fileList.Count; i++)
        {
            string path = fileList[i].FullName;
            string key = $"{EditorUserBuildSettings.activeBuildTarget}/{appversion}/{fileList[i].Name}";
            PutObjectRequest request = new PutObjectRequest(cosBucketConfig.Name, key, path);
            PutObjectResult result = cosXml.PutObject(request);
            if (result.IsSuccessful())
            {
                GameDebug.Log("upload file success:" + path);
            }
            else
            {
                GameDebug.LogError("upload bundle fail£º" + path);
                GameDebug.LogError(result.GetResultInfo());
            }
        }

        string buildPlayerPath = CustomerPreference.GetConfig<string>(CustomerPreferenceEnum.BuildPlayerPath);
        UploadFile2CDN($"{EditorUserBuildSettings.activeBuildTarget}/VersionConfig.txt", $"{buildPlayerPath}/VersionConfig.txt");
    }
    private static void UploadFile2CDN(string key, string path)
    {
        CosBucketConfig cosBucketConfig = CustomerPreference.GetConfig<CosBucketConfig>(CustomerPreferenceEnum.CosBucketConfig);
        CosXmlConfig cosXmlConfig = new CosXmlConfig.Builder().SetRegion("ap-beijing").Build();
        long durationSecond = 600;
        QCloudCredentialProvider qCloudCredentialProvider = new DefaultQCloudCredentialProvider(cosBucketConfig.SecretId, cosBucketConfig.SecretKey, durationSecond);
        CosXml cosXml = new CosXmlServer(cosXmlConfig, qCloudCredentialProvider);

        key = key.Replace("\\", "/");
        PutObjectRequest request = new PutObjectRequest(cosBucketConfig.Name, key, path);
        PutObjectResult result = cosXml.PutObject(request);
        if (result.IsSuccessful())
        {
            GameDebug.Log("upload file success:" + path);
        }
        else
        {
            GameDebug.LogError("upload file fail:" + path);
            GameDebug.LogError(result.GetResultInfo());
        }
    }

    [MenuItem("Tools/Build/BuildProject", false, (int)ToolsMenuSort.ExportProject)]
    public static void BuildPlayer()
    {
#if UNITY_ANDROID
        AndroidBuild();
#elif UNITY_IOS
        IOSBuild();
#elif WEIXINMINIGAME
        WeChatBuild();
#elif DOUYINMINIGAME
        TTBuild();
#endif
    }
#if UNITY_ANDROID
    private static void AndroidBuild()
    {
        string time = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        string buildPlayerPath = CustomerPreference.GetConfig<string>(CustomerPreferenceEnum.BuildPlayerPath);
        buildPlayerPath = $"{buildPlayerPath}\\{EditorUserBuildSettings.activeBuildTarget}\\{appversion}\\{time}\\{GameSetting.AppName}";
        Directory.CreateDirectory(Path.GetDirectoryName(buildPlayerPath));

        try
        {
            BuildPipeline.BuildPlayer(EditorBuildSettings.scenes, buildPlayerPath, EditorUserBuildSettings.activeBuildTarget, options);
            if (latestAppVersion) UploadFile2CDN($"{EditorUserBuildSettings.activeBuildTarget}/{GameSetting.AppName}", buildPlayerPath);
        }
        catch (Exception e)
        {
            GameDebug.LogError(e.Message);
            throw e;
        }
    }
#endif
#if UNITY_IOS
    private static void IOSBuild()
    {
        string time = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        string buildPlayerPath = CustomerPreference.GetConfig<string>(CustomerPreferenceEnum.BuildPlayerPath);
        buildPlayerPath = $"{buildPlayerPath}\\{EditorUserBuildSettings.activeBuildTarget}\\{appversion}\\{time}\\{GameSetting.AppName}";
        Directory.CreateDirectory(Path.GetDirectoryName(buildPlayerPath));

        try
        {
            BuildPipeline.BuildPlayer(EditorBuildSettings.scenes, buildPlayerPath, EditorUserBuildSettings.activeBuildTarget, options);
        }
        catch (Exception e)
        {
            GameDebug.LogError(e.Message);
            throw e;
        }
    }
#endif
#if WEIXINMINIGAME
    private static void WeChatBuild()
    {
        CheckAppVersion();
        WXConvertCore.config.ProjectConf.CDN = GameSetting.CDNPlatform;
        WXConvertCore.config.ProjectConf.bundlePathIdentifier = appversion;
        EditorUtility.SetDirty(WXConvertCore.config);
        AssetDatabase.SaveAssets();

        try
        {
            WXConvertCore.WXExportError result;
            result = WXConvertCore.DoExport();
            if (result == WXConvertCore.WXExportError.SUCCEED)
            {
                string packageOutputDir = $"{WXConvertCore.config.ProjectConf.DST}/webgl";
                DirectoryInfo dir = new DirectoryInfo(packageOutputDir);
                FileInfo[] fileInfos = dir.GetFiles();
                for (int i = 0; i < fileInfos.Length; i++)
                {
                    string name = fileInfos[i].Name;
                    if (name.EndsWith(".webgl.data.unityweb.bin.txt"))
                    {
                        UploadFile2CDN($"{EditorUserBuildSettings.activeBuildTarget}/{name}", fileInfos[i].FullName);
                    }
                }
            }
        }
        catch (Exception e)
        {
            GameDebug.LogError(e.Message);
            throw e;
        }
    }
#endif
#if DOUYINMINIGAME
    private static async void TTBuild()
    {
        try
        {
            await BuildManager.Build(Framework.Wasm);
            string fullName = StarkBuilderSettings.Instance.webglPackagePath;
            string fileName = Path.GetFileName(fullName);
            UploadFile2CDN($"{EditorUserBuildSettings.activeBuildTarget}/{fileName}", fullName);
        }
        catch (Exception e)
        {
            GameDebug.LogError(e.Message);
            throw e;
        }
    }
#endif
}
