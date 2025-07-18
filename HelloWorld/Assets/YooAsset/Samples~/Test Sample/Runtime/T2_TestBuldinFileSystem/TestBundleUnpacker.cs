using System;
using System.IO;
using System.Text;
using System.Collections;
using UnityEngine;
using NUnit.Framework;
using YooAsset;

/// <summary>
/// 测试内置文件解压
/// </summary>
public class TestBundleUnpacker
{
    public IEnumerator RuntimeTester()
    {
        ResourcePackage package = YooAssets.GetPackage(TestDefine.AssetBundlePackageName);
        Assert.IsNotNull(package);

        var resourceUnpacker = package.CreateResourceUnpacker("unpack", 10, 1);
        Assert.AreEqual(resourceUnpacker.TotalDownloadCount, 2);

        yield return resourceUnpacker;
        Assert.AreEqual(EOperationStatus.Succeed, resourceUnpacker.Status);

        // 等待一秒
        yield return new WaitForSeconds(1f);
    }
}

/* 资源代码流程
 * 内置文件解压（解压器）
BundleInfo::CreateDownloader()
{
    return _buildFileSystem.DownloadFileAsync(Bundle, options);	
}
BuildinFileSystem::DownloadFileAsync()
{
	options.ImportFilePath = xxxxxx;
	_unpackFileSystem.DownloadFileAsync(bundle, options);
}
UnpackFileSystem::DownloadFileAsync()
{
	string mainURL = ConvertToWWWPath(options.ImportFilePath);
 	options.SetURL(mainURL, mainURL);
 	return new DownloadPackageBundleOperation(bundle, options);
}
*/