// GPU Instancer Pro
// Copyright (c) GurBu Technologies

using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;

namespace GPUInstancerPro
{
    public static class GPUIEditorConstants
    {
        public static readonly string TITLE = "GPU Instancer Pro";

        public static readonly string PATH_UI = "UI/";
        public static readonly string EDITOR_TEXT = "GPUIEditorText.txt";

        public static readonly float LABEL_WIDTH = 180;

        public static class Texts
        {
            public static readonly string showHelpTooltip = "Show Help";
            public static readonly string hideHelpTooltip = "Hide Help";
            public static readonly string isShaderVariantCollection = "Generate Shader Variant Collection";
            public static readonly string cancel = "Cancel";
            public static readonly string ok = "OK";
        }
        
        public static class HelpTexts
        {
            public static readonly string isShaderVariantCollection = "If enabled, a ShaderVariantCollection with reference to shaders that are used in GPUI Managers will be generated automatically inside Resources folder. This will add the GPUI shader variants automatically to your builds. These shader variants are required for GPUI to work in your builds.";
            public static readonly string prefabReplacerIntro = "The Prefab Replacer is designed to easily replace GameObjects in your scene hierarchy with prefab instances.";
            public static readonly string prefabReplacerReplaceCancel = "The \"Replace Selection With Prefab\" button replaces the selected GameObjects with the prefab's instances. The \"Cancel\" button closes this window.";
            public static readonly string prefabReplacerPrefab = "The \"Prefab\" will be used to create instances that will replace the selected GameObjects.";
            public static readonly string prefabReplacerReplaceNames = "If \"Replace Names\" option is enabled, new instances will use the prefab name. If disabled, instances will have the same names with the GameObjects that are being replaced.";
            public static readonly string prefabReplacerSelectedObjects = "The \"Selected GameObjects\" section shows the list of selected GameObjects that will be replaced with prefab instances.";
        }

        public static readonly string ERRORTEXT_shaderGraph = "ShaderGraph shader does not contain GPU Instancer Pro Setup: {0}. Please add GPU Instancer Pro Setup from the ShaderGraph window.";
        public static readonly string ERRORTEXT_shaderConversion = "Can not automatically add procedural GPU instancing setup to shader: {0}. If you are using a Unity built-in shader, please download the shader source code to your project from the Unity Archive.";
        public static readonly string ERRORTEXT_shaderConversionWithCrowd = "Can not automatically add procedural GPU instancing setup to shader: {0}. Crowd Animations extension requires shader modifications for GPU skinning. Please see the Docs for more information.";
        public static readonly string ERRORTEXT_surfshader = "Better Shaders surface shader does not contain GPU Instancer setup: {0}. Please create a stacked shader and add Stackable_GPUInstancer to the Shader List.";
        public static readonly string ERRORTEXT_stackedshader = "Better Shaders stacked shader does not contain GPU Instancer setup: {0}. Please add Stackable_GPUInstancer to the Shader List.";

        public static class Styles
        {
            public static GUIStyle label = new GUIStyle("Label");
            public static GUIStyle boldLabel = new GUIStyle("BoldLabel");
            public static GUIStyle button = new GUIStyle("Button");
            public static GUIStyle foldout = new GUIStyle("Foldout");
            public static GUIStyle box = new GUIStyle("Box");
            public static GUIStyle richLabel = new GUIStyle("Label");
            public static GUIStyle helpButton = new GUIStyle("Button")
            {
                padding = new RectOffset(2, 2, 2, 2)
            };
            public static GUIStyle helpButtonSelected = new GUIStyle("Button")
            {
                padding = new RectOffset(2, 2, 2, 2),
                normal = helpButton.active
            };
        }
        public static class Colors
        {
            public static Color lightBlue = new Color(0.5f, 0.6f, 0.8f, 1);
            public static Color darkBlue = new Color(0.07f, 0.27f, 0.35f, 1);
            public static Color lightGreen = new Color(0.1f, 0.8f, 0.1f, 1);
            public static Color green = new Color(61 / 255f, 104 / 255f, 58 / 255f);
            public static Color lightRed = new Color(0.8f, 0.2f, 0.2f, 1);
            public static Color darkRed = new Color(0.4f, 0, 0, 1);
            public static Color gainsboro = new Color(220 / 255f, 220 / 255f, 220 / 255f);
            public static Color dimgray = new Color(105 / 255f, 105 / 255f, 105 / 255f);
            public static Color darkyellow = new Color(153 / 255f, 153 / 255f, 0);
            public static Color blue = new Color(0.14f, 0.33f, 0.49f, 0.6f);
        }

