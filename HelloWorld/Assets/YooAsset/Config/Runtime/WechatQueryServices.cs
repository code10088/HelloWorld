using YooAsset;
using WeChatWASM;

public class WechatQueryServices : IWechatQueryServices
{
    private WXFileSystemManager fileSystemManager;
    public bool Query(string packageName, string fileName, string fileCRC)
    {
        if (fileSystemManager == null) fileSystemManager = WX.GetFileSystemManager();
        string filePath = $"{WX.env.USER_DATA_PATH}/{fileName}";
        string result = fileSystemManager.AccessSync(filePath);
        return result.Equals("access:ok");
    }
}
