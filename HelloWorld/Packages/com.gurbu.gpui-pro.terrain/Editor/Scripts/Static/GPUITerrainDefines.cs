// GPU Instancer Pro
// Copyright (c) GurBu Technologies

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace GPUInstancerPro.TerrainModule
{
    [InitializeOnLoad]
    public static class GPUITerrainDefines
    {
        private static readonly string PACKAGE_NAME = "com.gurbu.gpui-pro.terrain";
        private static readonly string[] AUTO_PACKAGE_IMPORTER_GUIDS = { "26531a4416285454588b14c6ee100321" };

        static GPUITerrainDefines()
        {
            GPUIDefines.OnPackageVersionChanged -= OnVersionChanged;
            GPUIDefines.OnPackageVersionChanged += OnVersionChanged;
            GPUIDefines.OnImportPackages -= ImportPackages;
            GPUIDefines.OnImportPackages += ImportPackages;

            // Delayed to wait for asset loading
            EditorApplication.delayCall -= DelayedInitialization;
            EditorApplication.delayCall += DelayedInitialization;
        }

        static void DelayedInitialization()
        {
            if (!GPUIEditorSettings.Instance.IsSubModuleExists(PACKAGE_NAME))
                GPUIEditorSettings.Instance.RequirePackageReload();

            GPUITerrainConstants.CheckForComputeCompilerErrors();
        }

        private static void OnVersionChanged(string packageName)
        {
            if (packageName == PACKAGE_NAME && GPUIEditorSettings.Instance.IsSubModuleVersionChanged(PACKAGE_NAME, out string currentVersion, out string processedVersion))
            {
#if GPUIPRO_DEVMODE
                Debug.Log(GPUIConstants.LOG_PREFIX + PACKAGE_NAME + " version changed from: " + processedVersion + " to: " + currentVersion);
#endif
                GPUIEditorSettings.Instance.SetSubModuleProcessedVersion(packageName, currentVersion);

                ImportPackages(false);
            }
        }

        public static void ImportPackages(bool forceReimport)
        {
            // Load default assets
            _ = GPUITerrainConstants.DefaultDetailMesh;
            _ = GPUITerrainConstants.DefaultDetailMaterial;
            _ = GPUITerrainConstants.DefaultDetailMaterialDescription;

            GPUIProPackageImporter.ImportPackages(AUTO_PACKAGE_IMPORTER_GUIDS, forceReimport);
            GPUITerrainConstants.ReimportComputeShaders();
        }
    }
}