        public static string GetUIPath()
        {
            return GPUIConstants.GetPackagesPath() + GPUIConstants.PATH_EDITOR + PATH_UI;
        }

        public static string GetEditorTextPath()
        {
            return GetUIPath() + EDITOR_TEXT;
        }

        #region Menu Items

        [MenuItem("Assets/Create/Rendering/GPU Instancer Pro/Setup Shaders for GPU Instancer", validate = false, priority = 2101)]
        public static void SetupShaderForGPUIMenuItem()
        {
            GPUIShaderBindings.Instance.ClearEmptyShaderInstances();
            Shader[] shaders = Selection.GetFiltered<Shader>(SelectionMode.Assets);
            if (shaders != null)
            {
                for (int i = 0; i < shaders.Length; i++)
                {
                    GPUIShaderUtility.SetupShaderForGPUI(shaders[i], null);
                }
            }

            Material[] materials = Selection.GetFiltered<Material>(SelectionMode.Assets);
            if (materials != null)
            {
                for (int i = 0; i < materials.Length; i++)
                {
                    Material mat = materials[i];
                    if (!GPUIShaderBindings.Instance.IsShaderSetupForGPUI(mat.shader.name, null))
                    {
                        GPUIShaderUtility.SetupShaderForGPUI(mat.shader, null);
                    }
                    if (GPUIShaderBindings.Instance.IsShaderSetupForGPUI(mat.shader.name, null))
                    {
                        GPUIShaderUtility.AddShaderVariantToCollection(mat, null);
                        Debug.Log(GPUIConstants.LOG_PREFIX + mat.name + " material has been successfully added to Shader Variant Collection.", mat);
                    }
                }
            }
        }

        [MenuItem("Assets/Create/Rendering/GPU Instancer Pro/Setup Shaders for GPU Instancer", validate = true, priority = 2101)]
        public static bool SetupShaderForGPUIMenuItemValidate()
        {
            Shader[] shaders = Selection.GetFiltered<Shader>(SelectionMode.Assets);
            Material[] materials = Selection.GetFiltered<Material>(SelectionMode.Assets);
            return (shaders != null && shaders.Length > 0) || (materials != null && materials.Length > 0);
        }

        [MenuItem("Tools/GPU Instancer Pro/Reimport Packages", validate = false, priority = 501)]
        public static void ReimportPackages()
        {
            GPUIDefines.ImportInitialPackages();
            GPUIDefines.ImportPackages(true);
        }

        [MenuItem("Tools/GPU Instancer Pro/Reimport Packages", validate = true, priority = 501)]
        public static bool ReimportPackagesValidate()
        {
            return !Application.isPlaying;
        }

        [MenuItem("Tools/GPU Instancer Pro/Import Demos", validate = false, priority = 502)]
        public static void ImportDemoPackages()
        {
            GPUIDefines.ImportInitialPackages();
            GPUIProDemoImporter.ImportDemos(GPUIRuntimeSettings.Instance.RenderPipeline);
        }

        [MenuItem("Tools/GPU Instancer Pro/Import Demos", validate = true, priority = 502)]
        public static bool ImportDemoPackagesValidate()
        {
            return !Application.isPlaying;
        }

