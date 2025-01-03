using System;
using System.IO;
using System.Text;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.U2D;
using UnityEngine.TestTools;
using NUnit.Framework;
using YooAsset;

public class BuildinFileSystemTester : IPrebuildSetup, IPostBuildCleanup
{
    private const string TEST_PACKAGE_ROOT_KEY = "TEST_PACKAGE_ROOT_KEY";
    private const string RAW_PACKAGE_ROOT_KEY = "RAW_PACKAGE_ROOT_KEY";

    void IPrebuildSetup.Setup()
    {
#if UNITY_EDITOR
        AssetBundleCollectorMaker.MakeCollectorSettingData();

        // 构建TestPackage
        {
            var simulateParams = new EditorSimulateBuildParam("TestPackage");
            simulateParams.BuildPipelineName = "ScriptableBuildPipeline";
            simulateParams.InvokeAssmeblyName = "YooAsset.Test.Editor";
            simulateParams.InvokeClassFullName = "TestSimulateBuilder";
            simulateParams.InvokeMethodName = "SimulateBuild";
            var simulateResult = EditorSimulateModeHelper.SimulateBuild(simulateParams);
            UnityEditor.EditorPrefs.SetString(TEST_PACKAGE_ROOT_KEY, simulateResult.PackageRootDirectory);
        }

        // 构建RawPackage
        {
            var simulateParams = new EditorSimulateBuildParam("RawPackage");
            simulateParams.BuildPipelineName = "RawFileBuildPipeline";
            simulateParams.InvokeAssmeblyName = "YooAsset.Test.Editor";
            simulateParams.InvokeClassFullName = "TestSimulateBuilder";
            simulateParams.InvokeMethodName = "SimulateBuild";
            var simulateResult = EditorSimulateModeHelper.SimulateBuild(simulateParams);
            UnityEditor.EditorPrefs.SetString(RAW_PACKAGE_ROOT_KEY, simulateResult.PackageRootDirectory);
        }
#endif
    }
    void IPostBuildCleanup.Cleanup()
    {
    }

    [UnitySetUp]
    public virtual IEnumerator RuntimeSetup()
    {
        // 初始化YooAsset
        YooAssets.Initialize();

        // 初始化TestPackage
        {
            string packageRoot = string.Empty;
#if UNITY_EDITOR
            packageRoot = UnityEditor.EditorPrefs.GetString(TEST_PACKAGE_ROOT_KEY);
#endif
            if (Directory.Exists(packageRoot) == false)
                throw new Exception($"Not found package root : {packageRoot}");

            var package = YooAssets.CreatePackage("TestPackage");

            // 初始化资源包
            var initParams = new OfflinePlayModeParameters();
            initParams.BuildinFileSystemParameters = FileSystemParameters.CreateDefaultBuildinFileSystemParameters(null, packageRoot);
            var initializeOp = package.InitializeAsync(initParams);
            yield return initializeOp;
            if (initializeOp.Status != EOperationStatus.Succeed)
                Debug.LogError(initializeOp.Error);
            Assert.AreEqual(EOperationStatus.Succeed, initializeOp.Status);

            // 请求资源版本
            var requetVersionOp = package.RequestPackageVersionAsync();
            yield return requetVersionOp;
            if (requetVersionOp.Status != EOperationStatus.Succeed)
                Debug.LogError(requetVersionOp.Error);
            Assert.AreEqual(EOperationStatus.Succeed, requetVersionOp.Status);

            // 更新资源清单
            var updateManifestOp = package.UpdatePackageManifestAsync(requetVersionOp.PackageVersion);
            yield return updateManifestOp;
            if (updateManifestOp.Status != EOperationStatus.Succeed)
                Debug.LogError(updateManifestOp.Error);
            Assert.AreEqual(EOperationStatus.Succeed, updateManifestOp.Status);
        }

        // 初始化RawPackage
        {
            string packageRoot = string.Empty;
#if UNITY_EDITOR
            packageRoot = UnityEditor.EditorPrefs.GetString(RAW_PACKAGE_ROOT_KEY);
#endif
            if (Directory.Exists(packageRoot) == false)
                throw new Exception($"Not found package root : {packageRoot}");

            var package = YooAssets.CreatePackage("RawPackage");

            // 初始化资源包
            var initParams = new OfflinePlayModeParameters();
            initParams.BuildinFileSystemParameters = FileSystemParameters.CreateDefaultBuildinFileSystemParameters(null, packageRoot);
            initParams.BuildinFileSystemParameters.AddParameter(FileSystemParametersDefine.APPEND_FILE_EXTENSION, true);
            var initializeOp = package.InitializeAsync(initParams);
            yield return initializeOp;
            if (initializeOp.Status != EOperationStatus.Succeed)
                Debug.LogError(initializeOp.Error);
            Assert.AreEqual(EOperationStatus.Succeed, initializeOp.Status);

            // 请求资源版本
            var requetVersionOp = package.RequestPackageVersionAsync();
            yield return requetVersionOp;
            if (requetVersionOp.Status != EOperationStatus.Succeed)
                Debug.LogError(requetVersionOp.Error);
            Assert.AreEqual(EOperationStatus.Succeed, requetVersionOp.Status);

            // 更新资源清单
            var updateManifestOp = package.UpdatePackageManifestAsync(requetVersionOp.PackageVersion);
            yield return updateManifestOp;
            if (updateManifestOp.Status != EOperationStatus.Succeed)
                Debug.LogError(updateManifestOp.Error);
            Assert.AreEqual(EOperationStatus.Succeed, updateManifestOp.Status);
        }
    }

    [UnityTest]
    public IEnumerator RuntimeInit()
    {
        // 声明该方法可以被自动化流程识别！
        yield break;
    }
}