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
    public abstract class GPUIEditor : Editor, IGPUIEditor
    {
        private VisualElement _rootVisualElement;
        protected Button _helpButton;
        protected bool _isShowHelpText = false;
        protected List<GPUIHelpBox> _helpBoxes;
        protected bool _isScrollable = false;

        // UI Stored Value Keys
        public const string KEY_UISV_isManagerSettingsFoldoutCollapsed = "isManagerSettingsFoldoutCollapsed";
        public const string KEY_UISV_isRegisteredInstancesFoldoutCollapsed = "isRegisteredInstancesFoldoutCollapsed";
        public const string KEY_UISV_isProfileSettingsFoldoutExpanded = "isProfileSettingsFoldoutExpanded";
        public const string KEY_UISV_activeToolbarIndex = "activeToolbarIndex";

        protected virtual void OnEnable()
        {
            _helpBoxes = new();
        }

        protected virtual void OnDisable()
        {
            if (_helpBoxes != null)
            {
                foreach (var hb in _helpBoxes)
                {
                    if (hb != null)
                        hb.Clear();
                }
                _helpBoxes = null;
            }
        }

        public override VisualElement CreateInspectorGUI()
        {
            _rootVisualElement = new();
            CreateInspectorUI(_rootVisualElement);

            return _rootVisualElement;
        }

        public void RedrawAll()
        {
            if (_rootVisualElement == null) return;
            _rootVisualElement.Clear();
            CreateInspectorUI(_rootVisualElement);
        }

        public virtual void CreateInspectorUI(VisualElement rootVisualElement)
        {
            CreateInspectorUI(rootVisualElement, serializedObject, GetTitleText(), GetVersionNoText(), GetWikiURLParams(), ToggleHelp, DrawContentGUI, DrawFooterGUI, _isScrollable, out _helpButton);
        }

        public static void CreateInspectorUI(VisualElement rootVisualElement, SerializedObject serializedObject, string titleText, string versionText, string wikiURLParams, Action toggleHelp, Action<VisualElement> contentDrawer, Action<VisualElement> footerDrawer, bool isScrollable, out Button helpButton)
        {
            serializedObject.Update();

            VisualTreeAsset editorUITemplate = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(GPUIEditorConstants.GetUIPath() + "GPUIEditorUI.uxml");

            editorUITemplate.CloneTree(rootVisualElement);
            if (EditorGUIUtility.isProSkin)
                rootVisualElement.styleSheets.Add(AssetDatabase.LoadAssetAtPath<StyleSheet>("Packages/com.gurbu.gpui-pro/Editor/UI/GPUIEditorStyleDark.uss"));
            else
                rootVisualElement.styleSheets.Add(AssetDatabase.LoadAssetAtPath<StyleSheet>("Packages/com.gurbu.gpui-pro/Editor/UI/GPUIEditorStyleLight.uss"));
            GPUIEditorUtility.DrawHeaderGUI(rootVisualElement.Q("HeaderElement"), titleText, versionText, wikiURLParams, toggleHelp, out helpButton);
            contentDrawer.Invoke(GPUIEditorUtility.GetContentElement(rootVisualElement, isScrollable));
            footerDrawer?.Invoke(rootVisualElement.Q("FooterElement"));;

            serializedObject.ApplyModifiedProperties();
        }

        public virtual void DrawContentGUI(VisualElement contentElement)
        {
            contentElement.Add(new IMGUIContainer(DrawIMGUIContainer));
        }

        public virtual void DrawIMGUIContainer()
        {
            DrawDefaultInspector();
        }

        public virtual void DrawFooterGUI(VisualElement footerElement) { }

        public VisualElement DrawSerializedProperty(SerializedProperty prop)
        {
            return DrawSerializedProperty(prop, out _);
        }

        public VisualElement DrawSerializedProperty(SerializedProperty prop, out PropertyField propertyField)
        {
            return DrawSerializedProperty(prop, prop != null ? prop.name : string.Empty, out propertyField);
        }

        public VisualElement DrawSerializedProperty(SerializedProperty prop, string textCode, out PropertyField propertyField)
        {
            return GPUIEditorUtility.DrawSerializedProperty(prop, textCode, _helpBoxes, out propertyField);
        }

        public static VisualElement DrawSerializedProperty(SerializedProperty prop, List<GPUIHelpBox> helpBoxes)
        {
            return GPUIEditorUtility.DrawSerializedProperty(prop, prop != null ? prop.name: string.Empty, helpBoxes, out _);
        }

        public static VisualElement DrawSerializedProperty(SerializedProperty prop, List<GPUIHelpBox> helpBoxes, out PropertyField propertyField)
        {
            return GPUIEditorUtility.DrawSerializedProperty(prop, prop != null ? prop.name : string.Empty, helpBoxes, out propertyField);
        }

        public void DrawIMGUISerializedProperty(SerializedProperty prop)
        {
            GPUIEditorUtility.DrawIMGUISerializedProperty(prop, _isShowHelpText);
        }

        public void DrawIMGUISerializedProperty(SerializedProperty prop, string textCode)
        {
            GPUIEditorUtility.DrawIMGUISerializedProperty(prop, textCode, _isShowHelpText);
        }

        public VisualElement DrawSerializedProperty<T>(BaseField<T> field, SerializedProperty prop)
        {
            return GPUIEditorUtility.DrawSerializedProperty(field, prop, prop != null ? prop.name : string.Empty, _helpBoxes);
        }

        public VisualElement DrawMultiField<T>(BaseField<T> field, SerializedProperty arrayProp, List<int> selectedIndexes, string subPropPath, bool addHelpText)
        {
            return GPUIEditorUtility.DrawMultiField(field, arrayProp, selectedIndexes, subPropPath, subPropPath, _helpBoxes, addHelpText);
        }

        public VisualElement DrawMultiField<T>(BaseField<T> field, List<SerializedProperty> props, string textCode, bool addHelpText)
        {
            return GPUIEditorUtility.DrawMultiField(field, props, textCode, _helpBoxes, addHelpText);
        }

        public VisualElement DrawMultiFieldWithValues<T>(BaseField<T> field, List<T> values, string textCode, bool addHelpText, EventCallback<ChangeEvent<T>> changeEventCallback)
        {
            return GPUIEditorUtility.DrawMultiFieldWithValues(field, values, textCode, _helpBoxes, addHelpText, changeEventCallback);
        }

        public VisualElement DrawField<T>(BaseField<T> field, T value, string textCode, EventCallback<ChangeEvent<T>> changeEventCallback)
        {
            return GPUIEditorUtility.DrawField(field, value, textCode, _helpBoxes, changeEventCallback);
        }

        public abstract string GetTitleText();

        public virtual string GetVersionNoText()
        {
            return GPUIConstants.UNITY_VERSION + " v" + GPUIEditorSettings.Instance.GetVersion();
        }

        public virtual string GetWikiURLParams()
        {
            return "title=GPU_Instancer_Pro:GettingStarted";
        }

        public void ToggleHelp()
        {
            _isShowHelpText = !_isShowHelpText;
            GPUIEditorUtility.SetShowHelp(_isShowHelpText, _helpButton, _helpBoxes);
        }

        public List<GPUIHelpBox> GetHelpBoxes()
        {
            return _helpBoxes;
        }
    }
}
