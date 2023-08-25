using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Text;

public class Downloader : Singletion<Downloader>, SingletionInterface
{
    private List<HttpItem> all = new List<HttpItem>();
    private List<HttpItemGroup> group = new List<HttpItemGroup>();
    private int count = 0;
    private int updateId = -1;

    public void Init()
    {
        
    }
    public void Download(string url, string path, Action<string, byte[]> finish, Action<string, float, float> update = null, int timeout = 5)
    {
        all.Add(new HttpItem(url, path, finish, update, timeout));
        if (updateId < 0) updateId = Updater.Instance.StartUpdate(UpdateProgress);
        CheckHttpQueue();
    }
    public void Download(string[] url, string[] path, Action<string[]> finish, Action<int, int> update = null)
    {
        group.Add(new HttpItemGroup(url, path, finish, update));
        if (updateId < 0) updateId = Updater.Instance.StartUpdate(UpdateProgress);
    }
    public void CheckHttpQueue()
    {
        if (count < GameSetting.httpLimit && all.Count > 0)
        {
            HttpItem item = all[0];
            all.RemoveAt(0);
            item.Start();
            count++;
        }
    }
    public void UpdateProgress()
    {
        for (int i = 0; i < all.Count; i++)
        {
            all[i].Update();
        }
        for (int i = 0; i < group.Count; i++)
        {
            group[i].Update();
        }
        if (all.Count == 0 && group.Count == 0)
        {
            Updater.Instance.StopUpdate(updateId);
        }
    }

    public class HttpItemGroup
    {
        private int total;
        private Action<string[]> finish;
        private Action<int, int> update;
        private int success;
        private List<string> fail;
        public HttpItemGroup(string[] _url, string[] _path, Action<string[]> _finish, Action<int, int> _update)
        {
            total = _url.Length;
            finish = _finish;
            update = _update;
            for (int i = 0; i < total; i++) Instance.Download(_url[i], _path[i], Finish);
        }
        private void Finish(string url, byte[] bytes)
        {
            if (bytes != null && bytes.Length > 0)
            {
                success++;
            }
            else
            {
                fail.Add(url);
            }
            if (success + fail.Count == total)
            {
                finish(fail.ToArray());
                Instance.group.Remove(this);
            }
        }
        public void Update()
        {
            update?.Invoke(success + fail.Count, total);
        }
        public bool IsFinish()
        {
            return success + fail.Count == total;
        }
    }
    public class HttpItem
    {
        private string url;
        private string path;
        private int timeout;
        private long fileLength;
        private long downloadedLength;
        private Action<string, byte[]> finish;
        private Action<string, float, float> update;
        private byte[] result;
        private int retry = 0;

        public HttpItem(string _url, string _path, Action<string, byte[]> _finish, Action<string, float, float> _update, int _timeout)
        {
            url = _url;
            path = _path;
            timeout = _timeout;
            finish = _finish;
            update = _update;
        }
        public void Start()
        {
            ThreadManager.Instance.StartThread(Request, Finish, priority:0);
        }
        private void Request(object o)
        {
            var hwr = WebRequest.Create(url) as HttpWebRequest;
            hwr.Timeout = timeout;
            hwr.Method = WebRequestMethods.File.DownloadFile;
            long saveLength = !string.IsNullOrEmpty(path) && File.Exists(path) ? new FileInfo(path).Length : 0;
            hwr.AddRange(saveLength);
            var hwb = hwr.GetResponse() as HttpWebResponse;
            if (hwb.StatusCode == HttpStatusCode.OK)
            {
                fileLength = hwb.ContentLength;
                Stream stream = hwb.GetResponseStream();
                result = new byte[fileLength];

                int l = 0;
                while (l < fileLength)
                {
                    int _l = stream.Read(result, l, result.Length);
                    if (_l <= 0) break;
                    l += _l;
                    downloadedLength = Math.Max(l, downloadedLength);//·ÀÖ¹½ø¶ÈÌõµ¹ÍË
                }
                hwb.Dispose();
                hwr.Abort();

                if (!string.IsNullOrEmpty(path))
                {
                    FileStream fs = new FileStream(path, FileMode.OpenOrCreate, FileAccess.ReadWrite);
                    fs.Write(result, (int)saveLength, result.Length);
                    fs.Flush();
                    result = new byte[fs.Length];
                    fs.Read(result, 0, result.Length);
                    MD5 md5 = new MD5CryptoServiceProvider();
                    byte[] hash = md5.ComputeHash(fs);
                    fs.Dispose();
                    string name = Path.GetFileName(path);
                    StringBuilder sb = new StringBuilder();
                    for (int i = 0; i < hash.Length; i++) sb.Append(hash[i].ToString("x2"));
                    if (!name.Equals(sb.ToString()))
                    {
                        File.Delete(path);
                        Request(null);
                    }
                }
            }
            else
            {
                hwb.Dispose();
                hwr.Abort();
                result = null;
                if (++retry > 3) return;
                Request(null);
            }
        }
        private void Finish()
        {
            finish?.Invoke(url, result);
            Instance.CheckHttpQueue();
        }
        public void Update()
        {
            update?.Invoke(url, downloadedLength, fileLength);
        }
    }
}
