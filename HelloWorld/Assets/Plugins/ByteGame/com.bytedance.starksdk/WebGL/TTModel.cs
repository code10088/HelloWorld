namespace TTSDK
{
    public class TTBaseResponse
    {
        public string callbackId; //回调id,调用者不需要关注
        public string errMsg;
        public int errCode;
    }

    public class TTBaseActionParam<T>
    {
        public System.Action<T> success; //接口调用成功的回调函数
        public System.Action<T> fail; //接口调用失败的回调函数	
    }

    public class TTReadFileResponse : TTBaseResponse
    {
        /// <summary>
        /// 如果返回二进制，则数据在这个字段
        /// </summary>
        public byte[] binData;

        /// <summary>
        /// 如果返回的是字符串，则数据在这个字段
        /// </summary>
        public string stringData;
    }

    public class AccessParam : TTBaseActionParam<TTBaseResponse>
    {
        public string path;
    }

    public class UnlinkParam : TTBaseActionParam<TTBaseResponse>
    {
        public string filePath;
    }

    public class MkdirParam : TTBaseActionParam<TTBaseResponse>
    {
        /// <summary>
        /// 创建的目录路径 (本地路径)
        /// </summary>
        public string dirPath;

        /// <summary>
        /// 是否在递归创建该目录的上级目录后再创建该目录。如果对应的上级目录已经存在，则不创建该上级目录。如 dirPath 为 a/b/c/d 且 recursive 为 true，将创建 a 目录，再在 a 目录下创建 b 目录，以此类推直至创建 a/b/c 目录下的 d 目录。
        /// </summary>
        public bool recursive = false;
    }

    public class RmdirParam : TTBaseActionParam<TTBaseResponse>
    {
        /// <summary>
        /// 删除的目录路径 (本地路径)
        /// </summary>
        public string dirPath;

        /// <summary>
        /// 是否递归删除目录。如果为 true，则删除该目录和该目录下的所有子目录以及文件。
        /// </summary>
        public bool recursive = false;
    }

    public class CopyFileParam : TTBaseActionParam<TTBaseResponse>
    {
        public string srcPath;
        public string destPath;
    }

    public class RenameFileParam : TTBaseActionParam<TTBaseResponse>
    {
        public string srcPath;
        public string destPath;
    }

    public class WriteFileParam : TTBaseActionParam<TTBaseResponse>
    {
        /// <summary>
        /// 要写入的文件路径 (本地路径)
        /// </summary>
        public string filePath;

        /// <summary>
        /// 要写入的二进制数据
        /// </summary>
        public byte[] data;
    }

    public class WriteFileStringParam : TTBaseActionParam<TTBaseResponse>
    {
        /// <summary>
        /// 要写入的文件路径 (本地路径)
        /// </summary>
        public string filePath;

        /// <summary>
        /// 要写入的二进制数据
        /// </summary>
        public string data;

        /// <summary>
        /// 指定写入文件的字符编码
        /// </summary>
        public string encoding = "utf8";
    }

    public class ReadFileParam : TTBaseActionParam<TTReadFileResponse>
    {
        /// <summary>
        /// 要读取的文件的路径 (本地路径)
        /// </summary>
        public string filePath;

        /// <summary>
        /// 指定读取文件的字符编码，如果不传 encoding，则以 ArrayBuffer 格式读取文件的二进制内容
        /// </summary>
        public string encoding;
    }

    public class StatParam : TTBaseActionParam<TTStatResponse>
    {
        /// <summary>
        /// 文件/目录路径
        /// </summary>
        public string path;
    }

    public class GetSavedFileListParam : TTBaseActionParam<TTGetSavedFileListResponse>
    {
    }

    public class TTReadFileCallback : TTBaseResponse
    {
        public string data;
        public int byteLength;
    }
    
    public class TTGetSavedFileListResponse : TTBaseResponse
    {
        public TTFileInfo[] fileList;
    }

    public class RemoveSavedFileParam : TTBaseActionParam<TTBaseResponse>
    {
        public string FilePath;
    }
    
    public class TTStatResponse : TTBaseResponse
    {
        public TTStatInfo stat;
    }

    public class TTBaseFileInfo
    {
        /// <summary>
        /// 文件大小，单位：B
        /// </summary>
        public long size;

        /// <summary>
        /// 文件的类型和存取的权限
        /// </summary>
        public int mode;

        /// <summary>
        /// 判断当前文件是否一个普通文件
        /// </summary>
        /// <returns>是普通文件返回true，不是则返回false</returns>
        public bool IsFile()
        {
            return (61440 & mode) == 32768;
        }

        /// <summary>
        /// 判断当前文件是否一个目录
        /// </summary>
        /// <returns>是目录返回true，不是则返回false</returns>
        public bool IsDirectory()
        {
            return (61440 & mode) == 16384;
        }
    }

    public class TTFileInfo : TTBaseFileInfo
    {
        /// <summary>
        /// 文件创建时间
        /// </summary>
        public long createTime;

        /// <summary>
        /// 文件路径
        /// </summary>
        public string filePath;
    }

    public class TTStatInfo : TTBaseFileInfo
    {
        /// <summary>
        /// 文件最近一次被存取或被执行的时间
        /// </summary>
        public long lastAccessedTime;

        /// <summary>
        /// 文件最后一次被修改的时间
        /// </summary>
        public long lastModifiedTime;
    }
    
    
    public class AppendFileParam : TTBaseActionParam<TTBaseResponse>
    {
        /// <summary>
        /// 要追加写入的文件路径
        /// </summary>
        public string FilePath;

        /// <summary>
        /// 要追加写入的二进制数据
        /// </summary>
        public byte[] Data;
    }

    public class AppendFileStringParam : TTBaseActionParam<TTBaseResponse>
    {
        /// <summary>
        /// 要追加写入的文件路径
        /// </summary>
        public string FilePath;
        /// <summary>
        /// 要追加写入的数据
        /// </summary>
        public string Data;
        /// <summary>
        /// 指定写入文件的字符编码
        /// </summary>
        public string Encoding = "utf8";
    }
    
    public class TTReadDirResponse : TTBaseResponse
    {
        public string[] files;
    }
    
    public class ReadDirParam : TTBaseActionParam<TTReadDirResponse>
    {
        /// <summary>
        /// 要读取的目录路径
        /// </summary>
        public string DirPath;
    }
    
    public class TruncateParam: TTBaseActionParam<TTBaseResponse>
    {
        /// <summary>
        /// 要截断的文件路径
        /// </summary>
        public string FilePath;
        /// <summary>
        /// 截断的长度
        /// </summary>
        public int Length;
    }

    public class TTOpenResponse:TTBaseResponse
    {
        /// <summary>
        /// 文件标识符
        /// </summary>
        public string fd;
    }

    public class OpenParam: TTBaseActionParam<TTOpenResponse>
    {
        /// <summary>
        /// 文件路径
        /// </summary>
        public string filePath;
        /// <summary>
        /// 文件系统标志
        /// </summary>
        public string flag;
    }

    public class OpenSyncParam
    {
        /// <summary>
        /// 文件路径
        /// </summary>
        public string filePath;
        /// <summary>
        /// 文件系统标志
        /// </summary>
        public string flag;
    }

    public class CloseParam: TTBaseActionParam<TTBaseResponse>
    {
        /// <summary>
        /// 文件标识符
        /// </summary>
        public string fd;
    }

    public class CloseSyncParam
    {
        /// <summary>
        /// 文件标识符
        /// </summary>
        public string fd;
    }

    public class TTWriteResponse: TTBaseResponse
    {
        /// <summary>
        /// 被写入到文件中的字节数
        /// </summary>
        public int bytesWritten;
    }

    public class WriteParam: TTBaseActionParam<TTWriteResponse>
    {
        /// <summary>
        /// 文件标识符
        /// </summary>
        public string fd;
        /// <summary>
        /// ArrayBuffer 时有效，决定 ArrayBuffer 中要被写入的部位
        /// </summary>
        public int offset;
        /// <summary>
        /// ArrayBuffer 时有效，指定要写入的字节数
        /// </summary>
        public int? length;
        /// <summary>
        /// String 时有效，指定写入文件的字符编码
        /// </summary>
        public string encoding;
    }


    public class WriteBinParam: WriteParam
    {
        /// <summary>
        /// 写入数据
        /// </summary>
        public byte[] data;
    }

    public class WriteStringParam: WriteParam
    {
        /// <summary>
        /// 写入数据
        /// </summary>
        public string data;
    }

    public class WriteBinSyncParam
    {
        /// <summary>
        /// 文件标识符
        /// </summary>
        public string fd;
        /// <summary>
        /// 写入数据
        /// </summary>
        public byte[] data;
        /// <summary>
        /// ArrayBuffer 时有效，决定 ArrayBuffer 中要被写入的部位
        /// </summary>
        public int offset;
        /// <summary>
        /// ArrayBuffer 时有效，指定要写入的字节数
        /// </summary>
        public int? length;
        /// <summary>
        /// String 时有效，指定写入文件的字符编码
        /// </summary>
        public string encoding;
    }

    public class WriteStringSyncParam
    {
        /// <summary>
        /// 文件标识符
        /// </summary>
        public string fd;
        /// <summary>
        /// 写入数据
        /// </summary>
        public string data;
        /// <summary>
        /// ArrayBuffer 时有效，决定 ArrayBuffer 中要被写入的部位
        /// </summary>
        public int offset;
        /// <summary>
        /// ArrayBuffer 时有效，指定要写入的字节数
        /// </summary>
        public int? length;
        /// <summary>
        /// String 时有效，指定写入文件的字符编码
        /// </summary>
        public string encoding;
    }

    public class WriteResult
    {
        /// <summary>
        /// 被写入到文件中的字节数
        /// </summary>
        public int bytesWritten;
    }

    public class TTReadResponse: TTBaseResponse
    {
        /// <summary>
        /// 被写入的缓存区的对象
        /// </summary>
        public byte[] arrayBuffer;
        /// <summary>
        /// 实际读取的字节数
        /// </summary>
        public int bytesRead;
    }

    public class ReadParam: TTBaseActionParam<TTReadResponse>
    {
        /// <summary>
        /// 文件标识符
        /// </summary>
        public string fd;
        /// <summary>
        /// 数据
        /// </summary>
        public byte[] arrayBuffer;
        /// <summary>
        /// 缓冲区中的写入偏移量
        /// </summary>
        public int offset;
        /// <summary>
        /// 要从文件中读取的字节数
        /// </summary>
        public int length;
        /// <summary>
        /// 文件读取的起始位置
        /// </summary>
        public int? position;
    }

    public class ReadSyncParam
    {
        /// <summary>
        /// 文件标识符
        /// </summary>
        public string fd;
        /// <summary>
        /// 数据
        /// </summary>
        public byte[] arrayBuffer;
        /// <summary>
        /// 缓冲区中的写入偏移量
        /// </summary>
        public int offset;
        /// <summary>
        /// 要从文件中读取的字节数
        /// </summary>
        public int length;
        /// <summary>
        /// 文件读取的起始位置
        /// </summary>
        public int? position;
    }

    public class ReadResult
    {
        /// <summary>
        /// 被写入的缓存区的对象
        /// </summary>
        public byte[] arrayBuffer;
        /// <summary>
        /// 实际读取的字节数
        /// </summary>
        public int bytesRead;
    }

    public class TTReadCompressedCallback: TTBaseResponse
    {
        /// <summary>
        /// 文件长度
        /// </summary>
        public int byteLength;
    }

    public class TTReadCompressedFileResponse: TTBaseResponse
    {
        /// <summary>
        /// 文件内容
        /// </summary>
        public byte[] arrayBuffer;
    }

    public class ReadCompressedFileParam: TTBaseActionParam<TTReadCompressedFileResponse>
    {
        /// <summary>
        /// 文件路径
        /// </summary>
        public string filePath;
        /// <summary>
        /// 文件压缩类型
        /// </summary>
        public string compressionAlgorithm;
    }

    public class ReadCompressedFileSyncParam
    {
        /// <summary>
        /// 文件路径
        /// </summary>
        public string filePath;
        /// <summary>
        /// 文件压缩类型
        /// </summary>
        public string compressionAlgorithm;
    }

    public class FstatResponse: TTBaseResponse
    {
        /// <summary>
        /// 文件状态
        /// </summary>
        public TTStatInfo stats;
    }

    public class FstatParam: TTBaseActionParam<FstatResponse>
    {
        /// <summary>
        /// 文件标识符
        /// </summary>
        public string fd;
    }

    public class FstatSyncParam
    {
        /// <summary>
        /// 文件标识符
        /// </summary>
        public string fd;
    }

    public class FtruncateParam: TTBaseActionParam<TTBaseResponse>
    {
        /// <summary>
        /// 文件标识符
        /// </summary>
        public string fd;
        /// <summary>
        /// 截断位置
        /// </summary>
        public int length;
    }

    public class FtruncateSyncParam
    {
        /// <summary>
        /// 文件标识符
        /// </summary>
        public string fd;
        /// <summary>
        /// 截断位置
        /// </summary>
        public int length;
    }
}