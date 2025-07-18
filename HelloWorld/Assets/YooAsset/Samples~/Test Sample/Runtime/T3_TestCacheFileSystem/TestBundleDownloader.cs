using System;
using System.IO;
using System.Text;
using System.Collections;
using UnityEngine;
using NUnit.Framework;
using YooAsset;

/// <summary>
/// 测试远端文件下载
/// </summary>
public class TestBundleDownloader
{
    public IEnumerator RuntimeTester()
    {
        ResourcePackage package = YooAssets.GetPackage(TestDefine.AssetBundlePackageName);
        Assert.IsNotNull(package);

        var downloader = package.CreateResourceDownloader(10, 1);
        if (downloader.TotalDownloadCount == 0)
        {
            Assert.Fail($"Test downloader not found any file !");
        }

        downloader.BeginDownload();
        downloader.DownloadFinishCallback = (DownloaderFinishData data) =>
        {
            if (data.Succeed == false)
                Assert.Fail($"Test downloader failed ! {downloader.Error}");
        };

        // 等待一秒
        yield return new WaitForSeconds(1f);
    }
}

/* 资源代码流程
 * 远端文件下载（下载器）
CacheFileSystem::DownloadFileAsync()
{
	//RemoteServices返回CDN文件路径
	string mainURL = RemoteServices.GetRemoteMainURL(bundle.FileName);
    string fallbackURL = RemoteServices.GetRemoteFallbackURL(bundle.FileName);
    options.SetURL(mainURL, fallbackURL);
    return new DownloadPackageBundleOperation(bundle, options);
}
*/