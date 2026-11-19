using System.IO;
using YooAsset;

public class BundleEncryptor : IBundleEncryptor
{
    public BundleEncryptResult Encrypt(BundleEncryptArgs args)
    {
        var fileData = File.ReadAllBytes(args.FilePath);
        for (int i = 0; i < fileData.Length; i++) fileData[i] ^= 64;
        return new BundleEncryptResult(true, fileData);
    }
}