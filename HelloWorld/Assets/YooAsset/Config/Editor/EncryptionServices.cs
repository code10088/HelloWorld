using System;
using System.IO;
using YooAsset;

public class EncryptionServices : IEncryptionServices
{
    public EncryptResult Encrypt(EncryptFileInfo fileInfo)
    {
        int offset = 8;
        var fileData = File.ReadAllBytes(fileInfo.FilePath);
        var encryptedData = new byte[fileData.Length + offset];
        Buffer.BlockCopy(fileData, 0, encryptedData, offset, fileData.Length);

        EncryptResult result = new EncryptResult();
        result.Encrypted = true;
        result.EncryptedData = encryptedData;
        return result;
    }
}