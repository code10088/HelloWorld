using System.IO;
using YooAsset;

public class BundleDecryptor : IBundleMemoryDecryptor
{
    byte[] IBundleMemoryDecryptor.GetDecryptedData(BundleDecryptArgs args)
    {
        byte[] data = args.FileData;
        if (data == null) data = File.ReadAllBytes(args.FilePath);
        for (int i = 0; i < data.Length; i++) data[i] ^= 64;
        return data;
    }
}