        [MenuItem("Tools/GPU Instancer Pro/Utilities/Add Event System", validate = false, priority = 191)]
        public static GPUIEventSystem ToolbarAddEventSystem()
        {
            GameObject go = new GameObject("GPUI Event System");
            GPUIEventSystem eventSystem = go.AddComponent<GPUIEventSystem>();

            Selection.activeGameObject = go;
            Undo.RegisterCreatedObjectUndo(go, "Add GPUI Event System");

            return eventSystem;
        }

        [MenuItem("Tools/GPU Instancer Pro/Utilities/Add Runtime Settings Overwrite", validate = false, priority = 192)]
        public static GPUIRuntimeSettingsOverwrite ToolbarAddRuntimeSettingsOverwrite()
        {
            GameObject go = new GameObject("GPUI Runtime Settings");
            GPUIRuntimeSettingsOverwrite runtimeSettingsOverwrite = go.AddComponent<GPUIRuntimeSettingsOverwrite>();

            Selection.activeGameObject = go;
            Undo.RegisterCreatedObjectUndo(go, "Add Runtime Settings Overwrite");

            return runtimeSettingsOverwrite;
        }

        [MenuItem("Tools/GPU Instancer Pro/Utilities/Add Debugger Canvas", validate = false, priority = 193)]
        public static GameObject ToolbarAddDebuggerCanvas()
        {
            GameObject result = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(GPUIConstants.GetPackagesPath() + GPUIConstants.PATH_RUNTIME + GPUIConstants.PATH_PREFABS + GPUIConstants.FILE_DEBUGGER_CANVAS + ".prefab"));
            Undo.RegisterCreatedObjectUndo(result, "Add Debugger Canvas");
            result.name = "GPUI Debugger Canvas";
            if (Object.FindFirstObjectByType<EventSystem>() == null)
            {
                GameObject eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
                eventSystem.AddOrGetComponent<GPUIInputModuleHandler>();
            }
            return result;
        }

        [MenuItem("Tools/GPU Instancer Pro/Shaders/Clear Shader Variant Collection", validate = false, priority = 251)]
        public static void ToolbarClearShaderVariantCollection()
        {
            if (EditorUtility.DisplayDialog("Clear Shader Variant Collection", "Do you wish to clear all shader variants from the Shader Variant Collection?", "Yes", "No"))
                GPUIShaderUtility.ClearShaderVariantCollection();
        }

        [MenuItem("Tools/GPU Instancer Pro/Shaders/Regenerate Shaders", validate = false, priority = 252)]
        public static void ToolbarRegenerateShaders()
        {
            if (EditorUtility.DisplayDialog("Regenerate Shaders", "Do you wish to regenerate all shaders registered in the GPUIShaderBindings?", "Yes", "No"))
                GPUIShaderUtility.RegenerateShaders();
        }

        [MenuItem("Tools/GPU Instancer Pro/Shaders/Setup Selected Materials for GPUI Pro", validate = false, priority = 253)]
        public static void ToolbarSetupShaderForGPUIPro()
        {
            SetupShaderForGPUIMenuItem();
        }

        [MenuItem("Tools/GPU Instancer Pro/Shaders/Setup Selected Materials for GPUI Pro", validate = true, priority = 253)]
        public static bool ToolbarSetupShaderForGPUIProValidate()
        {
            return SetupShaderForGPUIMenuItemValidate();
        }

        [MenuItem("Tools/GPU Instancer Pro/Shaders/Replace Selected Material Shaders", validate = false, priority = 254)]
        public static void ToolbarSwitchShaderToGPUIPro()
        {
            SetupShaderForGPUIMenuItem();
            Material[] materials = Selection.GetFiltered<Material>(SelectionMode.Editable);
            if (materials != null)
            {
                for (int i = 0; i < materials.Length; i++)
                {
                    Material mat = materials[i];
                    if (GPUIShaderBindings.Instance.GetInstancedShader(mat.shader, null, out Shader instancedShader))
                    {
                        mat.shader = instancedShader;
                        EditorUtility.SetDirty(mat);
                    }
                }
            }
        }

