// GPU Instancer Pro
// Copyright (c) GurBu Technologies

using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace GPUInstancerPro
{
    [InitializeOnLoad]
    public static class GPUIDefines
    {
        public static readonly uint GPUI_PRO_BUILD_NO = 33;
        public static Version DEMO_UPDATE_VERSION = Version.Parse("0.10.4"); // The package version where the demo scenes were last updated and needs to be reimported.
        public static Version REGENERATE_SHADERS_VERSION = Version.Parse("0.10.3"); // The package version where the automatic shader converter has been modified and the shaders will be regenerated.
        public static readonly string PACKAGE_NAME = "com.gurbu.gpui-pro";
        private static readonly string[] AUTO_PACKAGE_IMPORTER_GUIDS = { "aefcba3f5637c0a419117e2bbe53b7df" };
        public static readonly string INITIAL_PACKAGE_PATH = "Packages/com.gurbu.gpui-pro/Editor/InitialPackage.unitypackage";
        public static readonly string INITIAL_PACKAGE_URP_PATH = "Packages/com.gurbu.gpui-pro/Editor/InitialPackageURP.unitypackage";
        public static readonly string INITIAL_PACKAGE_HDRP_PATH = "Packages/com.gurbu.gpui-pro/Editor/InitialPackageHDRP.unitypackage";

        public static readonly string SHADERS_BUILTIN_PACKAGE_PATH = "Packages/com.gurbu.gpui-pro/Editor/Extras/Shaders-Builtin.unitypackage";
        public static readonly string SHADERS_SHADERGRAPH_PACKAGE_PATH = "Packages/com.gurbu.gpui-pro/Editor/Extras/Shaders-ShaderGraph.unitypackage";
        public static readonly List<string> REMOVED_FILES = new List<string>() {
            "Packages/com.gurbu.gpui-pro/Runtime/Shaders/Billboard/Billboard2DRendererSoftOcclusion_GPUI.shader",
            "Packages/com.gurbu.gpui-pro/Runtime/Shaders/Billboard/Billboard2DRendererTree_GPUI.shader",
            "Packages/com.gurbu.gpui-pro/Runtime/Shaders/Billboard/Billboard2DRendererTreeCreator_GPUI.shader",
            "Packages/com.gurbu.gpui-pro/Runtime/Shaders/Billboard/BillboardBuiltin_GPUIPro.shader",
            "Packages/com.gurbu.gpui-pro/Runtime/Shaders/Billboard/BillboardHDRP_GPUIPro.shadergraph",
            "Packages/com.gurbu.gpui-pro/Runtime/Shaders/Billboard/BillboardURP_GPUIPro.shadergraph",
            "Packages/com.gurbu.gpui-pro/Runtime/Shaders/Nature", // folder
            "Packages/com.gurbu.gpui-pro/Runtime/Shaders/Foliage_GPUIPro.shader",
            "Packages/com.gurbu.gpui-pro/Runtime/Shaders/FoliageLambert_GPUIPro.shader",
            "Packages/com.gurbu.gpui-pro/Runtime/Shaders/Foliage_SG.shadergraph",
            "Packages/com.gurbu.gpui-pro/Runtime/Shaders/Standard_GPUIPro.shader",
            "Packages/com.gurbu.gpui-pro/Runtime/Shaders/StandardSpecular_GPUIPro.shader",
            "Packages/com.gurbu.gpui-pro/Runtime/Shaders/Internal-DepthNormalsTexture_GPUIPro.shader",
            "Packages/com.gurbu.gpui-pro/Runtime/Shaders/Normal-VertexLit_GPUIPro.shader",
        };

        public static event Action<string> OnPackageVersionChanged;
        public static event Action<bool> OnImportPackages;
        public static event Action OnImportDemos;

        private static bool _executeOnImportDemosAfterPackageImport = false;

        private static UnityEditor.PackageManager.Requests.ListRequest _packageListRequest;

        static GPUIDefines()
        {
            GPUIRenderingSystem.editor_UpdateMethod = OnEditorUpdate;
            // Delayed to wait for asset loading
            EditorApplication.delayCall -= DelayedInitialization;
            EditorApplication.delayCall += DelayedInitialization;
        }

        static void DelayedInitialization()
        {
            string locatorPath = AssetDatabase.GUIDToAssetPath(GPUIConstants.PATH_LOCATOR_GUID);
            if (string.IsNullOrEmpty(locatorPath) || AssetDatabase.LoadAssetAtPath<GPUIPathLocator>(locatorPath) == null)
                ImportInitialPackages();

            GPUIEditorSettings editorSettings = GPUIEditorSettings.Instance;

            if (!editorSettings.HasValidVersion() || editorSettings._requirePackageReload || GPUI_PRO_BUILD_NO != editorSettings.GetBuildNo())
            {
                LoadPackageDefinitions();
                editorSettings.SetBuildNo(GPUI_PRO_BUILD_NO);
            }
            UnityEditor.PackageManager.Events.registeredPackages -= OnRegisteredPackages;
            UnityEditor.PackageManager.Events.registeredPackages += OnRegisteredPackages;

            EditorApplication.update -= OnEditorUpdate;
            EditorApplication.update += OnEditorUpdate;

            GPUIShaderUtility.AddDefaultShaderVariants();
            GPUIShaderUtility.CheckForShaderModifications();

            editorSettings.UpdateShaderBindingValues();
        }

        public static void ImportInitialPackages()
        {
#if GPUIPRO_DEVMODE
            Debug.Log(GPUIConstants.LOG_PREFIX + "Importing initial packages...");
#endif
            GPUIEditorSettings.Instance.ImportPackageAtPath(INITIAL_PACKAGE_PATH);
            if (GPUIRuntimeSettings.Instance.IsURP)
                GPUIEditorSettings.Instance.ImportPackageAtPath(INITIAL_PACKAGE_URP_PATH);
            else if (GPUIRuntimeSettings.Instance.IsHDRP)
                GPUIEditorSettings.Instance.ImportPackageAtPath(INITIAL_PACKAGE_HDRP_PATH);
        }

        private static void OnRegisteredPackages(UnityEditor.PackageManager.PackageRegistrationEventArgs obj)
        {
            LoadPackageDefinitions();
        }

        private static void LoadPackageDefinitions()
        {
#if GPUIPRO_DEVMODE
            Debug.Log(GPUIConstants.LOG_PREFIX + "GPUIDefines Loading Package Definitions...");
#endif
            GPUIEditorSettings.Instance._requirePackageReload = false;
            EditorUtility.SetDirty(GPUIEditorSettings.Instance);
            _packageListRequest = UnityEditor.PackageManager.Client.List(true);
            EditorApplication.update -= PackageListRequestHandler;
            EditorApplication.update += PackageListRequestHandler;
        }

        private static void PackageListRequestHandler()
        {
            bool isVersionChanged = false;
            string previousVersion = GPUIEditorSettings.Instance.GetVersion();
            try
            {
                if (_packageListRequest != null)
                {
                    if (!_packageListRequest.IsCompleted)
                        return;
                    if (_packageListRequest.Result != null)
                    {
                        foreach (var packageInfo in _packageListRequest.Result)
                        {
                            if (packageInfo.name.Equals(PACKAGE_NAME))
                            {
                                if (GPUIEditorSettings.Instance.SetVersion(packageInfo.version))
                                {
#if GPUIPRO_DEVMODE
                                    Debug.Log(PACKAGE_NAME + " version changed to " + packageInfo.version);
#endif
                                    isVersionChanged = true;
                                }
                            }
                            else if (packageInfo.name.StartsWith(PACKAGE_NAME) && !packageInfo.name.EndsWith(".tests"))
                            {
                                if (GPUIEditorSettings.Instance.SetSubModuleVersion(packageInfo.name, packageInfo.version))
                                {
#if GPUIPRO_DEVMODE
                                    Debug.Log(packageInfo.name + " version changed to " + packageInfo.version);
#endif
                                    OnPackageVersionChanged?.Invoke(packageInfo.name);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }

            _packageListRequest = null;
            EditorApplication.update -= PackageListRequestHandler;

            if (isVersionChanged)
                DoVersionUpdate(previousVersion);
        }

        private static void DoVersionUpdate(string previousVersionText)
        {
            if (Version.TryParse(previousVersionText, out Version previousVersion) && Version.TryParse(GPUIEditorSettings.Instance.GetVersion(), out Version currentVersion))
            {
                GPUIRuntimeSettings.Instance.DetermineRenderPipeline();

                #region Reimport demo scenes
                if (GPUIProDemoImporter.IsDemosImported() && previousVersion.CompareTo(DEMO_UPDATE_VERSION) < 0 && currentVersion.CompareTo(DEMO_UPDATE_VERSION) >= 0)
                {
                    Debug.Log(GPUIConstants.LOG_PREFIX + "Importing new demo scenes for version " + GPUIEditorSettings.Instance.GetVersion() + "...");
                    GPUIProDemoImporter.ImportDemos(GPUIRuntimeSettings.Instance.RenderPipeline);
                }
                #endregion Reimport demo scenes

                #region Regenerate shaders
                if (previousVersion.CompareTo(REGENERATE_SHADERS_VERSION) < 0 && currentVersion.CompareTo(REGENERATE_SHADERS_VERSION) >= 0)
                {
                    if (GPUIShaderBindings.Instance.shaderInstances != null && GPUIShaderBindings.Instance.shaderInstances.Count > 0)
                    {
                        Debug.Log(GPUIConstants.LOG_PREFIX + "Regenerating shaders for version " + GPUIEditorSettings.Instance.GetVersion() + "...");
                        GPUIShaderUtility.RegenerateShaders();
                    }
                }
                #endregion Regenerate shaders

                #region Remove deprecated files
                bool anyFileDeleted = false;

                foreach (string path in REMOVED_FILES)
                {
                    // Ensure cross-platform path compatibility
                    string fullPath = Path.GetFullPath(path);

                    if (Directory.Exists(fullPath))
                    {
                        try
                        {
                            Directory.Delete(fullPath, true);
                            string metaFile = fullPath + ".meta";
                            if (File.Exists(metaFile))
                                File.Delete(metaFile);
                            Debug.Log(GPUIConstants.LOG_PREFIX + "Deleted deprecated folder: " + fullPath);
                            anyFileDeleted = true;
                        }
                        catch (Exception e)
                        {
                            Debug.LogWarning(GPUIConstants.LOG_PREFIX + "Failed to delete deprecated folder: " + fullPath + "\n" + e.Message);
                        }
                    }
                    else if (File.Exists(fullPath))
                    {
                        try
                        {
                            File.Delete(fullPath);
                            string metaFile = fullPath + ".meta";
                            if (File.Exists(metaFile))
                                File.Delete(metaFile);
                            Debug.Log(GPUIConstants.LOG_PREFIX + "Deleted deprecated file: " + fullPath);
                            anyFileDeleted = true;
                        }
                        catch (Exception e)
                        {
                            Debug.LogWarning(GPUIConstants.LOG_PREFIX + "Failed to delete deprecated file: " + fullPath + "\n" + e.Message);
                        }
                    }
                }

                // Force Unity to refresh the asset database if anything was deleted
                if (anyFileDeleted)
                {
                    AssetDatabase.Refresh();
                }
                #endregion Remove deprecated files
            }

            ImportPackages(false);
        }


        public static void ImportPackages(bool forceReimport)
        {
            #region Import Shader files for render pipeline
            if (GPUIRuntimeSettings.Instance.IsBuiltInRP)
            {
                GPUIEditorSettings.Instance.ImportPackageAtPath(SHADERS_BUILTIN_PACKAGE_PATH);
                Debug.Log(GPUIConstants.LOG_PREFIX + "Importing builtin shaders for version " + GPUIEditorSettings.Instance.GetVersion() + "...");
            }
            else
            {
                GPUIEditorSettings.Instance.ImportPackageAtPath(SHADERS_SHADERGRAPH_PACKAGE_PATH);
                Debug.Log(GPUIConstants.LOG_PREFIX + "Importing Shader Graph shaders for version " + GPUIEditorSettings.Instance.GetVersion() + "...");
            }
            #endregion Import Shader files for render pipeline

            GPUIProPackageImporter.ImportPackages(AUTO_PACKAGE_IMPORTER_GUIDS, forceReimport);
            GPUIConstants.ReimportComputeShaders();
            OnImportPackages?.Invoke(forceReimport);
        }

        public static void OnDemosImported()
        {
            _executeOnImportDemosAfterPackageImport = true;
        }

        #region Editor Update

        private static void OnEditorUpdate()
        {
            GPUIEditorSettings editorSettings = GPUIEditorSettings.Instance;
            if (GPUIRenderingSystem.IsActive)
            {
                if (editorSettings.isAutoShaderConversion)
                    GPUIShaderUtility.AutoShaderConverterUpdate();
            }

            if (!Application.isPlaying)
            {
                if (editorSettings.packageImportList != null && editorSettings.packageImportList.Count > 0)
                {
                    string packagePath = editorSettings.packageImportList[0];
                    editorSettings.packageImportList.RemoveAt(0);
                    EditorUtility.SetDirty(editorSettings);
                    if (System.IO.File.Exists(packagePath))
                        AssetDatabase.ImportPackage(packagePath, false);
#if GPUIPRO_DEVMODE
                    else
                        Debug.LogWarning(GPUIConstants.LOG_PREFIX + "Can not find package at path: " + packagePath);
#endif

                    if (_executeOnImportDemosAfterPackageImport && editorSettings.packageImportList.Count == 0)
                    {
                        _executeOnImportDemosAfterPackageImport = false;
                        OnImportDemos?.Invoke();
                    }
                }
                if (editorSettings._requirePackageReload)
                    DelayedInitialization();
            }
        }

        #endregion Editor Update
    }
}
