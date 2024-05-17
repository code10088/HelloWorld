using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using YooAsset;

public class DecryptionServices : IDecryptionServices
{
    public AssetBundle LoadAssetBundle(DecryptFileInfo fileInfo, out Stream managedStream)
    {
        managedStream = null;
        return AssetBundle.LoadFromFile(fileInfo.FileLoadPath, fileInfo.ConentCRC, 8);
    }

    public AssetBundleCreateRequest LoadAssetBundleAsync(DecryptFileInfo fileInfo, out Stream managedStream)
    {
        managedStream = null;
        return AssetBundle.LoadFromFileAsync(fileInfo.FileLoadPath, fileInfo.ConentCRC, 8);
    }
}
