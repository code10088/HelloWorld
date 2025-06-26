// GPU Instancer Pro
// Copyright (c) GurBu Technologies

using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace GPUInstancerPro.PrefabModule
{
    public class GPUIPrefabReplacerWindow : GPUIEditorWindow
    {
        private GameObject selectedPrefab;
        private bool replaceNames = true;

        private Vector2 scrollPos = Vector2.zero;

        [MenuItem("Tools/GPU Instancer Pro/Utilities/Show Prefab Replacer", validate = false, priority = 521)]
        public static GPUIPrefabReplacerWindow ShowWindow()
        {
            GPUIPrefabReplacerWindow window = GetWindow<GPUIPrefabReplacerWindow>(false, "GPUI Prefab Replacer", true);
            window.minSize = new Vector2(400, 560);

            return window;
        }

        public override void DrawContentGUI(VisualElement contentElement)
        {
            DrawHelpText(GPUIEditorConstants.HelpTexts.prefabReplacerIntro, contentElement);
            contentElement.Add(new IMGUIContainer(DrawPrefabReplacerContents));
        }

        void DrawPrefabReplacerContents()
        {
            EditorGUILayout.BeginVertical(GPUIEditorConstants.Styles.box);
            GUILayout.Space(5);
            EditorGUILayout.BeginHorizontal();

            GPUIEditorUtility.DrawColoredButton(new GUIContent("Replace Selection With Prefab"), GPUIEditorConstants.Colors.green, Color.white, FontStyle.Bold, Rect.zero,
                () =>
                {
                    ReplaceSelectionWithPrefab();
                });

            GPUIEditorUtility.DrawColoredButton(new GUIContent(GPUIEditorConstants.Texts.cancel), Color.red, Color.white, FontStyle.Bold, Rect.zero,
                () =>
                {
                    this.Close();
                });

            EditorGUILayout.EndHorizontal();
            DrawIMGUIHelpText(GPUIEditorConstants.HelpTexts.prefabReplacerReplaceCancel);

            GUILayout.Space(10);
            selectedPrefab = (GameObject)EditorGUILayout.ObjectField("Prefab", selectedPrefab, typeof(GameObject), false);

#if UNITY_2018_3_OR_NEWER
            if (selectedPrefab != null &&
                    PrefabUtility.GetPrefabAssetType(selectedPrefab) != PrefabAssetType.Regular
                    && PrefabUtility.GetPrefabAssetType(selectedPrefab) != PrefabAssetType.Variant)
#else
            if (selectedPrefab != null && PrefabUtility.GetPrefabType(selectedPrefab) != PrefabType.Prefab)
#endif
                selectedPrefab = null;
            DrawIMGUIHelpText(GPUIEditorConstants.HelpTexts.prefabReplacerPrefab);

            replaceNames = EditorGUILayout.Toggle("Replace Names", replaceNames);
            DrawIMGUIHelpText(GPUIEditorConstants.HelpTexts.prefabReplacerReplaceNames);

            GUILayout.Space(10);
            EditorGUILayout.EndVertical();

            EditorGUILayout.BeginVertical(GPUIEditorConstants.Styles.box);
            GUILayout.Space(5);
            GPUIEditorUtility.DrawCustomLabel("Selected GameObjects", GPUIEditorConstants.Styles.boldLabel);
            DrawIMGUIHelpText(GPUIEditorConstants.HelpTexts.prefabReplacerSelectedObjects);
            GUILayout.Space(5);
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
            EditorGUILayout.BeginVertical();
            int count = 0;
            foreach (Transform selectedTransform in Selection.transforms)
            {
                EditorGUILayout.LabelField(selectedTransform.name);
                count++;
                if (count > 100)
                {
                    EditorGUILayout.LabelField("...");
                    break;
                }
            }
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        public void ReplaceSelectionWithPrefab()
        {
            Transform[] selectedTransforms = Selection.transforms;
            int totalCount = selectedTransforms.Length;
            if (selectedPrefab != null && totalCount > 0)
            {
                GameObject prefabInstance;
                for (int i = 0; i < totalCount; i++)
                {
                    if (EditorUtility.DisplayCancelableProgressBar("GPU Instancer Prefab Replacer", "Replacing prefabs: " + i + "/" + totalCount, i / (float)totalCount))
                    {
                        this.Close();
                        return;
                    }
                    Transform selectedTransform = selectedTransforms[i];
                    if (selectedTransform)
                    {
                        prefabInstance = (GameObject)PrefabUtility.InstantiatePrefab(selectedPrefab);
                        if (!replaceNames)
                            prefabInstance.name = selectedTransform.name;
                        Undo.RegisterCreatedObjectUndo(prefabInstance, "GPUI Prefab Replacer");
                        prefabInstance.transform.parent = selectedTransform.parent;
                        prefabInstance.transform.SetSiblingIndex(selectedTransform.GetSiblingIndex());
                        prefabInstance.transform.localPosition = selectedTransform.localPosition;
                        prefabInstance.transform.localRotation = selectedTransform.localRotation;
                        prefabInstance.transform.localScale = selectedTransform.localScale;
                        Undo.DestroyObjectImmediate(selectedTransform.gameObject);
                    }
                }
                EditorUtility.ClearProgressBar();
                this.Close();
                GUIUtility.ExitGUI();
            }
        }

        public override string GetWikiURLParams()
        {
            return "title=GPU_Instancer_Pro:GettingStarted#Prefab_Replacer_Window";
        }

        public override string GetTitleText()
        {
            return "GPUI Prefab Replacer";
        }
    }
}