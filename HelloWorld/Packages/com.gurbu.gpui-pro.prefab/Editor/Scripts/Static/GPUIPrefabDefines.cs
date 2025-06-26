// GPU Instancer Pro
// Copyright (c) GurBu Technologies

using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace GPUInstancerPro.PrefabModule
{
    [InitializeOnLoad]
    public static class GPUIPrefabDefines
    {
        private static readonly string PACKAGE_NAME = "com.gurbu.gpui-pro.prefab";
        private static readonly string[] AUTO_PACKAGE_IMPORTER_GUIDS = { };

        static GPUIPrefabDefines()
        {
            GPUIDefines.OnPackageVersionChanged -= OnVersionChanged;
            GPUIDefines.OnPackageVersionChanged += OnVersionChanged;
            GPUIDefines.OnImportPackages -= ImportPackages;
            GPUIDefines.OnImportPackages += ImportPackages;
            GPUIDefines.OnImportDemos -= OnDemosImported;
            GPUIDefines.OnImportDemos += OnDemosImported;

            // Delayed to wait for asset loading
            EditorApplication.delayCall -= DelayedInitialization;
            EditorApplication.delayCall += DelayedInitialization;
        }

        static void DelayedInitialization()
        {
            if (!GPUIEditorSettings.Instance.IsSubModuleExists(PACKAGE_NAME))
                GPUIEditorSettings.Instance.RequirePackageReload();
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
            GPUIProPackageImporter.ImportPackages(AUTO_PACKAGE_IMPORTER_GUIDS, forceReimport);
        }

        private static void OnDemosImported()
        {
            if (GPUIRuntimeSettings.Instance.IsBuiltInRP)
                return;
            AssetDatabase.Refresh();
            string assetPath = GPUIConstants.GetDefaultPath() + GPUIProDemoImporter.DEMOS_PATH + "/Prefab/_SharedResources/SRPDependent/MaterialVariations/TreePineLP_GPUIVariationDefinition.asset";
            GPUIMaterialVariationDefinition mvDef = AssetDatabase.LoadAssetAtPath<GPUIMaterialVariationDefinition>(assetPath);
            if (mvDef != null)
                GPUIMaterialVariationEditorUtility.GenerateShader(mvDef);
#if GPUIPRO_DEVMODE
            else
                Debug.LogWarning(GPUIConstants.LOG_PREFIX + "Cant find asset at path: " + assetPath);
#endif
        }
    }
}
