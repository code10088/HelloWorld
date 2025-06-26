// GPU Instancer Pro
// Copyright (c) GurBu Technologies

using System;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace GPUInstancerPro
{
    public class GPUIProPackageImporterData : ScriptableObject
    {
        [Tooltip("Custom domain name for asset")]
        public string domain = "com.mycompany.myasset";
        [Tooltip("Package definitions")]
        public PackageDefinition[] packageDefinitions;
        [Tooltip("Determines if the imported packages data will be stored within this file or on a separate file.")]
        public bool storeImportedDataExternally;
        [HideInInspector]
        public List<ImportedPackageInfo> importedPackageInfos;
        [HideInInspector]
        [Tooltip("Sub Folder will be created under this folder.")]
        public UnityEngine.Object importedDataFolder;
        [HideInInspector]
        [Tooltip("Imported data will be created under this folder.")]
        public string importedDataSubFolder;
        [NonSerialized]
        public bool forceReimport;
        [NonSerialized]
        private GPUIProPackageImporterImportedData _importedData;

        private readonly string IMPORTED_PACKAGES_DATA_FILE_NAME = "GPUIProPackageImportedData";

        public bool Validate()
        {
            if (string.IsNullOrEmpty(domain))
            {
                Debug.LogError(GPUIConstants.LOG_PREFIX + "Domain name is not provided!", this);
                return false;
            }
            for (int i = 0; i < packageDefinitions.Length; i++)
            {
                if (!packageDefinitions[i].Validate(this))
                    return false;
            }
            return true;
        }

        public List<ImportedPackageInfo> GetImportedPackageInfos()
        {
            if (!storeImportedDataExternally)
            {
                if (importedPackageInfos == null)
                    importedPackageInfos = new List<ImportedPackageInfo>();
                return importedPackageInfos;
            }
            if (_importedData == null)
            {
#if UNITY_EDITOR
                LoadExternalImportedData();
                if (_importedData == null)
                {
#endif
                    _importedData = ScriptableObject.CreateInstance<GPUIProPackageImporterImportedData>();
#if UNITY_EDITOR
                    AssetDatabase.CreateAsset(_importedData, GetExternalImportedDataPath());
                }
#endif
            }
            if (_importedData.importedPackageInfos == null)
                _importedData.importedPackageInfos = new List<ImportedPackageInfo>();
            return _importedData.importedPackageInfos;
        }

        public void SaveImportedPackageInfos()
        {
#if UNITY_EDITOR
            if (!storeImportedDataExternally)
                EditorUtility.SetDirty(this);
            else
                EditorUtility.SetDirty(_importedData);
#endif
        }

        public void ClearImportedData()
        {
            importedPackageInfos = new List<ImportedPackageInfo>();
#if UNITY_EDITOR
            LoadExternalImportedData();

            if (_importedData != null)
            {
                string filePath = AssetDatabase.GetAssetPath(_importedData);
                if (!string.IsNullOrEmpty(filePath))
                {
                    AssetDatabase.DeleteAsset(filePath);
                    AssetDatabase.SaveAssets();
                    AssetDatabase.Refresh();
                }
            }
#endif
        }

        public bool HasImportedData()
        {
            if (!storeImportedDataExternally)
                return importedPackageInfos != null && importedPackageInfos.Count > 0;
            LoadExternalImportedData();
            if (_importedData == null)
                return false;
            return _importedData.importedPackageInfos != null && _importedData.importedPackageInfos.Count > 0;
        }

#if UNITY_EDITOR
        private void LoadExternalImportedData()
        {
            if (_importedData == null && storeImportedDataExternally)
                _importedData = AssetDatabase.LoadAssetAtPath<GPUIProPackageImporterImportedData>(GetExternalImportedDataPath());
        }

        private string GetExternalImportedDataPath()
        {
            string folderPath = GPUIConstants.GetDefaultUserDataPath() + GPUIConstants.PATH_EDITOR;
            if (!Directory.Exists(folderPath))
                Directory.CreateDirectory(folderPath);
            return folderPath + IMPORTED_PACKAGES_DATA_FILE_NAME + ".asset";
        }
#endif

        [Serializable]
        public struct PackageDefinition
        {
            public string packageName;
            public UnityEngine.Object packageToImport;
            [PIPackageToImportVersion]
            public string packageToImportVersion;
            public PackageCondition[] packageConditions;

            public bool Validate(GPUIProPackageImporterData packageImporterData)
            {
                if (string.IsNullOrEmpty(packageName))
                {
                    Debug.LogError(GPUIConstants.LOG_PREFIX + "Package name is not provided!", packageImporterData);
                    return false;
                }
                if (packageToImport == null)
                {
                    Debug.LogError(GPUIConstants.LOG_PREFIX + "Package to import is not provided for " + packageName, packageImporterData);
                    return false;
                }
                if (string.IsNullOrEmpty(packageToImportVersion))
                {
                    Debug.LogError(GPUIConstants.LOG_PREFIX + "Package version is not provided for " + packageName, packageImporterData);
                    return false;
                }
                if (!Version.TryParse(packageToImportVersion, out _))
                {
                    Debug.LogError(GPUIConstants.LOG_PREFIX + "Package version is invalid for " + packageName, packageImporterData);
                    return false;
                }
                for (int i = 0; i < packageConditions.Length; i++)
                {
                    if (!packageConditions[i].Validate(packageImporterData))
                        return false;
                }
                return true;
            }
        }

        [Serializable]
        public struct PackageCondition
        {
            public PackageConditionType conditionType;
            [PIDependentPackageName]
            public string dependentPackageName;
            [PIDependentPackageExpression]
            public int dependentPackageExpression;
            [PIDependentPackageVersion]
            public string dependentPackageVersion;

            public bool Validate(GPUIProPackageImporterData packageImporterData)
            {
                if (string.IsNullOrEmpty(dependentPackageName))
                {
                    Debug.LogError(GPUIConstants.LOG_PREFIX + "Depended package is not provided for the condition.", packageImporterData);
                    return false;
                }
                if (conditionType == PackageConditionType.ScriptDefine)
                {
                    return true;
                }
                if (dependentPackageExpression < 3 && (string.IsNullOrEmpty(dependentPackageVersion) || !Version.TryParse(dependentPackageVersion, out _)))
                {
                    Debug.LogError(GPUIConstants.LOG_PREFIX + "Depended package version is invalid for the condition.", packageImporterData);
                    return false;
                }
                return true;
            }
        }

        public enum PackageConditionType
        {
            UnityPackage = 0,
            ScriptDefine = 1
        }

        [Serializable]
        public struct ImportedPackageInfo
        {
            public string packageURL;
            public string importedVersion;
        }

        public class PIDependentPackageNameAttribute : PropertyAttribute { }
        public class PIDependentPackageExpressionAttribute : PropertyAttribute { }
        public class PIDependentPackageVersionAttribute : PropertyAttribute { }
        public class PIPackageToImportVersionAttribute : PropertyAttribute { }
    }
}