using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
namespace TTSDK
{
    public class TTFileSystemManagerDefault : TTFileSystemManager
    {
        public static readonly TTFileSystemManagerDefault Instance = new TTFileSystemManagerDefault();

        /// <summary>
        /// 将字符串写入文件（同步）
        /// </summary>
        /// <param name="filePath">要写入的文件路径</param>
        /// <param name="data">要写入的文本</param>
        /// <param name="encoding">指定写入文件的字符编码</param>
        /// <returns>成功返回空字符串，失败返回错误信息</returns>
        public override string WriteFileSync(string filePath, string data, string encoding = "utf8")
        {
            if (!System.IO.Directory.Exists(System.IO.Path.GetDirectoryName(filePath)))
            {
                return $"{System.IO.Path.GetDirectoryName(filePath)} not exist";
            }

            try
            {
                System.IO.File.WriteAllText(filePath, data);
            }
            catch (System.Exception exception)
            {
                return exception.Message;
            }

            return "";
        }

        /// <summary>
        /// 将二进制写入文件（同步）
        /// </summary>
        /// <param name="filePath">要写入的文件路径</param>
        /// <param name="data">要写入的二进制数据</param>
        /// <returns>成功返回空字符串，失败返回错误信息</returns>
        public override string WriteFileSync(string filePath, byte[] data)
        {
            if (!System.IO.Directory.Exists(System.IO.Path.GetDirectoryName(filePath)))
            {
                return $"{System.IO.Path.GetDirectoryName(filePath)} not exist";
            }

            try
            {
                System.IO.File.WriteAllBytes(filePath, data);
            }
            catch (System.Exception exception)
            {
                return exception.Message;
            }

            return "";
        }

        /// <summary>
        /// 将二进制写入文件（异步）
        /// </summary>
        /// <param name="param"></param>
        public override void WriteFile(WriteFileParam param)
        {
            var errMsg = WriteFileSync(param.filePath, param.data);
            CallbackBaseResponse(errMsg, param.success, param.fail);
        }

        /// <summary>
        /// 将字符串写入文件（异步）
        /// </summary>
        /// <param name="param"></param>
        public override void WriteFile(WriteFileStringParam param)
        {
            var errMsg = WriteFileSync(param.filePath, param.data);
            CallbackBaseResponse(errMsg, param.success, param.fail);
        }

        /// <summary>
        /// 读取本地文件内容（异步）
        /// </summary>
        /// <param name="param"></param>
        public override void ReadFile(ReadFileParam param)
        {
            if (!System.IO.File.Exists(param.filePath))
            {
                CallbackReadFileResponse("file not exist", param.success, param.fail);
                return;
            }

            if (string.IsNullOrEmpty(param.encoding) || param.encoding.Equals("binary"))
            {
                var data = System.IO.File.ReadAllBytes(param.filePath);
                CallbackReadFileResponse("", param.success, param.fail, data);
            }
            else
            {
                var data = System.IO.File.ReadAllText(param.filePath);
                CallbackReadFileResponse("", param.success, param.fail, null, data);
            }
        }

        /// <summary>
        /// 从本地文件读取二进制数据数据（同步）
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns>字节数据，读取失败返回null</returns>
        public override byte[] ReadFileSync(string filePath)
        {
            try
            {
                return System.IO.File.ReadAllBytes(filePath);
            }
            catch (System.Exception exception)
            {
                UnityEngine.Debug.LogError($"ReadFileSync: {exception.Message}");
                return null;
            }
        }

        /// <summary>
        /// 从本地文件读取字符串数据（同步）
        /// </summary>
        /// <param name="filePath">要读取的文件的路径</param>
        /// <param name="encoding">指定读取文件的字符编码, 不能为空</param>
        /// <returns>字符串数据，读取失败返回null</returns>
        public override string ReadFileSync(string filePath, string encoding)
        {
            try
            {
                return System.IO.File.ReadAllText(filePath);
            }
            catch (System.Exception exception)
            {
                UnityEngine.Debug.LogError($"ReadFileSync: {exception.Message}");
                return null;
            }
        }

        /// <summary>
        /// 判断文件/目录是否存在（同步）
        /// </summary>
        /// <param name="path">要判断是否存在的文件/目录路径</param>
        /// <returns>成功返回 true, 失败返回 false</returns>
        public override bool AccessSync(string path)
        {
            return System.IO.File.Exists(path) || System.IO.Directory.Exists(path);
        }

