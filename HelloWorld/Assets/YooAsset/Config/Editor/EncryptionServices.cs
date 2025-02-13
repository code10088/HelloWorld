using System.IO;
using YooAsset;

public class EncryptionServices : IEncryptionServices
{
    public EncryptResult Encrypt(EncryptFileInfo fileInfo)
    {
        var fileData = File.ReadAllBytes(fileInfo.FileLoadPath);
        for (int i = 0; i < fileData.Length; i++) fileData[i] ^= 64;
        EncryptResult result = new EncryptResult();
        result.Encrypted = true;
        result.EncryptedData = fileData;
        return result;
    }
}