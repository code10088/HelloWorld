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

public class TestLoadImage
{
    [UnityTest]
    public IEnumerator RuntimeTester()
    {
        ResourcePackage package = YooAssets.GetPackage("TestPackage");
        Assert.IsNotNull(package);

        var subAssetsHandle = package.LoadSubAssetsAsync<Sprite>("image_a");
        yield return subAssetsHandle;
        Assert.AreEqual(EOperationStatus.Succeed, subAssetsHandle.Status);

        var subAssetObjects = subAssetsHandle.SubAssetObjects;
        Assert.IsNotNull(subAssetObjects);

        int count = subAssetObjects.Count;
        Assert.AreEqual(count, 3);
    }
}