        /// <summary>
        /// 判断文件/目录是否存在（异步）
        /// </summary>
        /// <param name="param"></param>
        public override void Access(AccessParam param)
        {
            var exist = AccessSync(param.path);
            CallbackBaseResponse(exist ? "" : "no such file or directory", param.success, param.fail);
        }

        /// <summary>
        /// 复制文件（同步） 
        /// </summary>
        /// <param name="srcPath">源文件路径</param>
        /// <param name="destPath">目标文件路径</param>
        /// <returns>成功返回空字符串，失败返回错误信息</returns>
        public override string CopyFileSync(string srcPath, string destPath)
        {
            if (System.IO.File.Exists(srcPath))
            {
                try
                {
                    System.IO.File.Copy(srcPath, destPath, true);
                }
                catch (System.Exception exception)
                {
                    return exception.Message;
                }

                return "";
            }
            else
            {
                return "source file not exist";
            }
        }

        /// <summary>
        /// 复制文件（异步）
        /// </summary>
        /// <param name="param"></param>
        public override void CopyFile(CopyFileParam param)
        {
            var errMsg = CopyFileSync(param.srcPath, param.destPath);
            CallbackBaseResponse(errMsg, param.success, param.fail);
        }

        /// <summary>
        /// 重命名文件（异步）
        /// </summary>
        /// <param name="param"></param>
        public override void RenameFile(RenameFileParam param)
        {
            var errMsg = RenameFileSync(param.srcPath, param.destPath);
            CallbackBaseResponse(errMsg, param.success, param.fail);
        }

        /// <summary>
        /// 重命名文件（同步）
        /// </summary>
        /// <param name="srcPath">源文件路径</param>
        /// <param name="destPath">目标文件路径</param>
        /// <returns>成功返回空字符串，失败返回错误信息</returns>
        public override string RenameFileSync(string srcPath, string destPath)
        {
            if (System.IO.File.Exists(srcPath))
            {
                try
                {
                    if (System.IO.File.Exists(destPath))
                    {
                        System.IO.File.Delete(destPath);
                    }

                    System.IO.File.Move(srcPath, destPath);
                }
                catch (System.Exception exception)
                {
                    return exception.Message;
                }

                return "";
            }

            return "source file not exist";
        }

        /// <summary>
        /// 删除文件（同步）
        /// </summary>
        /// <param name="filePath">源文件路径，支持本地路径</param>
        /// <returns>成功返回空字符串，失败返回错误信息</returns>
        public override string UnlinkSync(string filePath)
        {
            if (System.IO.File.Exists(filePath))
            {
                try
                {
                    System.IO.File.Delete(filePath);
                }
                catch (System.Exception exception)
                {
                    return exception.Message;
                }

                return "";
            }

            return "file not exist";
        }

        /// <summary>
        /// 删除文件（异步）
        /// </summary>
        /// <param name="param"></param>
        public override void Unlink(UnlinkParam param)
        {
            var errMsg = UnlinkSync(param.filePath);
            CallbackBaseResponse(errMsg, param.success, param.fail);
        }

        /// <summary>
        /// 创建目录（异步）
        /// </summary>
        /// <param name="param"></param>
        public override void Mkdir(MkdirParam param)
        {
            var errMsg = MkdirSync(param.dirPath, param.recursive);
            CallbackBaseResponse(errMsg, param.success, param.fail);
        }

        /// <summary>
        /// 创建目录（同步）
        /// </summary>
        /// <param name="dirPath">创建的目录路径</param>
        /// <param name="recursive">是否在递归创建该目录的上级目录后再创建该目录。如果对应的上级目录已经存在，则不创建该上级目录。如 dirPath 为 a/b/c/d 且 recursive 为 true，将创建 a 目录，再在 a 目录下创建 b 目录，以此类推直至创建 a/b/c 目录下的 d 目录。</param>
        /// <returns>成功返回空字符串，失败返回错误信息</returns>
        public override string MkdirSync(string dirPath, bool recursive = false)
        {
            if (!System.IO.Directory.Exists(dirPath))
            {
                try
                {
                    System.IO.Directory.CreateDirectory(dirPath);
                }
                catch (System.Exception exception)
                {
                    return exception.Message;
                }
            }

            return "";
        }

