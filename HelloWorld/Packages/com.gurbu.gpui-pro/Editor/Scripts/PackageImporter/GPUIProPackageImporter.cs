// GPU Instancer Pro
// Copyright (c) GurBu Technologies

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.PackageManager.Requests;
using UnityEngine;
using static GPUInstancerPro.GPUIProPackageImporterData;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;

namespace GPUInstancerPro
{
    public static class GPUIProPackageImporter
    {
        private static List<PackageInfo> _installedPackageInfos;
        private static string[] _scriptDefines;

        private static ListRequest _packageListRequest;
        private static bool _importPackagesAfterLoad;
        private static Queue<GPUIProPackageImporterData> _packageImporterDataQueue;
        public static bool _isPackagesLoading;

        public static void ImportPackages(string[] guids, bool forceReimport)
        {
            if (guids != null)
            {
                foreach (string guid in guids)
                {
                    if (!string.IsNullOrEmpty(guid))
                    {
                        string path = AssetDatabase.GUIDToAssetPath(guid);
                        if (!string.IsNullOrEmpty(path))
                        {
                            GPUIProPackageImporterData packageImporterData = AssetDatabase.LoadAssetAtPath<GPUIProPackageImporterData>(path);
                            if (packageImporterData != null)
                            {
                                packageImporterData.forceReimport = forceReimport;
                                ImportPackages(packageImporterData);
                            }
                        }
                    }
                }
            }
        }

        private static void InitializeData()
        {
            if (_scriptDefines == null)
                _scriptDefines = PlayerSettings.GetScriptingDefineSymbols(UnityEditor.Build.NamedBuildTarget.FromBuildTargetGroup(EditorUserBuildSettings.selectedBuildTargetGroup)).Split(';');

            if (_installedPackageInfos == null || _installedPackageInfos.Count == 0)
            {
                LoadInstalledPackages();
            }
        }

        private static void LoadInstalledPackages()
        {
            if (_isPackagesLoading)
                return;
            _isPackagesLoading = true;
            _packageListRequest = UnityEditor.PackageManager.Client.List(true, true);
            EditorApplication.update -= PackageListRequestHandler;
            EditorApplication.update += PackageListRequestHandler;
        }

        private static void PackageListRequestHandler()
        {
            try
            {
                if (_packageListRequest != null)
                {
                    if (!_packageListRequest.IsCompleted)
                        return;
                    if (_packageListRequest.Result != null)
                    {
                        _installedPackageInfos = new(_packageListRequest.Result);
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError(GPUIConstants.LOG_PREFIX + "Could not load package list. " + e.Message);
                _importPackagesAfterLoad = false;
            }
            _isPackagesLoading = false;
            EditorApplication.update -= PackageListRequestHandler;
            if (_importPackagesAfterLoad && _packageImporterDataQueue != null)
            {
                while (_packageImporterDataQueue.Count > 0)
                {
                    ImportPackages(_packageImporterDataQueue.Dequeue());
                }
            }
        }

        public static bool ImportPackages(GPUIProPackageImporterData packageImporterData)
        {
            if (packageImporterData == null || !packageImporterData.Validate())
                return false;

            if (_scriptDefines == null || _installedPackageInfos == null || _installedPackageInfos.Count == 0)
            {
                if (_packageImporterDataQueue == null)
                    _packageImporterDataQueue = new Queue<GPUIProPackageImporterData>();
                if (!_packageImporterDataQueue.Contains(packageImporterData))
                {
                    _importPackagesAfterLoad = true;
                    _packageImporterDataQueue.Enqueue(packageImporterData);
                    InitializeData();
                }
                return false;
            }

            int numPackagesImported = 0;
            // Do the imports
            for (int d = 0; d < packageImporterData.packageDefinitions.Length; d++)
            {
                PackageDefinition packageDefinition = packageImporterData.packageDefinitions[d];
                string packageURL = packageImporterData.domain + "." + packageDefinition.packageName;

                if (!packageImporterData.forceReimport)
                {
                    ImportedPackageInfo importedPackageInfo = packageImporterData.GetImportedPackageInfos().Find(pi => pi.packageURL.Equals(packageURL));
                    if (!string.IsNullOrEmpty(importedPackageInfo.importedVersion) && CheckVersion(0, packageDefinition.packageToImportVersion, importedPackageInfo.importedVersion))
                        continue;
                }

                bool isSuccessfull = true;
                for (int c = 0; c < packageDefinition.packageConditions.Length; c++)
                {
                    PackageCondition packageCondition = packageDefinition.packageConditions[c];

                    if (packageCondition.conditionType == PackageConditionType.ScriptDefine)
                    {
                        if (!_scriptDefines.Contains(packageCondition.dependentPackageName))
                        {
                            isSuccessfull = false;
                            break;
                        }
                        continue;
                    }

                    PackageInfo installedPackageInfo = _installedPackageInfos.Find(pi => pi.name.Equals(packageCondition.dependentPackageName));

                    if (installedPackageInfo == null)
                    {
                        if (packageCondition.dependentPackageExpression == 4)
                            continue;
                        else
                        {
                            isSuccessfull = false;
                            break;
                        }
                    }

                    if (packageCondition.dependentPackageExpression == 3)
                        continue;

                    if (!CheckVersion(packageCondition.dependentPackageExpression, packageCondition.dependentPackageVersion, installedPackageInfo.version))
                    {
                        isSuccessfull = false;
                        break;
                    }
                }

                if (isSuccessfull)
                {
                    Debug.Log(GPUIConstants.LOG_PREFIX + "Importing package: " + packageDefinition.packageName + " v"+ packageDefinition.packageToImportVersion, packageDefinition.packageToImport);
                    AssetDatabase.ImportPackage(AssetDatabase.GetAssetPath(packageDefinition.packageToImport), false);
                    packageImporterData.GetImportedPackageInfos().RemoveAll(ip => ip.packageURL.Equals(packageURL));
                    packageImporterData.GetImportedPackageInfos().Add(new ImportedPackageInfo
                    {
                        packageURL = packageURL,
                        importedVersion = packageDefinition.packageToImportVersion
                    });

                    numPackagesImported++;
                }
            }

            packageImporterData.forceReimport = false;
            if (numPackagesImported > 0)
            {
                EditorUtility.SetDirty(packageImporterData);
                packageImporterData.SaveImportedPackageInfos();
                Debug.Log(GPUIConstants.LOG_PREFIX + "Package import completed for domain: " + packageImporterData.domain + ". " + numPackagesImported + " new packages imported.", packageImporterData);
            }
            return true;
        }

        private static bool CheckVersion(int expressionIndex, string versionText, string installedPackageVersionText)
        {
            if (Version.TryParse(installedPackageVersionText.Split('-')[0], out Version installedVersion) && Version.TryParse(versionText, out Version expressionVersion))
            {
                int comparison = installedVersion.CompareTo(expressionVersion);

                switch (expressionIndex)
                {
                    case 0:
                        return comparison != -1;
                    case 1:
                        return comparison == -1;
                    case 2:
                    default:
                        return comparison == 0;
                }
            }
            return false;
        }
    }
}
