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

public class TestLoadAudio
{
    [UnityTest]
    public IEnumerator RuntimeTester()
    {
        ResourcePackage package = YooAssets.GetPackage("TestPackage");
        Assert.IsNotNull(package);

        // 音乐
        {
            var assetHandle = package.LoadAssetAsync<AudioClip>("music_a");
            yield return assetHandle;
            Assert.AreEqual(EOperationStatus.Succeed, assetHandle.Status);

            var audioClip = assetHandle.AssetObject as AudioClip;
            Assert.IsNotNull(audioClip);
        }

        // 音效
        {
            var assetHandle = package.LoadAssetAsync<AudioClip>("sound_a");
            yield return assetHandle;
            Assert.AreEqual(EOperationStatus.Succeed, assetHandle.Status);

            var audioClip = assetHandle.AssetObject as AudioClip;
            Assert.IsNotNull(audioClip);
        }
    }
}