        /// <summary>
        /// 删除目录（异步）
        /// </summary>
        /// <param name="param"></param>
        public override void Rmdir(RmdirParam param)
        {
            var errMsg = RmdirSync(param.dirPath, param.recursive);
            CallbackBaseResponse(errMsg, param.success, param.fail);
        }

        /// <summary>
        /// 删除目录（同步）
        /// </summary>
        /// <param name="dirPath">创建的目录路径</param>
        /// <param name="recursive">是否递归删除目录。如果为 true，则删除该目录和该目录下的所有子目录以及文件	。</param>
        /// <returns>成功返回空字符串，失败返回错误信息</returns>
        public override string RmdirSync(string dirPath, bool recursive = false)
        {
            if (System.IO.Directory.Exists(dirPath))
            {
                try
                {
                    System.IO.Directory.Delete(dirPath, recursive);
                }
                catch (System.Exception exception)
                {
                    return exception.Message;
                }

                return "";
            }
            else
            {
                return "directory not exist";
            }
        }

        /// <summary>
        /// 读取文件描述信息（同步）
        /// </summary>
        /// <param name="path">文件/目录路径</param>
        /// <param name="recursive">是否递归获取目录下的每个文件的 Stat 信息	</param>
        /// <param name="throwException">是否抛出错误信息，如果抛出错误信息，当文件不存在时则会抛出异常，错误信息从异常中获取。</param>
        /// <returns>返回文件信息，如果访问失败则返回null</returns>
        public override TTStatInfo StatSync(string path, bool throwException = false)
        {
            if (System.IO.File.Exists(path))
            {
                var fileInfo = new System.IO.FileInfo(path);
                return new TTStatInfo()
                {
                    size = fileInfo.Length,
                    mode = 33060,
                    lastAccessedTime = GetUnixTime(fileInfo.LastAccessTime.Ticks),
                    lastModifiedTime = GetUnixTime(fileInfo.LastWriteTime.Ticks)
                };
            }
            else if (System.IO.Directory.Exists(path))
            {
                var dirInfo = new System.IO.DirectoryInfo(path);
                return new TTStatInfo()
                {
                    size = 0,
                    mode = 16676,
                    lastAccessedTime = GetUnixTime(dirInfo.LastAccessTime.Ticks),
                    lastModifiedTime = GetUnixTime(dirInfo.LastWriteTime.Ticks)
                };
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// 读取文件描述信息（异步）
        /// </summary>
        /// <param name="param"></param>
        public override void Stat(StatParam param)
        {
            var info = StatSync(param.path);
            if (info != null)
            {
                param.success?.Invoke(new TTStatResponse()
                {
                    stat = info
                });
            }
            else
            {
                param.fail?.Invoke(new TTStatResponse()
                {
                    errCode = -1,
                    errMsg = "No such file or directory"
                });
            }
        }

        private void GetFilesRecursively(string path, System.Collections.Generic.List<TTFileInfo> fileInfos)
        {
            System.IO.DirectoryInfo dir = new System.IO.DirectoryInfo(path);
            if (dir.Exists)
            {
                System.IO.FileInfo[] files = dir.GetFiles();
                if (files != null && files.Length > 0)
                {
                    System.DateTime unixEpoch = new System.DateTime(1970, 1, 1, 0, 0, 0, System.DateTimeKind.Utc);
                    foreach (System.IO.FileInfo file in files)
                    {
                        long unixTimeStampInTicks = (file.CreationTime.ToUniversalTime() - unixEpoch).Ticks;
                        long unixTimeStampInSeconds = unixTimeStampInTicks / System.TimeSpan.TicksPerSecond;
                        fileInfos.Add(new TTFileInfo()
                        {
                            mode = 33060,
                            size = file.Length,
                            createTime = unixTimeStampInSeconds,
                            filePath = file.FullName
                        });
                    }
                }

                System.IO.DirectoryInfo[] subDirs = dir.GetDirectories();
                if (subDirs != null && subDirs.Length > 0)
                {
                    System.DateTime unixEpoch = new System.DateTime(1970, 1, 1, 0, 0, 0, System.DateTimeKind.Utc);
                    foreach (var subDir in subDirs)
                    {
                        long unixTimeStampInTicks = (subDir.CreationTime.ToUniversalTime() - unixEpoch).Ticks;
                        long unixTimeStampInSeconds = unixTimeStampInTicks / System.TimeSpan.TicksPerSecond;
                        fileInfos.Add(new TTFileInfo()
                        {
                            mode = 16676,
                            size = 0,
                            createTime = unixTimeStampInSeconds,
                            filePath = subDir.FullName
                        });
                        GetFilesRecursively(subDir.FullName, fileInfos);
                    }
                }
            }
        }

        /// <summary>
        /// 获取保存的用户目录文件列表
        /// </summary>
        public override void GetSavedFileList(GetSavedFileListParam param)
        {
            System.Collections.Generic.List<TTFileInfo> fileInfos =
                new System.Collections.Generic.List<TTFileInfo>();
            GetFilesRecursively(UnityEngine.Application.persistentDataPath, fileInfos);
            param.success?.Invoke(new TTGetSavedFileListResponse()
            {
                fileList = fileInfos.ToArray()
            });
        }

        /// <summary>
        /// 根据url链接获取本地缓存文件路径
        /// </summary>
        /// <param name="url">输入文件下载链接url</param>
        /// <returns>返回本地缓存文件路径，以scfile://user开头的路径，可以直接用这个路径访问该文件</returns>
        public override string GetLocalCachedPathForUrl(string url)
        {
            return "";
        }

        /// <summary>
        /// 判断该url是否有本地缓存文件
        /// </summary>
        /// <param name="url">输入文件下载链接url</param>
        /// <returns>如果存在缓存文件则返回true，不存在缓存文件则返回false</returns>
        public override bool IsUrlCached(string url)
        {
            return false;
        }

        public override string AppendFileSync(string filePath, string data, string encoding = "utf8")
        {
            try
            {
                File.AppendAllText(filePath, data);
            }
            catch (Exception e)
            {
                return e.Message;
            }

            return "";
        }

        public override string AppendFileSync(string filePath, byte[] data)
        {
            try
            {
                using FileStream fs = new FileStream(filePath, FileMode.Append, FileAccess.Write);
                fs.Write(data, 0, data.Length);
            }   
            catch (Exception e)
            {
                return e.Message;
            }

            return "";

        }

        public override void AppendFile(AppendFileParam param)
        {
            var errMsg = AppendFileSync(param.FilePath, param.Data);
            CallbackBaseResponse(errMsg, param.success, param.fail);
        }

        public override void AppendFile(AppendFileStringParam param)
        {
            var errMsg = AppendFileSync(param.FilePath, param.Data, param.Encoding);
            CallbackBaseResponse(errMsg, param.success, param.fail);
        }

        public override void ReadDir(ReadDirParam param)
        {
            var fileList = ReadDirSync(param.DirPath);
            param.success?.Invoke(new TTReadDirResponse{files = fileList});
        }

        public override string[] ReadDirSync(string dirPath)
        {
            try
            {
                if (Directory.Exists(dirPath))
                {
                    return Directory.GetFiles(dirPath);
                }
                else
                {
                    return new string[] { "Directory not exist" };
                }
            }
            catch (Exception e)
            {
                return new string[] { e.Message };
            }
        }

        public override void Truncate(TruncateParam param)
        {
            var errMsg = TruncateSync(param.FilePath, param.Length);
            CallbackBaseResponse(errMsg, param.success, param.fail);
        }

        public override string TruncateSync(string filePath, int length)
        {
            try
            {
                using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Write))
                {
                    fs.SetLength(length);
                }
            }
            catch (Exception e)
            {
                return e.Message;
            }

            return "";
        }

