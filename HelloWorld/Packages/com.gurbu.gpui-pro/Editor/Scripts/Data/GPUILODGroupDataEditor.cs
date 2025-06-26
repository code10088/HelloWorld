// GPU Instancer Pro
// Copyright (c) GurBu Technologies

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace GPUInstancerPro
{
    [CustomEditor(typeof(GPUILODGroupData))]
    public class GPUILODGroupDataEditor : GPUIEditor
    {
        private GPUILODGroupData _lodGroupData;
        private VisualTreeAsset _lodGroupDataUITemplate;
        private VisualElement _lodGroupSlider;
        private int _currentSeparatorIndex;
        private VisualElement _focusedLOD;
        private int _focusedLODIndex;
        private VisualElement _lodRenderers;
        private List<PropertyField> _boundPropertyFields;

        private readonly Color[] _lodColors = new Color[] { 
            new(60f / 255f, 70f / 255f, 36f / 255f, 0.5f),
            new(46f / 255f, 55f / 255f, 67f / 255f, 0.5f),
            new(40f / 255f, 64f / 255f, 73f / 255f, 0.5f),
            new(64f / 255f, 37f / 255f, 27f / 255f, 0.5f),
            new(53f / 255f, 46f / 255f, 63f / 255f, 0.5f),
            new(83f / 255f, 58f / 255f, 25f / 255f, 0.5f),
            new(58f / 255f, 55f / 255f, 29f / 255f, 0.5f),
            new(82f / 255f, 71f / 255f, 26f / 255f, 0.5f),
            new(80f / 255f, 0f / 255f, 0f / 255f, 0.5f),
        };

        protected override void OnEnable()
        {
            base.OnEnable();

            _lodGroupDataUITemplate = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(GPUIEditorConstants.GetUIPath() + "GPUILODGroupDataEditorUI.uxml");

            _lodGroupData = target as GPUILODGroupData;
            _lodGroupData?.InitializeTransitionValues();
        }

        public override void DrawContentGUI(VisualElement contentElement)
        {
            VisualElement rootElement = _lodGroupDataUITemplate.Instantiate();
            contentElement.Add(rootElement);

            _lodGroupSlider = rootElement.Q("LODGroupSlider");
            _lodRenderers = rootElement.Q("LODRenderers");

            ClearBeforeRedraw();
            DrawLODGroupSlider();
            DrawLODRenderers();

            VisualElement boundsVE = DrawSerializedProperty(serializedObject.FindProperty("bounds"));
            contentElement.Add(boundsVE);
            boundsVE.RegisterCallback<ChangeEvent<float>>((evt) =>
            {
                serializedObject.ApplyModifiedProperties();
                _lodGroupData.SetParameterBufferData();
                serializedObject.Update();
            });
            Button calculateBoundsButton = new Button(_lodGroupData.CalculateBounds);
            calculateBoundsButton.text = "Recalculate Bounds";
            contentElement.Add(calculateBoundsButton);

            VisualElement transitionValuesVE = DrawSerializedProperty(serializedObject.FindProperty("transitionValues"));
            contentElement.Add(transitionValuesVE);
            transitionValuesVE.RegisterCallbackDelayed<ChangeEvent<float>>(OnTransitionValuesChanged);

            VisualElement fadeTransitionWidthVE = DrawSerializedProperty(serializedObject.FindProperty("fadeTransitionWidth"));
            contentElement.Add(fadeTransitionWidthVE);
            fadeTransitionWidthVE.RegisterCallbackDelayed<ChangeEvent<float>>(OnTransitionValuesChanged);
        }

        private void OnTransitionValuesChanged(ChangeEvent<float> evt)
        {
            serializedObject.ApplyModifiedProperties();
            _lodGroupData.SetParameterBufferData();
            serializedObject.Update();
            //ClearBeforeRedraw();
            DrawLODGroupSlider();
            //DrawLODRenderers();
        }

        #region DrawLODGroupSlider

        private void DrawLODGroupSlider()
        {
            _lodGroupSlider.Clear();
            List<VisualElement> lodSeparators = new List<VisualElement>();
            int lodCount = _lodGroupData.Length;
            float previousPercent = -1f;
            for (int i = 0; i <= lodCount; i++)
            {
                float percent = 1f;
                if (i > 0)
                {
                    percent = _lodGroupData.transitionValues[i - 1];
                    if (percent == previousPercent && percent == 0)
                        continue;
                    previousPercent = percent;
                }
                Length left = new(ToSliderPercentage(1f - percent) * 100f, LengthUnit.Percent);

                int index = i;
                if (lodCount != i || percent != 0f)
                {
                    VisualElement lod = new VisualElement();
                    lod.name = (lodCount == i ? "Culled" : "LOD" + i);
                    Label lodLabel = new Label((lodCount == i ? "Culled" : "LOD " + i) + "\n%" + Mathf.RoundToInt(percent * 100f));
                    lod.Add(lodLabel);
                    lod.AddToClassList("gpui-lod");
                    lod.style.backgroundColor = lodCount == i ? _lodColors[8] : _lodColors[i];
                    lod.style.left = left;

                    if (!Application.isPlaying)
                    {
                        lod.AddManipulator(new ContextualMenuManipulator((evt) =>
                        {
                            evt.menu.AppendAction("Insert Before",
                                OnInsertAction,
                                GetInsertStatus,
                                index.ToString());
                            evt.menu.AppendAction("Delete",
                                OnDeleteAction,
                                GetDeleteStatus,
                                index.ToString());
                        }));
                    }

                    if (lodCount > i)
                    {
                        lod.focusable = true;
                        lod.RegisterCallback<FocusEvent>((evt) => {
                            OnLODFocus(lod, index);
                        });
                        if (_focusedLODIndex == i)
                            OnLODFocus(lod, i);
                    }

                    _lodGroupSlider.Add(lod);
                }

                if (i > 0)
                {
                    VisualElement lodSeparator = new VisualElement();
                    lodSeparator.name = "LODSeparator" + i;
                    lodSeparator.AddToClassList("gpui-lod-separator");
                    lodSeparator.style.left = left;

                    lodSeparator.RegisterCallback<MouseDownEvent>(evt => {
                        _currentSeparatorIndex = index - 1;
                        _lodGroupSlider.RegisterCallback<MouseMoveEvent>(OnSliderMove);
                    });
                    _lodGroupSlider.RegisterCallback<MouseUpEvent>(evt => {
                        _currentSeparatorIndex = -1;
                        _lodGroupSlider.UnregisterCallback<MouseMoveEvent>(OnSliderMove);
                    });
                    _lodGroupSlider.RegisterCallback<MouseLeaveEvent>(evt => {
                        _currentSeparatorIndex = -1;
                        _lodGroupSlider.UnregisterCallback<MouseMoveEvent>(OnSliderMove);
                    });

                    lodSeparators.Add(lodSeparator);
                }
            }

            for (int i = 0; i < lodSeparators.Count; i++)
            {
                _lodGroupSlider.Add(lodSeparators[i]);
            }
        }

        private void OnSliderMove(MouseMoveEvent mouseMoveEvent)
        {
            if (_currentSeparatorIndex < 0 || _currentSeparatorIndex > 7 || mouseMoveEvent.mouseDelta.x == 0f)
                return;

            float currentValue = _lodGroupData.transitionValues[_currentSeparatorIndex];
            float leftValue = _currentSeparatorIndex == 0 ? 1 : _lodGroupData.transitionValues[_currentSeparatorIndex - 1];
            float rightValue = _currentSeparatorIndex == 7 ? 0 : _lodGroupData.transitionValues[_currentSeparatorIndex + 1];

            float change = mouseMoveEvent.mouseDelta.x / _lodGroupSlider.contentRect.width;
            currentValue -= change;
            //float sliderPercentage = Mathf.Clamp01(ToSliderPercentage(currentValue) + change);
            //currentValue = ToTransitionValue(sliderPercentage);
            //Debug.Log(GPUIConstants.LOG_PREFIX + "sliderPercentage: " + sliderPercentage + " ToTransitionValue: " + (/*1f -*/ ToTransitionValue(sliderPercentage)));

            currentValue = Mathf.Max(rightValue, Mathf.Min(leftValue, currentValue));
            if (currentValue < 0.01)
                currentValue = 0;
            _lodGroupData.transitionValues[_currentSeparatorIndex] = currentValue;
            _lodGroupData.SetParameterBufferData();

            DrawLODGroupSlider();
        }

        private void OnInsertAction(DropdownMenuAction dropdownMenuAction)
        {
            ClearBeforeRedraw();
            int index = int.Parse(dropdownMenuAction.userData as string);
            _lodGroupData.AddLODAtIndex(index);
            serializedObject.Update();
            DrawLODGroupSlider();
            DrawLODRenderers();
        }

        private DropdownMenuAction.Status GetInsertStatus(DropdownMenuAction dropdownMenuAction)
        {
            if (_lodGroupData.Length < 8)
                return DropdownMenuAction.Status.Normal;
            return DropdownMenuAction.Status.Disabled;
        }

        private void OnDeleteAction(DropdownMenuAction dropdownMenuAction)
        {
            ClearBeforeRedraw();
            int index = int.Parse(dropdownMenuAction.userData as string);
            _lodGroupData.RemoveLODAtIndex(index);
            serializedObject.Update();
            DrawLODGroupSlider();
            DrawLODRenderers();
        }

        private DropdownMenuAction.Status GetDeleteStatus(DropdownMenuAction dropdownMenuAction)
        {
            int index = int.Parse(dropdownMenuAction.userData as string);
            if (_lodGroupData.Length > 1 && index < _lodGroupData.Length)
                return DropdownMenuAction.Status.Normal;
            return DropdownMenuAction.Status.Disabled;
        }

        private void OnLODFocus(VisualElement lod, int index)
        {
            _focusedLODIndex = index;
            if (_focusedLOD != null)
                _focusedLOD.RemoveFromClassList("gpui-lod-focused");
            _focusedLOD = lod;
            _focusedLOD.AddToClassList("gpui-lod-focused");
            OnLODRenderersFocus();
        }

        #endregion DrawLODGroupSlider

        #region DrawLODRenderers

        private void DrawLODRenderers()
        {
            _lodRenderers.Clear();
            int lodCount = serializedObject.FindProperty("lodDataArray").arraySize;
            _boundPropertyFields = new List<PropertyField>();
            for (int i = 0; i < lodCount; i++)
            {
                VisualElement lodRenderersContainer = new();
                lodRenderersContainer.name = "LOD" + i + "RenderersContainer";
                lodRenderersContainer.AddToClassList("gpui-lod-renderer-container");
                lodRenderersContainer.AddToClassList("gpui-bg-light");
                if (_focusedLODIndex == i)
                    lodRenderersContainer.AddToClassList("gpui-lod-renderer-container-focused");

                Foldout foldout = new();
                foldout.name = "LOD" + i + "RenderersFoldout";
                foldout.value = false;
                foldout.text = "         LOD " + i;

                PropertyField propertyField = new();
                propertyField.label = "Renderers";
                propertyField.BindProperty(serializedObject.FindProperty("lodDataArray").GetArrayElementAtIndex(i).FindPropertyRelative("rendererDataArray"));
                foldout.Add(propertyField);
                _boundPropertyFields.Add(propertyField);
                //propertyField.SetEnabled(!Application.isPlaying);

                lodRenderersContainer.Add(foldout);

                VisualElement lodColor = new();
                lodColor.name = "LOD" + i + "RenderersTitleColor";
                lodColor.AddToClassList("gpui-lod-renderer-title-color");
                lodColor.style.backgroundColor = _lodColors[i];
                lodRenderersContainer.Add(lodColor);

                _lodRenderers.Add(lodRenderersContainer);
            }
        }

        private void OnLODRenderersFocus()
        {
            int lodCount = _lodGroupData.Length;
            for (int i = 0; i < lodCount; i++)
            {
                VisualElement container = _lodRenderers.Q("LOD" + i + "RenderersContainer");
                if (container != null)
                {
                    container.RemoveFromClassList("gpui-lod-renderer-container-focused");
                    Foldout foldout = container.Q<Foldout>("LOD" + i + "RenderersFoldout");
                    if (foldout != null)
                    {
                        foldout.value = false;

                        if (_focusedLODIndex == i)
                        {
                            container.AddToClassList("gpui-lod-renderer-container-focused");
                            foldout.value = true;
                        }
                    }
                }
            }
        }

        #endregion DrawLODRenderers

        private void ClearBeforeRedraw()
        {
            if (_boundPropertyFields != null)
            {
                for (int i = 0; i < _boundPropertyFields.Count; i++)
                {
                    _boundPropertyFields[i].Unbind();
                }
                _boundPropertyFields = null;
            }
            _focusedLODIndex = -1;
        }

        private float ToSliderPercentage(float x)
        {
            return Mathf.Exp(Mathf.Pow(x, 8f / 5f) / 1.442695f) - 1f;
        }

        private float ToTransitionValue(float x)
        {
            return Mathf.Log10(Mathf.Pow(x, 5f / 8f) + 1f) * 3.321928f;
        }

        public override string GetTitleText()
        {
            return "GPUI LOD Group Data";
        }

        public override string GetWikiURLParams()
        {
            return "title=GPU_Instancer_Pro:GettingStarted#GPUI_LOD_Group_Data";
        }
    }
}
