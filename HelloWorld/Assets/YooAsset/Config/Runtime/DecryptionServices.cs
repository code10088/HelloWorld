using UnityEngine;
using YooAsset;

public class DecryptionServices : IDecryptionServices
{
    public DecryptResult LoadAssetBundle(DecryptFileInfo fileInfo)
    {
        DecryptResult decryptResult = new DecryptResult();
        decryptResult.ManagedStream = null;
        decryptResult.Result = AssetBundle.LoadFromFile(fileInfo.FileLoadPath, fileInfo.FileLoadCRC, 8);
        return decryptResult;
    }

    public DecryptResult LoadAssetBundleAsync(DecryptFileInfo fileInfo)
    {
        DecryptResult decryptResult = new DecryptResult();
        decryptResult.ManagedStream = null;
        decryptResult.CreateRequest = AssetBundle.LoadFromFileAsync(fileInfo.FileLoadPath, fileInfo.FileLoadCRC, 8);
        return decryptResult;
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