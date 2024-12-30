using UniFramework.Event;
using YooAsset;

public class PatchEventDefine
{
    /// <summary>
    /// 补丁包初始化失败
    /// </summary>
    public class InitializeFailed : IEventMessage
    {
        public static void SendEventMessage()
        {
            var msg = new InitializeFailed();
            UniEvent.SendMessage(msg);
        }
    }

    /// <summary>
    /// 补丁流程步骤改变
    /// </summary>
    public class PatchStatesChange : IEventMessage
    {
        public string Tips;

        public static void SendEventMessage(string tips)
        {
            var msg = new PatchStatesChange();
            msg.Tips = tips;
            UniEvent.SendMessage(msg);
        }
    }

    /// <summary>
    /// 发现更新文件
    /// </summary>
    public class FoundUpdateFiles : IEventMessage
    {
        public int TotalCount;
        public long TotalSizeBytes;

        public static void SendEventMessage(int totalCount, long totalSizeBytes)
        {
            var msg = new FoundUpdateFiles();
            msg.TotalCount = totalCount;
            msg.TotalSizeBytes = totalSizeBytes;
            UniEvent.SendMessage(msg);
        }
    }

    /// <summary>
    /// 下载进度更新
    /// </summary>
    public class DownloadProgressUpdate : IEventMessage
    {
        public int TotalDownloadCount;
        public int CurrentDownloadCount;
        public long TotalDownloadSizeBytes;
        public long CurrentDownloadSizeBytes;

        public static void SendEventMessage(DownloadUpdateData updateData)
        {
            var msg = new DownloadProgressUpdate();
            msg.TotalDownloadCount = updateData.TotalDownloadCount;
            msg.CurrentDownloadCount = updateData.CurrentDownloadCount;
            msg.TotalDownloadSizeBytes = updateData.TotalDownloadBytes;
            msg.CurrentDownloadSizeBytes = updateData.CurrentDownloadBytes;
            UniEvent.SendMessage(msg);
        }
    }

    /// <summary>
    /// 资源版本号更新失败
    /// </summary>
    public class PackageVersionUpdateFailed : IEventMessage
    {
        public static void SendEventMessage()
        {
            var msg = new PackageVersionUpdateFailed();
            UniEvent.SendMessage(msg);
        }
    }

    /// <summary>
    /// 补丁清单更新失败
    /// </summary>
    public class PatchManifestUpdateFailed : IEventMessage
    {
        public static void SendEventMessage()
        {
            var msg = new PatchManifestUpdateFailed();
            UniEvent.SendMessage(msg);
        }
    }

    /// <summary>
    /// 网络文件下载失败
    /// </summary>
    public class WebFileDownloadFailed : IEventMessage
    {
        public string FileName;
        public string Error;

        public static void SendEventMessage(DownloadErrorData errorData)
        {
            var msg = new WebFileDownloadFailed();
            msg.FileName = errorData.FileName;
            msg.Error = errorData.ErrorInfo;
            UniEvent.SendMessage(msg);
        }
    }
}