        [MenuItem("Tools/GPU Instancer Pro/Shaders/Replace Selected Material Shaders", validate = true, priority = 254)]
        public static bool ToolbarSwitchShaderToGPUIProValidate()
        {
            Material[] materials = Selection.GetFiltered<Material>(SelectionMode.Editable);
            return (materials != null && materials.Length > 0);
        }

        [MenuItem("Tools/GPU Instancer Pro/Help/Getting Started", validate = false, priority = 801)]
        public static void ToolbarOpenGettingStarted()
        {
            Application.OpenURL("https://wiki.gurbu.com/index.php?title=GPU_Instancer_Pro:GettingStarted");
        }

        [MenuItem("Tools/GPU Instancer Pro/Help/Best Practices", validate = false, priority = 802)]
        public static void ToolbarOpenBestPractices()
        {
            Application.OpenURL("https://wiki.gurbu.com/index.php?title=GPU_Instancer_Pro:BestPractices");
        }

        [MenuItem("Tools/GPU Instancer Pro/Help/F.A.Q.", validate = false, priority = 803)]
        public static void ToolbarOpenFAQ()
        {
            Application.OpenURL("https://wiki.gurbu.com/index.php?title=GPU_Instancer_Pro:FAQ");
        }

        //[MenuItem("Tools/GPU Instancer Pro/Help/Change Log", validate = false, priority = 811)]
        //public static void ToolbarOpenChangeLog()
        //{
        //    Application.OpenURL("https://gurbu.com/changelog");
        //}

        [MenuItem("Tools/GPU Instancer Pro/Help/Support Request", validate = false, priority = 902)]
        public static void ToolbarRequestSupport()
        {
            Application.OpenURL("https://wiki.gurbu.com/index.php?title=GPU_Instancer_Pro:Support");
        }

#if GPUI_ADDRESSABLES
        public static UnityEditor.AddressableAssets.Settings.AddressableAssetEntry CreateAssetEntry<T>(T source, string address) where T : Object
        {
            if (source == null || !AssetDatabase.Contains(source))
                return null;

            var addressableSettings = UnityEditor.AddressableAssets.AddressableAssetSettingsDefaultObject.Settings;
            var sourcePath = AssetDatabase.GetAssetPath(source);
            var sourceGuid = AssetDatabase.AssetPathToGUID(sourcePath);
            var entry = addressableSettings.CreateOrMoveEntry(sourceGuid, addressableSettings.DefaultGroup);
            entry.address = address;

            addressableSettings.SetDirty(UnityEditor.AddressableAssets.Settings.AddressableAssetSettings.ModificationEvent.EntryMoved, entry, true);

            return entry;
        }

        [MenuItem("Assets/Create/Rendering/GPU Instancer Pro/Mark Shader Addressable", validate = false, priority = 2111)]
        public static void MarkShaderAddressableMenuItem()
        {
            Shader[] shaders = Selection.GetFiltered<Shader>(SelectionMode.Assets);
            if (shaders != null)
            {
                for (int i = 0; i < shaders.Length; i++)
                {
                    Shader shader = shaders[i];
                    if (shader != null)
                        CreateAssetEntry(shader, shader.name);
                }
            }
        }

        [MenuItem("Assets/Create/Rendering/GPU Instancer Pro/Mark Shader Addressable", validate = true, priority = 2111)]
        public static bool MarkShaderAddressableMenuItemValidate()
        {
            Shader[] shaders = Selection.GetFiltered<Shader>(SelectionMode.Assets);
            return (shaders != null && shaders.Length > 0);
        }
#endif

        #endregion Menu Items
    }
}