        private static void CallbackReadFileResponse(string errMsg,
            System.Action<TTReadFileResponse> success,
            System.Action<TTReadFileResponse> fail,
            byte[] binData = null,
            string stringData = null)
        {
            if (string.IsNullOrEmpty(errMsg))
            {
                success?.Invoke(new TTReadFileResponse()
                {
                    binData = binData,
                    stringData = stringData
                });
            }
            else
            {
                fail?.Invoke(new TTReadFileResponse()
                {
                    errCode = -1,
                    errMsg = errMsg
                });
            }
        }

        private static void CallbackBaseResponse(string errMsg, System.Action<TTBaseResponse> success,
            System.Action<TTBaseResponse> fail)
        {
            if (string.IsNullOrEmpty(errMsg))
            {
                success?.Invoke(new TTBaseResponse());
            }
            else
            {
                fail?.Invoke(new TTBaseResponse()
                {
                    errCode = -1,
                    errMsg = errMsg
                });
            }
        }

        private static long GetUnixTime(long ticks)
        {
            var epochTicks = new System.DateTime(1970, 1, 1).Ticks;
            var unixTime = ((ticks - epochTicks) / System.TimeSpan.TicksPerSecond);
            return unixTime;
        }

        //文件流式读写
        /// <summary>
        /// (FileStream, string, bool, string)
        /// 文件流对象，打开参数，是否首次操作，路径
        /// </summary>
        private Dictionary<string, (FileStream, string, bool, string)> _openFiles = new();
        private int _fdCounter = 0;

