using System;
using System.IO;
using System.Text;
using System.Collections;
using UnityEngine;
using NUnit.Framework;
using YooAsset;

/// <summary>
/// 测试加载加密文件
/// </summary>
public class TestBundleEncryption
{
    public IEnumerator RuntimeTester()
    {
        ResourcePackage package = YooAssets.GetPackage(TestDefine.AssetBundlePackageName);
        Assert.IsNotNull(package);

        // 异步加载音乐播放预制体
        {
            var assetHandle = package.LoadAssetAsync<GameObject>("prefab_audioA");
            yield return assetHandle;
            Assert.AreEqual(EOperationStatus.Succeed, assetHandle.Status);

            var go = assetHandle.InstantiateSync(Vector3.zero, Quaternion.identity);
            Assert.IsNotNull(go);

            var audioSource = go.GetComponent<AudioSource>();
            Assert.IsNotNull(audioSource.clip);
        }

        // 同步加载音乐播放预制体
        {
            var assetHandle = package.LoadAssetSync<GameObject>("prefab_audioB");
            yield return assetHandle;
            Assert.AreEqual(EOperationStatus.Succeed, assetHandle.Status);

            var go = assetHandle.InstantiateSync(Vector3.zero, Quaternion.identity);
            Assert.IsNotNull(go);

            var audioSource = go.GetComponent<AudioSource>();
            Assert.IsNotNull(audioSource.clip);
        }

        yield return new WaitForSeconds(1f);
    }
}

/* 资源代码流程
 * 内置文件解压（加载器）
BuildinFileSystem::LoadBundleFile()
{
	_unpackFileSystem.LoadBundleFile(bundle);
}
UnpackFileSystem::LoadAssetBundleOperation()
{
	DownloadFileOptions options = new DownloadFileOptions(int.MaxValue, 60);
    _unpackFileSystem.DownloadFileAsync(_bundle, options);
}
UnpackFileSystem::DownloadFileAsync()
{
   //RemoteServices返回内置文件路径
   string mainURL = RemoteServices.GetRemoteMainURL(bundle.FileName);
   string fallbackURL = RemoteServices.GetRemoteFallbackURL(bundle.FileName);
   options.SetURL(mainURL, fallbackURL);
   return new DownloadPackageBundleOperation(bundle, options);
}
*/