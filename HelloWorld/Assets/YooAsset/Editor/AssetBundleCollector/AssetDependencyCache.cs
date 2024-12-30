using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace YooAsset.Editor
{
    public class AssetDependencyCache
    {
        /// <summary>
        /// 资源依赖缓存数据库
        /// </summary>
        private class CacheDatabase
        {
            private class CacheInfo
            {
                /// <summary>
                /// 此哈希函数会聚合了以下内容：源资源路径、源资源、元文件、目标平台以及导入器版本。
                /// 如果此哈希值发送变化，则说明导入资源可能已更改，因此应重新搜集依赖关系。
                /// </summary>
                public string DependHash;

                /// <summary>
                /// 直接依赖资源的GUID列表
                /// </summary>
                public List<string> DependGUIDs = new List<string>();
            }

            private string _databaseFilePath;
            private readonly Dictionary<string, CacheInfo> _database = new Dictionary<string, CacheInfo>(100000);

            /// <summary>
            /// 创建数据库
            /// </summary>
            public void CreateDatabase(string databaseFilePath, bool useCacheDatabase)
            {
                _databaseFilePath = databaseFilePath;
                _database.Clear();

                try
                {
                    if (useCacheDatabase && File.Exists(databaseFilePath))
                    {
                        // 解析缓存文件
                        using var stream = File.OpenRead(databaseFilePath);
                        using var reader = new BinaryReader(stream);
                        var count = reader.ReadInt32();
                        for (int i = 0; i < count; i++)
                        {
                            var assetPath = reader.ReadString();
                            var cacheInfo = new CacheInfo
                            {
                                DependHash = reader.ReadString(),
                                DependGUIDs = ReadStringList(reader),
                            };
                            _database.Add(assetPath, cacheInfo);
                        }

                        // 移除无效资源
                        List<string> removeList = new List<string>(10000);
                        foreach (var cacheInfoPair in _database)
                        {
                            var assetPath = cacheInfoPair.Key;
                            var assetGUID = AssetDatabase.AssetPathToGUID(assetPath);
                            if (string.IsNullOrEmpty(assetGUID))
                            {
                                removeList.Add(assetPath);
                            }
                        }
                        foreach (var assetPath in removeList)
                        {
                            _database.Remove(assetPath);
                        }
                    }
                }
                catch (Exception ex)
                {
                    ClearCache(true);
                    Debug.LogError($"Failed to load cache database : {ex.Message}");
                }

                // 查找新增或变动资源
                var allAssetPaths = AssetDatabase.GetAllAssetPaths();
                foreach (var assetPath in allAssetPaths)
                {
                    if (_database.TryGetValue(assetPath, out CacheInfo cacheInfo))
                    {
                        var dependHash = AssetDatabase.GetAssetDependencyHash(assetPath);
                        if (dependHash.ToString() != cacheInfo.DependHash)
                        {
                            _database[assetPath] = CreateCacheInfo(assetPath);
                        }
                    }
                    else
                    {
                        var newCacheInfo = CreateCacheInfo(assetPath);
                        _database.Add(assetPath, newCacheInfo);
                    }
                }
            }

            /// <summary>
            /// 保存缓存文件
            /// </summary>
            public void SaveCacheFile()
            {
                if (File.Exists(_databaseFilePath))
                    File.Delete(_databaseFilePath);

                try
                {
                    using var stream = File.Create(_databaseFilePath);
                    using var writer = new BinaryWriter(stream);
                    writer.Write(_database.Count);
                    foreach (var assetPair in _database)
                    {
                        string assetPath = assetPair.Key;
                        var assetInfo = assetPair.Value;
                        writer.Write(assetPath);
                        writer.Write(assetInfo.DependHash);
                        WriteStringList(writer, assetInfo.DependGUIDs);
                    }
                    writer.Flush();
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Failed to save cache database : {ex.Message}");
                }
            }

            /// <summary>
            /// 清理缓存数据
            /// </summary>
            public void ClearCache(bool clearDatabaseFile)
            {
                if (clearDatabaseFile)
                {
                    if (File.Exists(_databaseFilePath))
                        File.Delete(_databaseFilePath);
                }

                _database.Clear();
            }

            /// <summary>
            ///  获取资源的依赖列表
            /// </summary>
            public string[] GetDependencies(string assetPath, bool recursive)
            {
                // 注意：AssetDatabase.GetDependencies()方法返回结果里会踢出丢失文件！
                // 注意：AssetDatabase.GetDependencies()方法返回结果里会包含主资源路径！

                // 注意：机制上不允许存在未收录的资源
                if (_database.ContainsKey(assetPath) == false)
                {
                    throw new Exception($"Fatal : can not found cache info : {assetPath}");
                }

                var result = new HashSet<string> { assetPath };
                CollectDependencies(assetPath, result, recursive);

                // 注意：AssetDatabase.GetDependencies保持一致，将主资源添加到依赖列表最前面
                return result.ToArray();
            }
            private void CollectDependencies(string assetPath, HashSet<string> result, bool recursive)
            {
                if (_database.TryGetValue(assetPath, out var cacheInfo) == false)
                {
                    throw new Exception($"Fatal : can not found cache info : {assetPath}");
                }

                foreach (var dependGUID in cacheInfo.DependGUIDs)
                {
                    string dependAssetPath = AssetDatabase.GUIDToAssetPath(dependGUID);
                    if (string.IsNullOrEmpty(dependAssetPath))
                        continue;

                    // 如果是文件夹资源
                    if (AssetDatabase.IsValidFolder(dependAssetPath))
                        continue;

                    // 如果已经收集过
                    if (result.Contains(dependAssetPath))
                        continue;

                    result.Add(dependAssetPath);

                    // 递归收集依赖
                    if (recursive)
                        CollectDependencies(dependAssetPath, result, recursive);
                }
            }

            private List<string> ReadStringList(BinaryReader reader)
            {
                var count = reader.ReadInt32();
                var values = new List<string>(count);
                for (int i = 0; i < count; i++)
                {
                    values.Add(reader.ReadString());
                }
                return values;
            }
            private void WriteStringList(BinaryWriter writer, List<string> values)
            {
                writer.Write(values.Count);
                foreach (var value in values)
                {
                    writer.Write(value);
                }
            }
            private CacheInfo CreateCacheInfo(string assetPath)
            {
                var dependHash = AssetDatabase.GetAssetDependencyHash(assetPath);
                var dependAssetPaths = AssetDatabase.GetDependencies(assetPath, false);
                var dependGUIDs = new List<string>();
                foreach (var dependAssetPath in dependAssetPaths)
                {
                    string guid = AssetDatabase.AssetPathToGUID(dependAssetPath);
                    if (string.IsNullOrEmpty(guid) == false)
                    {
                        dependGUIDs.Add(guid);
                    }
                }

                var cacheInfo = new CacheInfo();
                cacheInfo.DependHash = dependHash.ToString();
                cacheInfo.DependGUIDs = dependGUIDs;
                return cacheInfo;
            }
        }

        private readonly CacheDatabase _database;

        /// <summary>
        /// 初始化资源依赖缓存系统
        /// </summary>
        public AssetDependencyCache(bool useCacheDatabase)
        {
            if (useCacheDatabase)
                Debug.Log("Use asset dependency database !");

            string databaseFilePath = "Library/AssetDependencyDB";
            _database = new CacheDatabase();
            _database.CreateDatabase(databaseFilePath, useCacheDatabase);

            if (useCacheDatabase)
            {
                _database.SaveCacheFile();
            }
        }

        /// <summary>
        ///  获取资源的依赖列表
        /// </summary>
        /// <param name="assetPath">资源路径</param>
        /// <param name="recursive">递归查找所有依赖</param>
        /// <returns>返回依赖的资源路径集合</returns>
        public string[] GetDependencies(string assetPath, bool recursive = true)
        {
            // 通过本地缓存获取依赖关系
            return _database.GetDependencies(assetPath, recursive);

            // 通过Unity引擎获取依赖关系
            //return AssetDatabase.GetDependencies(assetPath, recursive);
        }
    }
}