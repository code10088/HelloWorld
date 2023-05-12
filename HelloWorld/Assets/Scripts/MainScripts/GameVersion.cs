using Newtonsoft.Json;
using System;
using System.Collections.Generic;

public class GameVersion : Singletion<GameVersion>
{
    private AppVersionData version;
    private Action initFinish;

    public AppVersionData Version => version;
    public void Init(Action finish)
    {
        initFinish = finish;
        FileUtils.Instance.Read(FileUtils.PlatformPath + "Version.txt", Init);
    }
    private void Init(byte[] bytes)
    {
        string str = System.Text.Encoding.Default.GetString(bytes);
        version = JsonConvert.DeserializeObject<AppVersionData>(str);
        initFinish?.Invoke();
    }
    public void SetNewVersion(AppVersionData _version, Action finish)
    {
        version = _version;
        string str = JsonConvert.SerializeObject(version);
        byte[] bytes = System.Text.Encoding.Default.GetBytes(str);
        FileUtils.Instance.Write("Version.txt", bytes, a => finish?.Invoke());
    }
}
public class AppVersionData
{
    public string app_version { get; set; }
    public string app_url { get; set; }
    public string app_hotupdate { get; set; }
    public List<string> app_hotupdateres { get; set; }
}