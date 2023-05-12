using System;
using System.Reflection;
using Newtonsoft.Json;

namespace MainAssembly
{
    public class HotUpdateCode : Singletion<HotUpdateCode>
    {
        private Assembly hotAssembly;
        private AppVersionData latestVersion;

        public void Start()
        {
            CheckUpdateInfo();
        }
        private void CheckUpdateInfo()
        {
#if UNITY_EDITOR
            UpdateFinish_LoadHotAssembly();
#else
            Downloader.Instance.Download("Version.txt", string.Empty, CompareVersion, UpdateProgress);
            UIHotUpdateCode.Instance.SetText("Update Game Version");
#endif
        }
        private void UpdateProgress(string url, float downloaded, float total)
        {
            UIHotUpdateCode.Instance.SetText($"Download {downloaded / 1024}k/{total / 1024}k");
            UIHotUpdateCode.Instance.SetSlider(downloaded / total);
        }
        private void CompareVersion(string url, byte[] bytes)
        {
            if (bytes == null || bytes.Length == 0)
            {
                UIHotUpdateCode.Instance.OpenMessageBox("Tips", "Retry", CheckUpdateInfo);
            }
            else
            {
                string str = System.Text.Encoding.Default.GetString(bytes);
                latestVersion = JsonConvert.DeserializeObject<AppVersionData>(str);
                if (!FileUtils.Instance.CheckDownloaded(latestVersion.app_hotupdate)) DownloadRes();
                else if (!FileUtils.Instance.CheckDownloaded(latestVersion.app_hotupdateres.ToArray())) DownloadRes();
                else if (!latestVersion.app_version.Equals(GameVersion.Instance.Version.app_version)) DownloadRes();
                else UpdateFinish_LoadHotAssembly();
            }
        }
        private void DownloadRes()
        {
            string[] url = new string[latestVersion.app_hotupdateres.Count];
            for (int i = 0; i < url.Length; i++) url[i] = latestVersion.app_url + latestVersion.app_hotupdateres[i];
            Downloader.Instance.Download(url, latestVersion.app_hotupdateres.ToArray(), DownloadResFinish_DownloadDll, UpdateProgress);            
            UIHotUpdateCode.Instance.SetText("Download Resources");
        }
        private void UpdateProgress(int downloaded, int total)
        {
            UIHotUpdateCode.Instance.SetSlider(downloaded / (float)total);
        }
        private void DownloadResFinish_DownloadDll(string[] fail)
        {
            if (fail.Length == 0)
            {
                string url = latestVersion.app_url + latestVersion.app_hotupdate;
                Downloader.Instance.Download(url, latestVersion.app_hotupdate, DownloadDllFinish_SaveVersion, UpdateProgress);
                UIHotUpdateCode.Instance.SetText("Download Resources");
            }
            else
            {
                UIHotUpdateCode.Instance.OpenMessageBox("Tips", "Retry", DownloadRes);
            }
        }
        private void DownloadDllFinish_SaveVersion(string url, byte[] bytes)
        {
            if (bytes == null || bytes.Length == 0)
            {
                UIHotUpdateCode.Instance.OpenMessageBox("Tips", "Retry", DownloadRes);
            }
            else
            {
                GameVersion.Instance.SetNewVersion(latestVersion, UpdateFinish_LoadHotAssembly);
            }
        }
        private void UpdateFinish_LoadHotAssembly()
        {
            UIHotUpdateCode.Instance.Finish();
#if UNITY_EDITOR
            HotAssembly.GameStart.Instance.Init();
#else
            FileUtils.Instance.Read(GameVersion.Instance.Version.app_hotupdate, StartHotAssembly);
#endif
        }
        private void StartHotAssembly(byte[] bytes)
        {
            hotAssembly = Assembly.Load(bytes);
            Type t = hotAssembly.GetType("HotAssembly.GameStart");
            PropertyInfo p = t.BaseType.GetProperty("Instance");
            object o = p.GetMethod.Invoke(null, null);
            MethodInfo m = t.GetMethod("Init");
            m.Invoke(o, null);
        }
    }
}