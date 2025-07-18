using System;
using System.IO;
using System.Text;
using System.Collections;
using UnityEngine;
using NUnit.Framework;
using YooAsset;

/// <summary>
/// 测试边玩边下
/// </summary>
public class TestBundlePlaying
{
    public IEnumerator RuntimeTester()
    {
        ResourcePackage package = YooAssets.GetPackage(TestDefine.AssetBundlePackageName);
        Assert.IsNotNull(package);

        if (package.IsNeedDownloadFromRemote("panel_a") == false)
        {
            Assert.Fail("Load bundle is already existed !");
        }
        if (package.IsNeedDownloadFromRemote("panel_b") == false)
        {
            Assert.Fail("Load bundle is already existed !");
        }
        
        // 测试异步加载
        {
            var assetsHandle = package.LoadAssetAsync<GameObject>("panel_a");
            yield return assetsHandle;
            Assert.AreEqual(EOperationStatus.Succeed, assetsHandle.Status);
        }

        // 测试同步加载
        {
            // 验证失败结果
            UnityEngine.TestTools.LogAssert.ignoreFailingMessages = true;
            var assetsHandle = package.LoadAssetSync<GameObject>("panel_b");
            yield return assetsHandle;
            Assert.AreEqual(EOperationStatus.Failed, assetsHandle.Status);
            UnityEngine.TestTools.LogAssert.ignoreFailingMessages = false;

            // 清理加载器
            assetsHandle.Release();
            package.UnloadUnusedAssetsAsync();

            // 验证成功结果
            yield return new WaitForSeconds(1f);
            assetsHandle = package.LoadAssetSync<GameObject>("panel_b");
            yield return assetsHandle;
            Assert.AreEqual(EOperationStatus.Succeed, assetsHandle.Status);
        }

        // 等待一秒
        yield return new WaitForSeconds(1f);
    }
}

/* 资源代码流程
 * 远端文件下载（加载器）
CacheFileSystem::LoadBundleFile()
{
	_cacheFileSystem.LoadBundleFile(bundle);
}
CacheFileSystem::LoadAssetBundleOperation()
{
	DownloadFileOptions options = new DownloadFileOptions(int.MaxValue, 60);
    _cacheFileSystem.DownloadFileAsync(_bundle, options);
}
CacheFileSystem::DownloadFileAsync()
{
   //RemoteServices返回CDN文件路径
   string mainURL = RemoteServices.GetRemoteMainURL(bundle.FileName);
   string fallbackURL = RemoteServices.GetRemoteFallbackURL(bundle.FileName);
   options.SetURL(mainURL, fallbackURL);
   return new DownloadPackageBundleOperation(bundle, options);
}
*/