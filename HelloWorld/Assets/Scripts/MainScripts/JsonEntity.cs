using System.Collections.Generic;

public class BuiltinFileList
{
    public List<BuiltinFileItem> BuiltinFiles { get; set; }
}
public class BuiltinFileItem
{
    public string PackageName { get; set; }
    public string FileName { get; set; }
    public string FileCRC32 { get; set; }
}
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