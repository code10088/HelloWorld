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
    private static void HideSubScripts(bool b)
    {
        string source = Application.dataPath + "/Scripts/SubScripts";
        string dest = Application.dataPath + "/Scripts/.SubScripts";
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
        Generate();
        HybridCLRGenerate();
        YooAssetBuild();
        UploadBundles2CDN();
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
                Debug.Log("appversion:" + appversion);
                resversion = config.ResVersions[index];
                Debug.Log("resversion:" + resversion);
            }
            else if (args[i].StartsWith("--release:"))
            {
                bool b = bool.Parse(args[i].Replace("--release:", string.Empty));
                if (!b) options = BuildOptions.Development | BuildOptions.EnableDeepProfilingSupport | BuildOptions.AllowDebugging;
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
            EditorUtility.DisplayDialog("Error", "需要配置MSBuild路径：Preference->Customer->MSBuild路径", "Ok");
        }
        else
        {
            try
            {
                string strParam = newProjectPath + "/" + newProjectName + ".sln /t:Rebuild /p:Configuration=Debug";
                System.Diagnostics.Process msbuild = System.Diagnostics.Process.Start(strMSBuildPath, strParam);
                msbuild.WaitForExit();
                msbuild.Close();
                File.Copy($"{Environment.CurrentDirectory}\\HotAssembly\\obj\\Debug\\HotAssembly.dll", $"{Application.dataPath}\\ZRes\\Assembly\\HotAssembly.bytes", true);
            }
            catch (Exception e)
            {
                GameDebug.LogError(e.Message);
                throw e;
            }
            AssetDatabase.Refresh();
        }
    }

    [MenuItem("Tools/HybridCLRGenerate", false, (int)ToolsMenuSort.HybridCLRGenerate)]
    public static void HybridCLRGenerate()
    {
        HideSubScripts(true);
        try
        {
            PrebuildCommand.GenerateAll();
        }
        catch (Exception e)
        {
            HideSubScripts(false);
            GameDebug.LogError(e.Message);
            throw e;
        }
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

    [MenuItem("Tools/YooAssetBuild", false, (int)ToolsMenuSort.YooAssetBuild)]
    public static void YooAssetBuild()
    {
        CheckAppVersion();
        var buildParameters = new ScriptableBuildParameters();
        buildParameters.BuildOutputRoot = AssetBundleBuilderHelper.GetDefaultBuildOutputRoot();
        buildParameters.BuildinFileRoot = AssetBundleBuilderHelper.GetStreamingAssetsRoot();
        buildParameters.BuildPipeline = EBuildPipeline.ScriptableBuildPipeline.ToString();
        buildParameters.BuildTarget = EditorUserBuildSettings.activeBuildTarget;
        buildParameters.BuildMode = EBuildMode.IncrementalBuild;
        buildParameters.PackageName = AssetManager.PackageName;
        buildParameters.PackageVersion = resversion;
        buildParameters.EnableSharePackRule = false;
        buildParameters.VerifyBuildingResult = true;
        buildParameters.FileNameStyle = EFileNameStyle.BundleName_HashName;
        buildParameters.BuildinFileCopyOption = EBuildinFileCopyOption.ClearAndCopyByTags;
        buildParameters.BuildinFileCopyParams = "Builtin";
#if WEIXINMINIGAME
        buildParameters.EncryptionServices = null;
#else
        buildParameters.EncryptionServices = new EncryptionServices();
#endif
        buildParameters.CompressOption = ECompressOption.LZ4;

        ScriptableBuildPipeline pipeline = new ScriptableBuildPipeline();
        var buildResult = pipeline.Run(buildParameters, true);
        if (buildResult.Success) Debug.Log("build success");
        else Debug.LogError($"build fail:{buildResult.ErrorInfo}");
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
            if (result.IsSuccessful())
            {
                Debug.Log("upload file success:" + path);
            }
            else
            {
                Debug.LogError("upload bundle fail：" + path);
                Debug.LogError(result.GetResultInfo());
            }
        }

        string buildPlayerPath = CustomerPreference.GetConfig<string>(CustomerPreferenceEnum.BuildPlayerPath);
        UploadFile2CDN($"{EditorUserBuildSettings.activeBuildTarget}/VersionConfig.txt", $"{buildPlayerPath}/VersionConfig.txt");
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
        if (result.IsSuccessful())
        {
            Debug.Log("upload file success:" + path);
        }
        else
        {
            Debug.LogError("upload file fail:" + path);
            Debug.LogError(result.GetResultInfo());
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
#endif 
    }
    private static void AndroidBuild()
    {
        string time = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        string buildPlayerPath = CustomerPreference.GetConfig<string>(CustomerPreferenceEnum.BuildPlayerPath);
        buildPlayerPath = $"{buildPlayerPath}\\{EditorUserBuildSettings.activeBuildTarget}\\{appversion}\\{time}\\{GameSetting.AppName}";
        Directory.CreateDirectory(Path.GetDirectoryName(buildPlayerPath));

        HideSubScripts(true);
        try
        {
            BuildPipeline.BuildPlayer(EditorBuildSettings.scenes, buildPlayerPath, EditorUserBuildSettings.activeBuildTarget, options);
        }
        catch (Exception e)
        {
            HideSubScripts(false);
            GameDebug.LogError(e.Message);
            throw e;
        }
        HideSubScripts(false);
        if (latestAppVersion) UploadFile2CDN($"{EditorUserBuildSettings.activeBuildTarget}/{GameSetting.AppName}", buildPlayerPath);
    }
    private static void IOSBuild()
    {
        string time = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        string buildPlayerPath = CustomerPreference.GetConfig<string>(CustomerPreferenceEnum.BuildPlayerPath);
        buildPlayerPath = $"{buildPlayerPath}\\{EditorUserBuildSettings.activeBuildTarget}\\{appversion}\\{time}\\{GameSetting.AppName}";
        Directory.CreateDirectory(Path.GetDirectoryName(buildPlayerPath));

        HideSubScripts(true);
        try
        {
            BuildPipeline.BuildPlayer(EditorBuildSettings.scenes, buildPlayerPath, EditorUserBuildSettings.activeBuildTarget, options);
        }
        catch (Exception e)
        {
            HideSubScripts(false);
            GameDebug.LogError(e.Message);
            throw e;
        }
        HideSubScripts(false);
    }
    [MenuItem("Tools/WeChatBuild", false, (int)ToolsMenuSort.WeChatBuild)]
    public static void WeChatBuild()
    {
        HideSubScripts(true);
        WXConvertCore.WXExportError result;
        try
        {
            result = WXConvertCore.DoExport();
        }
        catch (Exception e)
        {
            HideSubScripts(false);
            GameDebug.LogError(e.Message);
            throw e;
        }
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
}