        /// <summary>
        /// 生成唯一标识符
        /// </summary>
        /// <returns></returns>
        private string GenerateFd()
        {
            using var sha256 = SHA256.Create();
            var input = Encoding.UTF8.GetBytes(Guid.NewGuid().ToString() + _fdCounter++);
            var hash = sha256.ComputeHash(input);
            return Convert.ToBase64String(hash).Replace("/", "_").Replace("+", "-").Substring(0, 16);
        }

        /// <summary>
        /// 打开文件
        /// </summary>
        /// <param name="param"></param>
        public override void Open(OpenParam param)
        {
            string err = "";
            string fd = null;
            FileMode mode = FileMode.Open;
            FileAccess access = FileAccess.Read;
            try
            {
                switch (param.flag)
                {
                    case "a":
                        mode = FileMode.OpenOrCreate;
                        access = FileAccess.Write;
                        break;
                    case "ax":
                        mode = FileMode.CreateNew;
                        access = FileAccess.Write;
                        break;
                    case "a+":
                        mode = FileMode.OpenOrCreate;
                        access = FileAccess.ReadWrite;
                        break;
                    case "ax+":
                        mode = FileMode.CreateNew;
                        access = FileAccess.ReadWrite;
                        break;
                    case "as":
                        mode = FileMode.OpenOrCreate;
                        access = FileAccess.Write;
                        break;
                    case "as+":
                        mode = FileMode.OpenOrCreate;
                        access = FileAccess.ReadWrite;
                        break;
                    case "r":
                        mode = FileMode.Open;
                        access = FileAccess.Read;
                        break;
                    case "r+":
                        mode = FileMode.Open;
                        access = FileAccess.ReadWrite;
                        break;
                    case "w":
                        mode = FileMode.Create;
                        access = FileAccess.Write;
                        break;
                    case "wx":
                        mode = FileMode.CreateNew;
                        access = FileAccess.Write;
                        break;
                    case "w+":
                        mode = FileMode.Create;
                        access = FileAccess.ReadWrite;
                        break;
                    case "wx+":
                        mode = FileMode.CreateNew;
                        access = FileAccess.ReadWrite;
                        break;
                    default:
                        throw new Exception("invalid flag");
                }
                var fs = new FileStream(param.filePath, mode, access);
                fd = GenerateFd();
                _openFiles[fd] = (fs, param.flag, true, param.filePath);
            }
            catch (Exception e)
            {
                err = e.Message;
            }

            if (string.IsNullOrEmpty(err))
            {
                param.success?.Invoke(new TTOpenResponse { fd = fd });
            }
            else
            {
                param.fail?.Invoke(new TTOpenResponse { errCode = -1, errMsg = err });
            }
        }

