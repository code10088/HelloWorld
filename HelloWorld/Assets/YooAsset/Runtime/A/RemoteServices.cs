using YooAsset;

public class RemoteServices : IRemoteServices
{
    private string defaultHostServer;
    private string fallbackHostServer;

    public RemoteServices(string _defaultHostServer, string _fallbackHostServer)
    {
        defaultHostServer = _defaultHostServer;
        fallbackHostServer = _fallbackHostServer;
    }
    string IRemoteServices.GetRemoteMainURL(string fileName)
    {
        return defaultHostServer + fileName;
    }
    string IRemoteServices.GetRemoteFallbackURL(string fileName)
    {
        return fallbackHostServer + fileName;
    }
}
