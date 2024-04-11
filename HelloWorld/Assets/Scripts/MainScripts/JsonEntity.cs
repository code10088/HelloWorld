using System.Collections.Generic;

public class VersionConfig
{
    public List<string> AppVersions { get; set; }
    public List<string> ResVersions { get; set; }
}
public class HotUpdateConfig
{
    public List<string> Metadata { get; set; }
    public List<string> HotUpdateRes { get; set; }
}