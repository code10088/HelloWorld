﻿using System.IO;
using UnityEngine;
using UnityEngine.Networking;

namespace YooAsset
{
    internal sealed class DownloadNormalFileOperation : DefaultDownloadFileOperation
    {
        private readonly DefaultCacheFileSystem _fileSystem;
        private VerifyTempFileOperation _verifyOperation;
        private string _tempFilePath;
        private ESteps _steps = ESteps.None;

        internal DownloadNormalFileOperation(DefaultCacheFileSystem fileSystem, PackageBundle bundle, DownloadFileOptions options) : base(bundle, options)
        {
            _fileSystem = fileSystem;
        }
        internal override void InternalStart()
        {
            _tempFilePath = _fileSystem.GetTempFilePath(Bundle);
            _steps = ESteps.CheckExists;
        }
        internal override void InternalUpdate()
        {
            if (_steps == ESteps.None || _steps == ESteps.Done)
                return;

            // 检测文件是否存在
            if (_steps == ESteps.CheckExists)
            {
                if (_fileSystem.Exists(Bundle))
                {
                    _steps = ESteps.Done;
                    Status = EOperationStatus.Succeed;
                }
                else
                {
                    _steps = ESteps.CreateRequest;
                }
            }

            // 创建下载器
            if (_steps == ESteps.CreateRequest)
            {
                FileUtility.CreateFileDirectory(_tempFilePath);

                // 删除临时文件
                if (File.Exists(_tempFilePath))
                    File.Delete(_tempFilePath);

                // 获取请求地址
                _requestURL = GetRequestURL();

                // 重置请求
                ResetRequestFiled();

                // 创建下载器
                CreateWebRequest();

                _steps = ESteps.CheckRequest;
            }

            // 检测下载结果
            if (_steps == ESteps.CheckRequest)
            {
                DownloadProgress = _webRequest.downloadProgress;
                DownloadedBytes = (long)_webRequest.downloadedBytes;
                Progress = DownloadProgress;
                if (_webRequest.isDone == false)
                {
                    CheckRequestTimeout();
                    return;
                }

                // 检查网络错误
                if (CheckRequestResult())
                    _steps = ESteps.VerifyTempFile;
                else
                    _steps = ESteps.TryAgain;

                // 注意：最终释放请求器
                DisposeWebRequest();
            }

            // 验证下载文件
            if (_steps == ESteps.VerifyTempFile)
            {
                var element = new TempFileElement(_tempFilePath, Bundle.FileCRC, Bundle.FileSize);
                _verifyOperation = new VerifyTempFileOperation(element);
                _verifyOperation.StartOperation();
                AddChildOperation(_verifyOperation);
                _steps = ESteps.CheckVerifyTempFile;
            }

            // 等待验证完成
            if (_steps == ESteps.CheckVerifyTempFile)
            {
                if (IsWaitForAsyncComplete)
                    _verifyOperation.WaitForAsyncComplete();

                _verifyOperation.UpdateOperation();
                if (_verifyOperation.IsDone == false)
                    return;

                if (_verifyOperation.Status == EOperationStatus.Succeed)
                {
                    if (_fileSystem.WriteCacheBundleFile(Bundle, _tempFilePath))
                    {
                        _steps = ESteps.Done;
                        Status = EOperationStatus.Succeed;
                    }
                    else
                    {
                        _steps = ESteps.Done;
                        Status = EOperationStatus.Failed;
                        Error = $"{_fileSystem.GetType().FullName} failed to write file !";
                        YooLogger.Error(Error);
                    }
                }
                else
                {
                    _steps = ESteps.TryAgain;
                    Error = _verifyOperation.Error;
                }

                // 注意：验证完成后直接删除文件
                if (File.Exists(_tempFilePath))
                    File.Delete(_tempFilePath);
            }

            // 重新尝试下载
            if (_steps == ESteps.TryAgain)
            {
                if (FailedTryAgain <= 0)
                {
                    Status = EOperationStatus.Failed;
                    _steps = ESteps.Done;
                    YooLogger.Error(Error);
                    return;
                }

                _tryAgainTimer += Time.unscaledDeltaTime;
                if (_tryAgainTimer > 1f)
                {
                    FailedTryAgain--;
                    _steps = ESteps.CreateRequest;
                    YooLogger.Warning(Error);
                }
            }
        }
        internal override void InternalAbort()
        {
            _steps = ESteps.Done;
            DisposeWebRequest();
        }
        internal override void InternalWaitForAsyncComplete()
        {
            while (true)
            {
                if (ExecuteWhileDone())
                {
                    //TODO 尝试同步加载远端的资源文件失败
                    if (Status == EOperationStatus.Failed)
                    {
                        YooLogger.Error($"Try load bundle {Bundle.BundleName} from remote !");
                        YooLogger.Error($"The load remote bundle url : {_requestURL}");
                    }

                    _steps = ESteps.Done;
                    break;
                }
            }
        }

        private void CreateWebRequest()
        {
            _webRequest = DownloadSystemHelper.NewUnityWebRequestGet(_requestURL);
            DownloadHandlerFile handler = new DownloadHandlerFile(_tempFilePath);
            handler.removeFileOnAbort = true;
            _webRequest.downloadHandler = handler;
            _webRequest.disposeDownloadHandlerOnDispose = true;
            _webRequest.SendWebRequest();
        }
        private void DisposeWebRequest()
        {
            if (_webRequest != null)
            {
                //注意：引擎底层会自动调用Abort方法
                _webRequest.Dispose();
                _webRequest = null;
            }
        }
    }
}