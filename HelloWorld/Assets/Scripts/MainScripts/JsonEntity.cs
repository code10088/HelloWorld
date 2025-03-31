using System.Collections.Generic;

public class VersionConfig
{
    public List<string> AppVersions { get; set; }
    public List<string> ResVersions { get; set; }
}
public class HotUpdateConfig
{
    public string[] HotAssembly { get; set; }
    public string[] HotUpdateRes { get; set; }
}