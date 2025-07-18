using System;
using System.Collections.Generic;

namespace YooAsset
{
    /// <summary>
    /// 下载器单元测试
    /// 1. 下载失败重试机制
    /// 2. 下载引用计数机制
    /// 3. 最大下载并发机制
    /// 4. 异步下载远端资源
    /// 5. 同步下载远端资源
    /// 6. 异步拷贝本地资源
    /// 7. 同步拷贝本地资源
    /// 9. 断点续传下载器
    /// </summary>
    internal class DownloadCenterOperation : AsyncOperationBase
    {
        private readonly DefaultCacheFileSystem _fileSystem;
        protected readonly Dictionary<string, UnityDownloadFileOperation> _downloaders = new Dictionary<string, UnityDownloadFileOperation>(1000);
        protected readonly List<string> _removeDownloadList = new List<string>(1000);

        public DownloadCenterOperation(DefaultCacheFileSystem fileSystem)
        {
            _fileSystem = fileSystem;
        }
        internal override void InternalStart()
        {
        }
        internal override void InternalUpdate()
        {
            // 获取可移除的下载器集合
            _removeDownloadList.Clear();
            foreach (var valuePair in _downloaders)
            {
                var downloader = valuePair.Value;
                downloader.UpdateOperation();

                // 注意：主动终止引用计数为零的下载任务
                if (downloader.RefCount <= 0)
                {
                    _removeDownloadList.Add(valuePair.Key);
                    downloader.AbortOperation();
                    continue;
                }

                if (downloader.IsDone)
                {
                    _removeDownloadList.Add(valuePair.Key);
                    continue;
                }
            }

            // 移除下载器
            foreach (var key in _removeDownloadList)
            {
                _downloaders.Remove(key);
            }

            // 最大并发数检测
            int processCount = GetProcessingOperationCount();
            if (processCount != _downloaders.Count)
            {
                if (processCount < _fileSystem.DownloadMaxConcurrency)
                {
                    int startCount = _fileSystem.DownloadMaxConcurrency - processCount;
                    if (startCount > _fileSystem.DownloadMaxRequestPerFrame)
                        startCount = _fileSystem.DownloadMaxRequestPerFrame;

                    foreach (var operationPair in _downloaders)
                    {
                        var operation = operationPair.Value;
                        if (operation.Status == EOperationStatus.None)
                        {
                            operation.StartOperation();
                            startCount--;
                            if (startCount <= 0)
                                break;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 创建下载任务
        /// </summary>
        public UnityDownloadFileOperation DownloadFileAsync(PackageBundle bundle, string url)
        {
            // 查询旧的下载器
            if (_downloaders.TryGetValue(bundle.BundleGUID, out var oldDownloader))
            {
                oldDownloader.Reference();
                return oldDownloader;
            }

            // 创建新的下载器
            UnityDownloadFileOperation newDownloader;
            bool isRequestLocalFile = DownloadSystemHelper.IsRequestLocalFile(url);
            if (isRequestLocalFile)
            {
                newDownloader = new UnityDownloadLocalFileOperation(_fileSystem, bundle, url);
                AddChildOperation(newDownloader);
                _downloaders.Add(bundle.BundleGUID, newDownloader);
            }
            else
            {
                if (bundle.FileSize >= _fileSystem.ResumeDownloadMinimumSize)
                {
                    newDownloader = new UnityDownloadResumeFileOperation(_fileSystem, bundle, url);
                    AddChildOperation(newDownloader);
                    _downloaders.Add(bundle.BundleGUID, newDownloader);
                }
                else
                {
                    newDownloader = new UnityDownloadNormalFileOperation(_fileSystem, bundle, url);
                    AddChildOperation(newDownloader);
                    _downloaders.Add(bundle.BundleGUID, newDownloader);
                }
            }

            newDownloader.Reference();
            return newDownloader;
        }

        /// <summary>
        /// 获取正在进行中的下载器总数
        /// </summary>
        private int GetProcessingOperationCount()
        {
            int count = 0;
            foreach (var operationPair in _downloaders)
            {
                var operation = operationPair.Value;
                if (operation.Status != EOperationStatus.None)
                    count++;
            }
            return count;
        }
    }
}