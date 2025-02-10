#if UNITY_WEBGL && WEIXINMINIGAME
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using YooAsset;
using WeChatWASM;

internal class WXFSClearUnusedBundleFilesAsync : FSClearCacheFilesOperation
{
    private enum ESteps
    {
        None,
        GetUnusedCacheFiles,
        ClearUnusedCacheFiles,
        Done,
    }

    private readonly WechatFileSystem _fileSystem;
    private readonly PackageManifest _manifest;
    private List<string> _unusedCacheFiles;
    private int _unusedFileTotalCount = 0;
    private ESteps _steps = ESteps.None;

    internal WXFSClearUnusedBundleFilesAsync(WechatFileSystem fileSystem, PackageManifest manifest)
    {
        _fileSystem = fileSystem;
        _manifest = manifest;
    }
    internal override void InternalOnStart()
    {
        _steps = ESteps.GetUnusedCacheFiles;
    }
    internal override void InternalOnUpdate()
    {
        if (_steps == ESteps.None || _steps == ESteps.Done)
            return;

        if (_steps == ESteps.GetUnusedCacheFiles)
        {
            _unusedCacheFiles = GetUnusedCacheFiles();
            _unusedFileTotalCount = _unusedCacheFiles.Count;
            _steps = ESteps.ClearUnusedCacheFiles;
            YooLogger.Log($"Found unused cache files count : {_unusedFileTotalCount}");
        }

        if (_steps == ESteps.ClearUnusedCacheFiles)
        {
            for (int i = _unusedCacheFiles.Count - 1; i >= 0; i--)
            {
                string clearFilePath = _unusedCacheFiles[i];
                _unusedCacheFiles.RemoveAt(i);
                _fileSystem.ClearRecord(clearFilePath);
                WX.RemoveFile(clearFilePath, null);

                if (OperationSystem.IsBusy)
                    break;
            }

            if (_unusedFileTotalCount == 0)
                Progress = 1.0f;
            else
                Progress = 1.0f - (_unusedCacheFiles.Count / _unusedFileTotalCount);

            if (_unusedCacheFiles.Count == 0)
            {
                _steps = ESteps.Done;
                Status = EOperationStatus.Succeed;
            }
        }
    }

    private List<string> GetUnusedCacheFiles()
    {
        var allRecords = _fileSystem.GetAllRecords();
        List<string> result = new List<string>(allRecords.Count);
        foreach (var filePath in allRecords)
        {
            // 如果存储文件名是按照Bundle文件哈希值存储
            string bundleGUID = Path.GetFileNameWithoutExtension(filePath);
            if (_manifest.TryGetPackageBundleByBundleGUID(bundleGUID, out PackageBundle value) == false)
            {
                result.Add(filePath);
            }

            // 如果存储文件名是按照Bundle文件名称存储
            /*
            string bundleName = Path.GetFileNameWithoutExtension(filePath);
            if (_manifest.TryGetPackageBundleByBundleName(bundleName, out PackageBundle value) == false)
            {
                result.Add(filePath);
            }
            */
        }
        return result;
    }
}
#endif