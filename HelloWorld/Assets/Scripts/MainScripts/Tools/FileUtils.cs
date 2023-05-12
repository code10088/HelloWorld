using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

public class FileUtils : Singletion<FileUtils>
{
    public static string PlatformPath
    {
        get
        {
            return Application.persistentDataPath + "/";
        }
    }

    private List<FileItem> queue = new List<FileItem>();
    private int count = 0;

    public void Read(string path, Action<byte[]> finish)
    {
        FileItem item = new FileItem(FileAccess.Read, path, null, finish);
        queue.Add(item);
        Check();
    }
    public void Write(string path, byte[] bytes, Action<byte[]> finish)
    {
        FileItem item = new FileItem(FileAccess.Write, path, bytes, finish);
        queue.Add(item);
        Check();
    }
    private void Check()
    {
        if (count < GameSetting.writeLimit && queue.Count > 0)
        {
            FileItem item = queue[0];
            queue.RemoveAt(0);
            item.Start();
            count++;
        }
    }
    public bool CheckDownloaded(string[] path)
    {
        for (int i = 0; i < path.Length; i++)
        {
            if (!CheckDownloaded(path[i])) return false;
        }
        return true;
    }
    public bool CheckDownloaded(string path)
    {
        if (path.StartsWith("http")) path = PlatformPath + Path.GetFileName(path);
        else if (!path.StartsWith("file")) path = PlatformPath + path;
        if (!File.Exists(path)) return false;
        FileStream fs = new FileStream(path, FileMode.OpenOrCreate);
        MD5 md5 = new MD5CryptoServiceProvider();
        byte[] hash = md5.ComputeHash(fs);
        fs.Dispose();
        string name = Path.GetFileName(path);
        StringBuilder sb = new StringBuilder();
        for (int i = 0; i < hash.Length; i++) sb.Append(hash[i].ToString("x2"));
        return name.Equals(sb.ToString());
    }

    public class FileItem
    {
        private FileAccess fa;
        private string path;
        private byte[] bytes;
        private Action<byte[]> finish;

        public FileItem(FileAccess _fa, string _path, byte[] _bytes, Action<byte[]> _finish)
        {
            fa = _fa;
            path = _path;
            bytes = _bytes;
            finish = _finish;
        }
        public void Start()
        {
            if (fa == FileAccess.Read) ThreadManager.Instance.StartThread(Read, Finish);
            else ThreadManager.Instance.StartThread(Write, Finish);
        }
        private void Read(object o)
        {
            if (File.Exists(path))
            {
                //当把一个对象放到using()中的时候，当超出using的作用于范围后，会自动调用该对象的Dispose()方法
                //Dispose自动调用Close和Flush方法
                FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read);
                bytes = new byte[fs.Length];
                fs.Read(bytes, 0, bytes.Length);
                fs.Dispose();
            }
        }
        private void Write(object o)
        {
            FileStream fs = new FileStream(path, FileMode.OpenOrCreate, FileAccess.Write);
            fs.Write(bytes, 0, bytes.Length);
            fs.Dispose();
        }
        private void Finish()
        {
            finish?.Invoke(bytes);
            Instance.Check();
        }
    }
}