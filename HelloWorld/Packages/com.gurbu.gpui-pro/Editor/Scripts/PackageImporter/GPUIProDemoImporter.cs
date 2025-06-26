// GPU Instancer Pro
// Copyright (c) GurBu Technologies

using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace GPUInstancerPro
{
    //[CreateAssetMenu(menuName = "GPU Instancer Pro/Demo Importer")]
    [HelpURL("https://wiki.gurbu.com/index.php?title=GPU_Instancer_Pro:GettingStarted")]
    public class GPUIProDemoImporter : ScriptableObject
    {
        [SerializeField]
        public GPUIRenderPipeline demoImporterRenderPipeline;
        // "Packages/com.gurbu.gpui-pro/"
        public static readonly string PACKAGES_PATH = "Packages";
        public static readonly string DEMOS_PATH = "Demos";
        public static readonly string BASE_DEMOS_PATH = "DemosBase";
        public static readonly string URP_OVERWRITE_PATH = "DemosURPOverwrite";
        public static readonly string HDRP_OVERWRITE_PATH = "DemosHDRPOverwrite";

        private void OnEnable()
        {
            GPUIRuntimeSettings.Instance.DetermineRenderPipeline();
            demoImporterRenderPipeline = GPUIRuntimeSettings.Instance.RenderPipeline;
        }

        //[ContextMenu("Import Demos")]
        public void ImportDemos()
        {
            ImportDemos(demoImporterRenderPipeline);
        }

        public static void ImportDemos(GPUIRenderPipeline renderPipeline)
        {
            ImportDemos(GPUIDefines.PACKAGE_NAME, renderPipeline);

            if (GPUIEditorSettings.Instance._subModuleVersions == null)
                return;
            foreach (var subModule in GPUIEditorSettings.Instance._subModuleVersions)
            {
                if (!string.IsNullOrEmpty(subModule.packageName))
                    ImportDemos(subModule.packageName, renderPipeline);
            }
        }

        private static void ImportDemos(string packageName, GPUIRenderPipeline renderPipeline)
        {
            GPUIEditorSettings.Instance.ImportPackageAtPath(PACKAGES_PATH + "/" + packageName + "/" + DEMOS_PATH + "/" + BASE_DEMOS_PATH + "/" + BASE_DEMOS_PATH + ".unitypackage");

            if (renderPipeline == GPUIRenderPipeline.URP)
                GPUIEditorSettings.Instance.ImportPackageAtPath(PACKAGES_PATH + "/" + packageName + "/" + DEMOS_PATH + "/" + URP_OVERWRITE_PATH + "/" + URP_OVERWRITE_PATH + ".unitypackage");
            else if (renderPipeline == GPUIRenderPipeline.HDRP)
                GPUIEditorSettings.Instance.ImportPackageAtPath(PACKAGES_PATH + "/" + packageName + "/" + DEMOS_PATH + "/" + HDRP_OVERWRITE_PATH + "/" + HDRP_OVERWRITE_PATH + ".unitypackage");

            GPUIDefines.OnDemosImported();
        }

        public static bool IsDemosImported()
        {
            return Directory.Exists(GPUIConstants.GetDefaultPath() + "Demos/Core");
        }

        public static bool IsDemosImportedForPackage(string packageDemoFolderName)
        {
            return Directory.Exists(GPUIConstants.GetDefaultPath() + "Demos/" + packageDemoFolderName);
        }
    }

    [CustomEditor(typeof(GPUIProDemoImporter))]
    public class GPUIProDemoImporterEditor : GPUIEditor
    {
        private GPUIProDemoImporter _demoImporter;

        private bool _packagesFoldoutValue;

        protected override void OnEnable()
        {
            base.OnEnable();

            _demoImporter = target as GPUIProDemoImporter;
        }

        public override void DrawIMGUIContainer()
        {
            DrawIMGUISerializedProperty(serializedObject.FindProperty("demoImporterRenderPipeline"));

#if GPUIPRO_DEVMODE
            _packagesFoldoutValue = EditorGUILayout.Foldout(_packagesFoldoutValue, "Packages", true);
            if (_packagesFoldoutValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.LabelField(GPUIDefines.PACKAGE_NAME);
                if (GPUIEditorSettings.Instance._subModuleVersions != null)
                {
                    foreach (var subModule in GPUIEditorSettings.Instance._subModuleVersions)
                    {
                        if (!string.IsNullOrEmpty(subModule.packageName))
                            EditorGUILayout.LabelField(subModule.packageName);
                    }
                }
                EditorGUI.indentLevel--;
            }
#endif
            GPUIEditorTextUtility.TryGetGPUIText("importDemosButton", out var gpuiText);
            if (GPUIEditorSettings.Instance.packageImportList == null || GPUIEditorSettings.Instance.packageImportList.Count == 0)
            {
                EditorGUILayout.Space(10);
                Rect rect = EditorGUILayout.GetControlRect();
                rect.height += 10;
                GPUIEditorUtility.DrawColoredButton(new GUIContent(gpuiText.title), GPUIEditorConstants.Colors.lightBlue, Color.white, FontStyle.Bold, rect, _demoImporter.ImportDemos);
                EditorGUILayout.Space(20);
                GPUIEditorUtility.DrawIMGUIHelpText(gpuiText.helpText);

                EditorGUILayout.Space(10);
            }
            else
            {
                EditorGUILayout.Space(10);
                GUIStyle guiStyle = new GUIStyle("label");
                guiStyle.fontSize = 16;
                EditorGUILayout.LabelField("Importing files. Please wait...", guiStyle);
                EditorGUILayout.Space(15);
            }
        }

        public override string GetTitleText()
        {
            return "Import Demos";
        }
    }
}
