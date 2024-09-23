using System.IO;
using UnityEngine;
using YooAsset;

public class DecryptionServices : IDecryptionServices
{
    public AssetBundle LoadAssetBundle(DecryptFileInfo fileInfo, out Stream managedStream)
    {
        managedStream = null;
        return AssetBundle.LoadFromFile(fileInfo.FileLoadPath, fileInfo.FileLoadCRC, 8);
    }

    public AssetBundleCreateRequest LoadAssetBundleAsync(DecryptFileInfo fileInfo, out Stream managedStream)
    {
        managedStream = null;
        return AssetBundle.LoadFromFileAsync(fileInfo.FileLoadPath, fileInfo.FileLoadCRC, 8);
    }

    public byte[] ReadFileData(DecryptFileInfo fileInfo)
    {
        throw new System.NotImplementedException();
    }

    public string ReadFileText(DecryptFileInfo fileInfo)
    {
        throw new System.NotImplementedException();
    }
}