        public override string OpenSync(OpenSyncParam param)
        {
            FileMode mode = FileMode.Open;
            FileAccess access = FileAccess.Read;
            switch (param.flag)
            {
                case "a":
                    mode = FileMode.OpenOrCreate;
                    access = FileAccess.Write;
                    break;
                case "a+":
                    mode = FileMode.OpenOrCreate;
                    access = FileAccess.ReadWrite;
                    break;
                case "as":
                    mode = FileMode.OpenOrCreate;
                    access = FileAccess.Write;
                    break;
                case "as+":
                    mode = FileMode.OpenOrCreate;
                    access = FileAccess.ReadWrite;
                    break;
                case "ax":
                    mode = FileMode.CreateNew;
                    access = FileAccess.Write;
                    break;
                case "ax+":
                    mode = FileMode.CreateNew;
                    access = FileAccess.ReadWrite;
                    break;
                case "r":
                    mode = FileMode.Open;
                    access = FileAccess.Read;
                    break;
                case "r+":
                    mode = FileMode.Open;
                    access = FileAccess.ReadWrite;
                    break;
                case "w":
                    mode = FileMode.Create;
                    access = FileAccess.Write;
                    break;
                case "wx":
                    mode = FileMode.CreateNew;
                    access = FileAccess.Write;
                    break;
                case "w+":
                    mode = FileMode.Create;
                    access = FileAccess.ReadWrite;
                    break;
                case "wx+":
                    mode = FileMode.CreateNew;
                    access = FileAccess.ReadWrite;
                    break;
                default:
                    throw new Exception("invalid flag");
            }
            var fs = new FileStream(param.filePath, mode, access);
            string fd = GenerateFd();
            _openFiles[fd] = (fs, param.flag, true, param.filePath);
            return fd;
        }

        public override void Close(CloseParam param)
        {
            string err = CloseSyncInternal(param.fd);
            CallbackBaseResponse(err, param.success, param.fail);
        }

        public override void CloseSync(CloseSyncParam param)
        {
            var errMsg = CloseSyncInternal(param.fd);
            if (!string.IsNullOrEmpty(errMsg))
            {
                throw new Exception(errMsg);
            }
        }

        private string CloseSyncInternal(string fd)
        {
            if (_openFiles.TryGetValue(fd, out var fs))
            {
                fs.Item1.Close();
                _openFiles.Remove(fd);
                return "";
            }
            return "bad file descriptor";
        }

        public override void Write(WriteBinParam param)
        {
            var err = WriteInternal(param.fd, param.data, param.offset, param.length ?? (param.data.Length - param.offset));
            if (err == null)
            {
                param.success?.Invoke(new TTWriteResponse { bytesWritten = param.length ?? (param.data.Length - param.offset) });
            }
            else
            {
                param.fail?.Invoke(new TTWriteResponse { errCode = -1, errMsg = err });
            }
        }

        public override void Write(WriteStringParam param)
        {
            byte[] data = System.Text.Encoding.UTF8.GetBytes(param.data);
            Write(new WriteBinParam
            {
                fd = param.fd,
                data = data,
                offset = param.offset,
                length = param.length,
                success = param.success,
                fail = param.fail
            });
        }

        public override WriteResult WriteSync(WriteBinSyncParam param)
        {
            var err = WriteInternal(param.fd, param.data, param.offset, param.length ?? (param.data.Length - param.offset));
            if (err == null) return new WriteResult { bytesWritten = param.length ?? (param.data.Length - param.offset)};
            throw new Exception(err);
        }

        public override WriteResult WriteSync(WriteStringSyncParam param)
        {
            byte[] data = System.Text.Encoding.UTF8.GetBytes(param.data);
            return WriteSync(new WriteBinSyncParam
            {
                fd = param.fd,
                data = data,
                encoding = param.encoding
            });
        }

        private string WriteInternal(string fd, byte[] data, int offset, int length)
        {
            if (!_openFiles.TryGetValue(fd, out var fs))
                return "bad file descriptor";
            try
            {
                if(fs.Item2.Contains("a")) fs.Item1.Seek(0, SeekOrigin.End);
                fs.Item1.Write(data, offset, length);
                fs.Item1.Flush();
                return null;
            }
            catch (Exception e)
            {
                return e.Message;
            }
        }

        public override void Read(ReadParam param)
        {
            var (buffer, bytesRead, err) = ReadInternal(param.fd, param.arrayBuffer, param.offset, param.length, param.position);
            if (err == null)
            {
                param.success?.Invoke(new TTReadResponse
                {
                    arrayBuffer = buffer,
                    bytesRead = bytesRead
                });
            }
            else
            {
                param.fail?.Invoke(new TTReadResponse { errCode = -1, errMsg = err });
            }
        }

        public override ReadResult ReadSync(ReadSyncParam param)
        {
            var (buffer, bytesRead, err) = ReadInternal(param.fd, param.arrayBuffer, param.offset, param.length, param.position);
            if (err == null) return new ReadResult { arrayBuffer = buffer, bytesRead = bytesRead };
            throw new Exception(err);
        }

