// GPU Instancer Pro
// Copyright (c) GurBu Technologies

using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Search;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace GPUInstancerPro
{
    [CustomEditor(typeof(GPUIAreaCuller))]
    public class GPUIAreaCullerEditor : GPUIEditor
    {
        private GPUIAreaCuller _areaCuller;

        protected override void OnEnable()
        {
            base.OnEnable();

            _areaCuller = target as GPUIAreaCuller;
        }

        public override void DrawContentGUI(VisualElement contentElement)
        {
            if (_areaCuller == null)
                return;

            SerializedProperty useCollidersSP = serializedObject.FindProperty("useColliders");
            contentElement.Add(DrawSerializedProperty(useCollidersSP, "areaCuller_useColliders", out var useCollidersPF));
            var colliderWarning = GPUIEditorUtility.CreateGPUIHelpBox("areaCuller_colliderWarning", null, null, HelpBoxMessageType.Warning);
            colliderWarning.SetVisible(false);
            contentElement.Add(colliderWarning);

            VisualElement boundsVE = DrawSerializedProperty(serializedObject.FindProperty("bounds"), "areaCuller_bounds", out _);
            contentElement.Add(boundsVE);
            if (useCollidersSP.boolValue)
            {
                boundsVE.SetVisible(false);
                if (!_areaCuller.gameObject.HasComponent<Collider>())
                    colliderWarning.SetVisible(true);
            }
            useCollidersPF.RegisterValueChangeCallback((evt) =>
            {
                if (useCollidersSP.boolValue)
                {
                    boundsVE.SetVisible(false);
                    if (!_areaCuller.gameObject.HasComponent<Collider>())
                        colliderWarning.SetVisible(true);
                }
                else
                {
                    boundsVE.SetVisible(true);
                    colliderWarning.SetVisible(false);
                }
            });

            contentElement.Add(DrawSerializedProperty(serializedObject.FindProperty("offset"), "areaCuller_offset", out _));

            SerializedProperty gpuiManagerFilterSP = serializedObject.FindProperty("gpuiManagerFilter");
            VisualElement gpuiManagerFilterVE = DrawSerializedProperty(gpuiManagerFilterSP, "areaCuller_gpuiManagerFilter", out var gpuiManagerFilterPF);
            gpuiManagerFilterVE.style.marginLeft = -12;
            contentElement.Add(gpuiManagerFilterVE);

            VisualElement prototypeIndexFilterVE = DrawSerializedProperty(serializedObject.FindProperty("prototypeIndexFilter"), "areaCuller_prototypeIndexFilter", out _);
            prototypeIndexFilterVE.style.marginLeft = -12;
            contentElement.Add(prototypeIndexFilterVE);
            prototypeIndexFilterVE.SetVisible(gpuiManagerFilterSP.arraySize > 0);
            gpuiManagerFilterPF.RegisterValueChangeCallback((evt) =>
            {
                prototypeIndexFilterVE.SetVisible(gpuiManagerFilterSP.arraySize > 0);
            });
        }

        public override string GetTitleText()
        {
            return "GPUI Area Culler";
        }

        public override string GetWikiURLParams()
        {
            return "title=GPU_Instancer_Pro:GettingStarted#GPUI_Area_Culler";
        }

        [MenuItem("Tools/GPU Instancer Pro/Utilities/Add Area Culler", validate = false, priority = 194)]
        public static GPUIAreaCuller ToolbarAddAreaCuller()
        {
            GameObject go = new GameObject("GPUI Area Culler");
            GPUIAreaCuller areaCuller = go.AddComponent<GPUIAreaCuller>();

            Selection.activeGameObject = go;
            Undo.RegisterCreatedObjectUndo(go, "Add Area Culler");

            return areaCuller;
        }
    }
}
