// GPU Instancer Pro
// Copyright (c) GurBu Technologies

using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace GPUInstancerPro
{
    public abstract class GPUIEditorWindow : EditorWindow, IGPUIEditor
    {
        protected Button _helpButton;
        protected bool _isShowHelpText = false;
        protected List<GPUIHelpBox> _helpBoxes;
        protected VisualTreeAsset _editorUITemplate;
        protected bool _isScrollable = false;

        protected virtual void OnEnable()
        {
            if (_editorUITemplate == null)
                _editorUITemplate = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(GPUIEditorConstants.GetUIPath() + "GPUIEditorUI.uxml");
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

        public virtual void CreateGUI()
        {
            if (_editorUITemplate == null)
                return;
            _editorUITemplate.CloneTree(rootVisualElement);
            if (IsDrawHeader())
                GPUIEditorUtility.DrawHeaderGUI(rootVisualElement.Q("HeaderElement"), GetTitleText(), GetVersionNoText(), GetWikiURLParams(), ToggleHelp, out _helpButton);
            else
                rootVisualElement.Q("HeaderElement").SetVisible(false);
            DrawContentGUI(GPUIEditorUtility.GetContentElement(rootVisualElement, _isScrollable));
            DrawFooterGUI(rootVisualElement.Q("FooterElement"));
        }

        public abstract void DrawContentGUI(VisualElement contentElement);

        public virtual void DrawFooterGUI(VisualElement footerElement) { }

        public VisualElement DrawSerializedProperty(SerializedProperty prop)
        {
            return DrawSerializedProperty(prop, out _);
        }

        public VisualElement DrawSerializedProperty(SerializedProperty prop, out PropertyField propertyField)
        {
            return DrawSerializedProperty(prop, prop.name, out propertyField);
        }

        public VisualElement DrawSerializedProperty(SerializedProperty prop, string textCode, out PropertyField propertyField)
        {
            return GPUIEditorUtility.DrawSerializedProperty(prop, textCode, _helpBoxes, out propertyField);
        }

        public void DrawHelpText(string text, VisualElement rootElement)
        {
            GPUIEditorUtility.DrawHelpText(_helpBoxes, text, rootElement);
        }

        public void DrawIMGUIHelpText(string text)
        {
            if (_isShowHelpText)
                GPUIEditorUtility.DrawIMGUIHelpText(text);
        }

        public virtual string GetWikiURLParams()
        {
            return "title=GPU_Instancer_Pro:GettingStarted";
        }
        
        public abstract string GetTitleText();

        public virtual string GetVersionNoText()
        {
            return GPUIConstants.UNITY_VERSION + " v" + GPUIEditorSettings.Instance.GetVersion();
        }

        public void ToggleHelp()
        {
            _isShowHelpText = !_isShowHelpText;
            GPUIEditorUtility.SetShowHelp(_isShowHelpText, _helpButton, _helpBoxes);
        }

        public bool IsShowHelpText()
        {
            return _isShowHelpText;
        }

        protected virtual bool IsDrawHeader()
        {
            return true;
        }
    }
}
