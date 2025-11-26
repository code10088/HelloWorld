using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

public static class FileUtils
{
    public static string PlatformPath
    {
        get
        {
            return Application.persistentDataPath + "/";
        }
    }

    #region 运行时多线程读写文件
    private static List<FileItem> queue = new List<FileItem>();
    private static int count = 0;

    public static void Read(string path, Action<byte[]> finish)
    {
        FileItem item = new FileItem(FileAccess.Read, path, null, finish);
        queue.Add(item);
        Check();
    }
    public static void Write(string path, byte[] bytes, Action<byte[]> finish)
    {
        FileItem item = new FileItem(FileAccess.Write, path, bytes, finish);
        queue.Add(item);
        Check();
    }
    private static void Check()
    {
        if (count < GameSetting.writeLimit && queue.Count > 0)
        {
            FileItem item = queue[0];
            queue.RemoveAt(0);
            item.Start();
            count++;
        }
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
            if (fa == FileAccess.Read) Driver.Instance.StartTask(Read, Finish);
            else Driver.Instance.StartTask(Write, Finish);
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
            Check();
        }
    }
    #endregion

    public static bool CheckDownloaded(string[] path)
    {
        for (int i = 0; i < path.Length; i++)
        {
            if (!CheckDownloaded(path[i])) return false;
        }
        return true;
    }
    public static bool CheckDownloaded(string path)
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

    public static string MD5(string fileName)
    {
        string hashMD5 = string.Empty;
        if (File.Exists(fileName))
        {
            using (FileStream fs = new FileStream(fileName, FileMode.Open, FileAccess.Read))
            {
                MD5 calculator = System.Security.Cryptography.MD5.Create();
                byte[] buffer = calculator.ComputeHash(fs);
                calculator.Clear();
                StringBuilder stringBuilder = new StringBuilder();
                for (int i = 0; i < buffer.Length; i++)
                {
                    stringBuilder.Append(buffer[i].ToString("x2"));
                }
                hashMD5 = stringBuilder.ToString();
            }
        }
        return hashMD5;
    }
    public static void GetAllFilePath(string path, List<FileInfo> fileList, string selection = "")
    {
        DirectoryInfo dir = new DirectoryInfo(path);
        FileInfo[] fileInfos = dir.GetFiles();
        for (int i = 0; i < fileInfos.Length; i++)
        {
            if (fileInfos[i].Name.EndsWith(selection))
                fileList.Add(fileInfos[i]);
        }
        DirectoryInfo[] directoryInfos = dir.GetDirectories();
        for (int i = 0; i < directoryInfos.Length; i++)
        {
            GetAllFilePath(directoryInfos[i].FullName, fileList, selection);
        }
    }
}