        private (byte[] buffer, int bytesRead, string err) ReadInternal(string fd, byte[] buffer, int offset, int length, int? position)
        {
            if (!_openFiles.TryGetValue(fd, out var fs))
                return (null, 0, "bad file descriptor");
            try
            {
                if (position.HasValue)
                    fs.Item1.Seek(position.Value, SeekOrigin.Begin);

                int read = fs.Item1.Read(buffer, offset, length);
                return (buffer, read, null);
            }
            catch (Exception e)
            {
                return (null, 0, e.Message);
            }
        }

        public override void ReadCompressedFile(ReadCompressedFileParam param)
        {
            string err = "";
            using MemoryStream ms = new MemoryStream();
            try
            {
                if (param.compressionAlgorithm != "br")
                {
                    throw new Exception("brotli decompress fail");
                }
                using FileStream fs = new FileStream(param.filePath, FileMode.Open, FileAccess.Read);
                using BrotliStream brotliStream = new BrotliStream(fs, CompressionMode.Decompress);
                brotliStream.CopyTo(ms);
            }
            catch (Exception e)
            {
                err = e.Message;
            }

            if (string.IsNullOrEmpty(err))
            {
                param.success?.Invoke(new() { arrayBuffer = ms.ToArray() });
            }
            else
            {
                param.fail?.Invoke(new() { errCode = -1, errMsg = err });
            }
        }

        public override byte[] ReadCompressedFileSync(ReadCompressedFileSyncParam param)
        {
            if (param.compressionAlgorithm != "br")
            {
                throw new Exception("brotli decompress fail");
            }
            using FileStream fs = new FileStream(param.filePath, FileMode.Open, FileAccess.Read);
            using BrotliStream brotliStream = new BrotliStream(fs, CompressionMode.Decompress);
            using MemoryStream ms = new MemoryStream();
            brotliStream.CopyTo(ms);
            return ms.ToArray();
        }

        public override void Fstat(FstatParam param)
        {
            if (!_openFiles.TryGetValue(param.fd, out var entry))
            {
                param.fail?.Invoke(new()
                {
                    errCode = -1,
                    errMsg = "bad file descriptor"
                });
                return;
            }

            try
            {
                FileInfo fileInfo = new FileInfo(entry.Item4);
                var info = new TTStatInfo
                {
                    size = fileInfo.Length,
                    mode = 33060,
                    lastAccessedTime = GetUnixTime(fileInfo.LastAccessTimeUtc.Ticks),
                    lastModifiedTime = GetUnixTime(fileInfo.LastWriteTimeUtc.Ticks)
                };
                param.success?.Invoke(new(){ stats = info });
            }
            catch (Exception e)
            {
                param.fail?.Invoke(new(){ errCode = -1, errMsg = e.Message });
            }
        }

        public override TTStatInfo FstatSync(FstatSyncParam param)
        {
            if (!_openFiles.TryGetValue(param.fd, out var entry))
            {
                throw new Exception("bad file descriptor");
            }

            FileInfo fileInfo = new FileInfo(entry.Item4);
            return new TTStatInfo
            {
                size = fileInfo.Length,
                mode = 33060,
                lastAccessedTime = GetUnixTime(fileInfo.LastAccessTimeUtc.Ticks),
                lastModifiedTime = GetUnixTime(fileInfo.LastWriteTimeUtc.Ticks)
            };
        }

        public override void Ftruncate(FtruncateParam param)
        {
            if (!_openFiles.TryGetValue(param.fd, out var entry))
            {
                param.fail?.Invoke(new TTBaseResponse { errCode = -1, errMsg = "bad file descriptor" });
                return;
            }
            try
            {
                entry.Item1.SetLength(param.length);
                entry.Item1.Flush();
                param.success?.Invoke(new TTBaseResponse());
            }
            catch (Exception e)
            {
                param.fail?.Invoke(new TTBaseResponse { errCode = -1, errMsg = e.Message });
            }
        }

        public override void FtruncateSync(FtruncateSyncParam param)
        {
            if (!_openFiles.TryGetValue(param.fd, out var entry))
            {
                throw new Exception("bad file descriptor");
            }

            entry.Item1.SetLength(param.length);
            entry.Item1.Flush();
        }
    }
}