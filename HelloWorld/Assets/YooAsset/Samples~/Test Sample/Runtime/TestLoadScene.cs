using System;
using System.IO;
using System.Text;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.U2D;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using NUnit.Framework;
using YooAsset;

public class TestLoadScene
{
    [UnityTest]
    public IEnumerator RuntimeTester()
    {
        ResourcePackage package = YooAssets.GetPackage("TestPackage");
        Assert.IsNotNull(package);

        // 主场景
        {
            var sceneHandle = package.LoadSceneAsync("scene_a", LoadSceneMode.Single);
            yield return sceneHandle;
            Assert.AreEqual(EOperationStatus.Succeed, sceneHandle.Status);

            var scene = sceneHandle.SceneObject;
            Assert.IsNotNull(scene);
        }

        // 附加场景
        yield return new WaitForSeconds(1f);
        {
            var sceneHandle = package.LoadSceneAsync("scene_b", LoadSceneMode.Additive);
            yield return sceneHandle;
            Assert.AreEqual(EOperationStatus.Succeed, sceneHandle.Status);

            var scene = sceneHandle.SceneObject;
            Assert.IsNotNull(scene);
        }

        // 主场景
        yield return new WaitForSeconds(1f);
        {
            var sceneHandle = package.LoadSceneAsync("scene_c", LoadSceneMode.Single);
            yield return sceneHandle;
            Assert.AreEqual(EOperationStatus.Succeed, sceneHandle.Status);

            var scene = sceneHandle.SceneObject;
            Assert.IsNotNull(scene);
        }
    }
}