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

public class TestLoadScriptableObject
{
    [UnityTest]
    public IEnumerator RuntimeTester()
    {
        ResourcePackage package = YooAssets.GetPackage("TestPackage");
        Assert.IsNotNull(package);

        var assetHandle = package.LoadAssetAsync("game_config");
        yield return assetHandle;
        Assert.AreEqual(EOperationStatus.Succeed, assetHandle.Status);

        var testScriptableObject = assetHandle.AssetObject as TestScriptableObject;
        Assert.IsNotNull(testScriptableObject);
        TestLogger.Log(this, testScriptableObject.ConfigName);
    }
}