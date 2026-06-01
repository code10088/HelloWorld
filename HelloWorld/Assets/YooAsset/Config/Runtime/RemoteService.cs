using System.Collections.Generic;
using YooAsset;

public class RemoteService : IRemoteService
{
    private string defaultHostServer;
    private string fallbackHostServer;

    public RemoteService(string _defaultHostServer, string _fallbackHostServer)
    {
        defaultHostServer = _defaultHostServer;
        fallbackHostServer = _fallbackHostServer;
    }
    public IReadOnlyList<string> GetRemoteUrls(string fileName)
    {
        return new List<string>()
        {
            $"{defaultHostServer}/{fileName}",
            $"{fallbackHostServer}/{fileName}"
        };
    }
}
