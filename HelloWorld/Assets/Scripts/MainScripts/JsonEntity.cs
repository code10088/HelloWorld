using System;

[Serializable]
public class VersionConfig
{
    public string[] AppVersions { get; set; }
    public string[] ResVersions { get; set; }
}
[Serializable]
public class HotUpdateConfig
{
    public string[] HotAssembly { get; set; }
    public string[] HotUpdateRes { get; set; }
}