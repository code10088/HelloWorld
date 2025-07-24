using System;
using System.IO;
using UnityEngine;
using YooAsset;

public class BundleStream : FileStream
{
    public BundleStream(string path, FileMode mode, FileAccess access, FileShare share) : base(path, mode, access, share)
    {

    }
    public override int Read(byte[] array, int offset, int count)
    {
        var index = base.Read(array, offset, count);
        for (int i = 0; i < array.Length; i++) array[i] ^= 64;
        return index;
    }
}
public class DecryptionServices : IDecryptionServices
{
    DecryptResult IDecryptionServices.LoadAssetBundle(DecryptFileInfo fileInfo)
    {
        BundleStream bundleStream = new BundleStream(fileInfo.FileLoadPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        DecryptResult decryptResult = new DecryptResult();
        decryptResult.ManagedStream = bundleStream;
        decryptResult.Result = AssetBundle.LoadFromStream(bundleStream, fileInfo.FileLoadCRC, 1024);
        return decryptResult;
    }
    DecryptResult IDecryptionServices.LoadAssetBundleAsync(DecryptFileInfo fileInfo)
    {
        BundleStream bundleStream = new BundleStream(fileInfo.FileLoadPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        DecryptResult decryptResult = new DecryptResult();
        decryptResult.ManagedStream = bundleStream;
        decryptResult.CreateRequest = AssetBundle.LoadFromStreamAsync(bundleStream, fileInfo.FileLoadCRC, 1024);
        return decryptResult;
    }
    DecryptResult IDecryptionServices.LoadAssetBundleFallback(DecryptFileInfo fileInfo)
    {
        return new DecryptResult();
    }
    byte[] IDecryptionServices.ReadFileData(DecryptFileInfo fileInfo)
    {
        throw new NotImplementedException();
    }
    string IDecryptionServices.ReadFileText(DecryptFileInfo fileInfo)
    {
        throw new NotImplementedException();
    }
}
public class WebDecryptionServices : IWebDecryptionServices
{
    public WebDecryptResult LoadAssetBundle(WebDecryptFileInfo fileInfo)
    {
        for (int i = 0; i < fileInfo.FileData.Length; i++) fileInfo.FileData[i] ^= 64;
        WebDecryptResult decryptResult = new WebDecryptResult();
        decryptResult.Result = AssetBundle.LoadFromMemory(fileInfo.FileData);
        return decryptResult;
    }
}
public class ManifestProcessServices : IManifestProcessServices
{
    public byte[] ProcessManifest(byte[] fileData)
    {
        for (int i = 0; i < fileData.Length; i++) fileData[i] ^= 64;
        return fileData;
    }
}
public class ManifestRestoreServices : IManifestRestoreServices
{
    public byte[] RestoreManifest(byte[] fileData)
    {
        for (int i = 0; i < fileData.Length; i++) fileData[i] ^= 64;
        return fileData;
    }
}