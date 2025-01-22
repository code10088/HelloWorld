using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using UnityEngine;
using UnityEditor;

namespace YooAsset.Editor
{
    public class AssetArtScannerSettingData
    {
        /// <summary>
        /// 配置数据是否被修改
        /// </summary>
        public static bool IsDirty { private set; get; } = false;


        static AssetArtScannerSettingData()
        {
        }

        private static AssetArtScannerSetting _setting = null;
        public static AssetArtScannerSetting Setting
        {
            get
            {
                if (_setting == null)
                    _setting = SettingLoader.LoadSettingData<AssetArtScannerSetting>();
                return _setting;
            }
        }

        /// <summary>
        /// 存储配置文件
        /// </summary>
        public static void SaveFile()
        {
            if (Setting != null)
            {
                IsDirty = false;
                EditorUtility.SetDirty(Setting);
                AssetDatabase.SaveAssets();
                Debug.Log($"{nameof(AssetArtScannerSetting)}.asset is saved!");
            }
        }

        /// <summary>
        /// 清空所有数据
        /// </summary>
        public static void ClearAll()
        {
            Setting.Scanners.Clear();
            SaveFile();
        }

        /// <summary>
        /// 扫描所有项
        /// </summary>
        public static void ScanAll()
        {
            foreach (var scanner in Setting.Scanners)
            {
                var scanResult = ScanInternal(scanner);
                if (scanResult.Succeed == false)
                {
                    Debug.LogError($"Scanner {scanner.ScannerName} failed ! {scanResult.ErrorInfo}");
                }
            }
        }

        /// <summary>
        /// 扫描所有项
        /// </summary>
        public static void ScanAll(string keyword)
        {
            foreach (var scanner in Setting.Scanners)
            {
                if (string.IsNullOrEmpty(keyword) == false)
                {
                    if (scanner.CheckKeyword(keyword) == false)
                        continue;
                }

                var scanResult = ScanInternal(scanner);
                if (scanResult.Succeed == false)
                {
                    Debug.LogError($"Scanner {scanner.ScannerName} failed ! {scanResult.ErrorInfo}");
                }
            }
        }

        /// <summary>
        /// 扫描单项
        /// </summary>
        public static ScannerResult Scan(string scannerGUID)
        {
            AssetArtScanner scanner = GetScannerByGUID(scannerGUID);
            var scanResult = ScanInternal(scanner);
            if (scanResult.Succeed == false)
            {
                Debug.LogError($"Scanner {scanner.ScannerName} failed ! {scanResult.ErrorInfo}");
            }
            return scanResult;
        }

        /// <summary>
        /// 获取指定的扫描器
        /// </summary>
        public static AssetArtScanner GetScannerByGUID(string scannerGUID)
        {
            foreach (var scanner in Setting.Scanners)
            {
                if (scanner.ScannerGUID == scannerGUID)
                    return scanner;
            }

            Debug.LogWarning($"Not found scanner : {scannerGUID}");
            return null;
        }

        // 扫描器编辑相关
        public static AssetArtScanner CreateScanner(string name, string desc)
        {
            AssetArtScanner scanner = new AssetArtScanner();
            scanner.ScannerGUID = System.Guid.NewGuid().ToString();
            scanner.ScannerName = name;
            scanner.ScannerDesc = desc;
            Setting.Scanners.Add(scanner);
            IsDirty = true;
            return scanner;
        }
        public static void RemoveScanner(AssetArtScanner scanner)
        {
            if (Setting.Scanners.Remove(scanner))
            {
                IsDirty = true;
            }
            else
            {
                Debug.LogWarning($"Failed remove scanner : {scanner.ScannerName}");
            }
        }
        public static void ModifyScanner(AssetArtScanner scanner)
        {
            if (scanner != null)
            {
                IsDirty = true;
            }
        }

        // 资源收集编辑相关
        public static void CreateCollector(AssetArtScanner scanner, AssetArtCollector collector)
        {
            scanner.Collectors.Add(collector);
            IsDirty = true;
        }
        public static void RemoveCollector(AssetArtScanner scanner, AssetArtCollector collector)
        {
            if (scanner.Collectors.Remove(collector))
            {
                IsDirty = true;
            }
            else
            {
                Debug.LogWarning($"Failed remove collector : {collector.CollectPath}");
            }
        }
        public static void ModifyCollector(AssetArtScanner scanner, AssetArtCollector collector)
        {
            if (scanner != null && collector != null)
            {
                IsDirty = true;
            }
        }

        private static ScannerResult ScanInternal(AssetArtScanner scanner)
        {
            if (scanner == null)
                return new ScannerResult("Scanner is null !");

            string saveDirectory = "Assets/";
            if (string.IsNullOrEmpty(scanner.SaveDirectory) == false)
            {
                saveDirectory = scanner.SaveDirectory;
                if (Directory.Exists(saveDirectory) == false)
                    return new ScannerResult($"Scanner save directory is invalid : {saveDirectory}");
            }

            ScanReport report = scanner.RunScanner();
            if (report != null)
            {
                string filePath = $"{saveDirectory}/{scanner.ScannerName}_{scanner.ScannerDesc}.json";
                ScanReportConfig.ExportJsonConfig(filePath, report);
                return new ScannerResult(filePath, report);
            }
            else
            {
                return new ScannerResult($"Scanner run failed : {scanner.ScannerName}");
            }
        }
    }
}