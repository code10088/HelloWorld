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
using WeChatWASM;
using YooAsset;
using TTSDK.Tool;
using TTSDK.Tool.API;

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
    [MenuItem("Tools/BuildBundles", false, (int)ToolsMenuSort.BuildBundles)]
    public static void BuildBundles()
    {
        CheckArgs();
        Generate();
        HybridCLRGenerate();
        YooAssetBuild();
        UploadBundles2CDN();
    }

    private static void HideSubScripts(bool b)
    {
        string source = Application.dataPath + "/Scripts/SubScripts";
        string dest = Application.dataPath + "/Scripts/.SubScripts";
        if (b)
        {
            if (Directory.Exists(dest)) Directory.Delete(dest, true);
            if (Directory.Exists(source)) Directory.Move(source, dest);
        }
        else
        {
            if (Directory.Exists(source)) Directory.Delete(source, true);
            if (Directory.Exists(dest)) Directory.Move(dest, source);
        }
        AssetDatabase.Refresh();
    }

    private static void CheckArgs()
    {
        string[] args = Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i].StartsWith("--appversion:"))
            {
                appversion = args[i].Replace("--appversion:", string.Empty);
                string path = CustomerPreference.GetConfig<string>(CustomerPreferenceEnum.BuildPlayerPath);
                string str = File.ReadAllText($"{path}/VersionConfig.txt", Encoding.UTF8);
                var config = JsonConvert.DeserializeObject<VersionConfig>(str);
                int index = config.AppVersions.FindIndex(a => a == appversion);
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
            }
        }
    }

    [MenuItem("Tools/HotAssemblyCompile", false, (int)ToolsMenuSort.HotAssemblyCompile)]
    public static void Generate()
    {
        //新建代码工程名
        string newProjectName = "HotAssembly";
        //新建代码工程目录
        string newProjectPath = Environment.CurrentDirectory + "/" + newProjectName;

        string allStr = File.ReadAllText(newProjectPath + "/" + newProjectName + ".txt");
        string assemblyStr = File.ReadAllText(Environment.CurrentDirectory + "/Assembly-CSharp.csproj");
        int start = assemblyStr.IndexOf("<DefineConstants>") + 17;
        int end = assemblyStr.IndexOf("</DefineConstants>");
        string defineConstants = assemblyStr.Substring(start, end - start);
        allStr = allStr.Replace("DebugDefineConstants", defineConstants);
        string strEditorInstallPath = AppDomain.CurrentDomain.BaseDirectory.Replace("/", "\\");
        allStr = allStr.Replace("EditorInstallPath", strEditorInstallPath);

        List<FileInfo> fileList = new List<FileInfo>();
        FileUtils.GetAllFilePath(Application.dataPath + "/Scripts/SubScripts", fileList, ".cs");
        string dataPath = (Application.dataPath + "/").Replace("/", "\\");
        string str = "    <Compile Include=\"..\\Assets\\{0}\">\n       <Link>{1}</Link>\n    </Compile>\n";
        string replaceStr = "";
        for (int i = 0; i < fileList.Count; i++)
        {
            if (fileList[i].FullName.Contains("\\Editor\\")) continue;
            string tempStr = fileList[i].FullName.Replace(dataPath, "");
            tempStr = string.Format(str, tempStr, fileList[i].Name);
            replaceStr += tempStr;
        }
        allStr = allStr.Replace("    LinkScripts", replaceStr);
        File.WriteAllText(newProjectPath + "/" + newProjectName + ".csproj", allStr);

        //生成dll
        string strMSBuildPath = CustomerPreference.GetConfig<string>(CustomerPreferenceEnum.MSBuildPath);
        if (string.IsNullOrEmpty(strMSBuildPath) || !File.Exists(strMSBuildPath))
        {
            GameDebug.LogError("需要配置MSBuild路径：Preference->Customer->MSBuild路径");
            throw new Exception();
        }
        try
        {
            string strParam = newProjectPath + "/" + newProjectName + ".sln /t:Rebuild /p:Configuration=Debug";
            System.Diagnostics.Process msbuild = System.Diagnostics.Process.Start(strMSBuildPath, strParam);
            msbuild.WaitForExit();
            msbuild.Close();
            File.Copy($"{Environment.CurrentDirectory}\\HotAssembly\\obj\\Debug\\HotAssembly.dll", $"{Application.dataPath}\\ZRes\\Assembly\\HotAssembly.bytes", true);
            AssetDatabase.Refresh();
        }
        catch (Exception e)
        {
            GameDebug.LogError(e.Message);
            throw e;
        }
    }

    [MenuItem("Tools/HybridCLRGenerate", false, (int)ToolsMenuSort.HybridCLRGenerate)]
    public static void HybridCLRGenerate()
    {
        try
        {
            HideSubScripts(true);
            PrebuildCommand.GenerateAll();
            HideSubScripts(false);
            TextAsset ta = AssetDatabase.LoadAssetAtPath<TextAsset>(GameSetting.HotUpdateConfigPath);
            var config = JsonConvert.DeserializeObject<HotUpdateConfig>(ta.text);
            string stripDir = HybridCLR.Editor.SettingsUtil.GetAssembliesPostIl2CppStripDir(EditorUserBuildSettings.activeBuildTarget);
            for (int i = 0; i < config.Metadata.Count; i++)
            {
                var path = config.Metadata[i];
                var name = Path.GetFileNameWithoutExtension(path);
                File.Copy($"{stripDir}/{name}.dll", $"{Environment.CurrentDirectory}/{path}", true);
            }
        }
        catch (Exception e)
        {
            HideSubScripts(false);
            GameDebug.LogError(e.Message);
            throw e;
        }
    }

    [MenuItem("Tools/YooAssetBuild", false, (int)ToolsMenuSort.YooAssetBuild)]
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
        buildParameters.LegacyDependency = false;
        buildParameters.ClearBuildCacheFiles = false;
        buildParameters.UseAssetDependencyDB = true;
        buildParameters.EnableSharePackRule = false;
        buildParameters.SingleReferencedPackAlone = true;
        buildParameters.VerifyBuildingResult = true;
        buildParameters.FileNameStyle = EFileNameStyle.BundleName_HashName;
        buildParameters.BuildinFileCopyOption = EBuildinFileCopyOption.ClearAndCopyByTags;
        buildParameters.BuildinFileCopyParams = "Builtin";
        buildParameters.EncryptionServices = new EncryptionServices();
        buildParameters.CompressOption = ECompressOption.LZ4;
        buildParameters.DisableWriteTypeTree = false;
        buildParameters.IgnoreTypeTreeChanges = true;
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

    [MenuItem("Tools/UploadBundles2CDN", false, (int)ToolsMenuSort.UploadBundles2CDN)]
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
                GameDebug.LogError("upload bundle fail：" + path);
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

    private static void BuildPlayer()
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
    private static void AndroidBuild()
    {
        string time = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        string buildPlayerPath = CustomerPreference.GetConfig<string>(CustomerPreferenceEnum.BuildPlayerPath);
        buildPlayerPath = $"{buildPlayerPath}\\{EditorUserBuildSettings.activeBuildTarget}\\{appversion}\\{time}\\{GameSetting.AppName}";
        Directory.CreateDirectory(Path.GetDirectoryName(buildPlayerPath));

        try
        {
            HideSubScripts(true);
            BuildPipeline.BuildPlayer(EditorBuildSettings.scenes, buildPlayerPath, EditorUserBuildSettings.activeBuildTarget, options);
            HideSubScripts(false);
            if (latestAppVersion) UploadFile2CDN($"{EditorUserBuildSettings.activeBuildTarget}/{GameSetting.AppName}", buildPlayerPath);
        }
        catch (Exception e)
        {
            HideSubScripts(false);
            GameDebug.LogError(e.Message);
            throw e;
        }
    }
    private static void IOSBuild()
    {
        string time = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        string buildPlayerPath = CustomerPreference.GetConfig<string>(CustomerPreferenceEnum.BuildPlayerPath);
        buildPlayerPath = $"{buildPlayerPath}\\{EditorUserBuildSettings.activeBuildTarget}\\{appversion}\\{time}\\{GameSetting.AppName}";
        Directory.CreateDirectory(Path.GetDirectoryName(buildPlayerPath));

        try
        {
            HideSubScripts(true);
            BuildPipeline.BuildPlayer(EditorBuildSettings.scenes, buildPlayerPath, EditorUserBuildSettings.activeBuildTarget, options);
            HideSubScripts(false);
        }
        catch (Exception e)
        {
            HideSubScripts(false);
            GameDebug.LogError(e.Message);
            throw e;
        }
    }
    [MenuItem("Tools/WeChatBuild", false, (int)ToolsMenuSort.WeChatBuild)]
    public static void WeChatBuild()
    {
        CheckAppVersion();
        WXConvertCore.config.ProjectConf.CDN = GameSetting.CDNPlatform;
        WXConvertCore.config.ProjectConf.bundlePathIdentifier = appversion;
        EditorUtility.SetDirty(WXConvertCore.config);
        AssetDatabase.SaveAssets();

        try
        {
            HideSubScripts(true);
            WXConvertCore.WXExportError result;
            result = WXConvertCore.DoExport();
            HideSubScripts(false);
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
            HideSubScripts(false);
            GameDebug.LogError(e.Message);
            throw e;
        }
    }
    [MenuItem("Tools/TTBuild", false, (int)ToolsMenuSort.TTBuild)]
    public static async void TTBuild()
    {
        try
        {
            HideSubScripts(true);
            await BuildManager.Build(Framework.Wasm);
            HideSubScripts(false);
            string fileName = Path.GetFileNameWithoutExtension(StarkBuilderSettings.Instance.webglPackagePath);
            string fullName = Path.GetFileName(StarkBuilderSettings.Instance.webglPackagePath);
            UploadFile2CDN($"{EditorUserBuildSettings.activeBuildTarget}/{fileName}", fullName);
        }
        catch (Exception e)
        {
            HideSubScripts(false);
            GameDebug.LogError(e.Message);
            throw e;
        }
    }
}
