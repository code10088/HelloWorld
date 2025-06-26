// GPU Instancer Pro
// Copyright (c) GurBu Technologies

using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UIElements;

namespace GPUInstancerPro
{
    public static class GPUIEditorUtility
    {
        #region Custom Editor

        public static VisualElement GetContentElement(VisualElement rootVisualElement, bool isScrollable)
        {
            VisualElement contentElement = rootVisualElement.Q("ContentElement");
            if (isScrollable)
            {
                contentElement.SetVisible(false);
                rootVisualElement.Q("ContentScrollView").SetVisible(true);
                contentElement = rootVisualElement.Q("ScrollContentElement");
            }
            return contentElement;
        }

        public static void DrawHeaderGUI(VisualElement headerElement, string title, string version, string wikiURLParams, Action toggleHelpAction, out Button helpButton)
        {
            headerElement.Q<Label>("TitleText").text = title;
            headerElement.Q<Label>("VersionText").text = version;

            helpButton = headerElement.Q<Button>("HelpButton");
            helpButton.RegisterCallback<MouseUpEvent>(ev => toggleHelpAction?.Invoke());

            Button wikiButton = headerElement.Q<Button>("WikiButton");
            if (!string.IsNullOrEmpty(wikiURLParams))
            {
                wikiButton.style.display = DisplayStyle.Flex;
                wikiButton.RegisterCallback<MouseUpEvent>(ev => Application.OpenURL("https://wiki.gurbu.com/index.php?" + wikiURLParams));
            }
            else
                wikiButton.style.display = DisplayStyle.None;
        }

        public static void DrawHelpText(List<GPUIHelpBox> helpBoxes, GPUIEditorTextUtility.GPUIText gpuiText, VisualElement rootElement)
        {
            if (string.IsNullOrEmpty(gpuiText.helpText))
                return;
            GPUIHelpBox helpBox = new(gpuiText.helpText, HelpBoxMessageType.Info, gpuiText.wwwAddress);
            helpBox.AddToClassList("gpui-hidden");
            rootElement.Add(helpBox);
            if (helpBoxes != null)
                helpBoxes.Add(helpBox);
        }

        public static void DrawHelpText(List<GPUIHelpBox> helpBoxes, string text, VisualElement rootElement)
        {
            GPUIHelpBox helpBox = new(text, HelpBoxMessageType.Info);
            helpBox.AddToClassList("gpui-hidden");
            rootElement.Add(helpBox);
            helpBoxes.Add(helpBox);
        }

        public static void DrawErrorMessage(VisualElement container, int errorCode, UnityEngine.Object targetObject, UnityAction fixAction, string fixButtonText = null)
        {
            DrawGPUIHelpBox(container, errorCode, targetObject, fixAction, errorCode > 0 ? HelpBoxMessageType.Error : HelpBoxMessageType.Warning, fixButtonText);
        }

        public static void DrawGPUIHelpBox(VisualElement container, int errorCode, UnityEngine.Object targetObject, UnityAction fixAction, HelpBoxMessageType messageType, string fixButtonText = null)
        {
            if (errorCode != 0)
                container.Add(CreateGPUIHelpBox(errorCode, targetObject, fixAction, messageType, fixButtonText));
        }

        public static GPUIHelpBox CreateGPUIHelpBox(int errorCode, UnityEngine.Object targetObject, UnityAction fixAction, HelpBoxMessageType messageType, string fixButtonText = null)
        {
            string textCode = string.Format("Error[{0}]", Mathf.Abs(errorCode));
            return CreateGPUIHelpBox(textCode, targetObject, fixAction, messageType, fixButtonText);
        }

        public static GPUIHelpBox CreateGPUIHelpBox(string textCode, UnityEngine.Object targetObject, UnityAction fixAction, HelpBoxMessageType messageType, string fixButtonText = null, Color? fixButtonColor = null)
        {
            if (GPUIEditorTextUtility.TryGetGPUIText(textCode, out var gpuiText) && !string.IsNullOrEmpty(gpuiText.helpText))
                return new GPUIHelpBox(gpuiText.helpText, messageType, gpuiText.wwwAddress, targetObject, fixAction, fixButtonText, fixButtonColor);
            else
                return new GPUIHelpBox(textCode, messageType, null, targetObject, fixAction, fixButtonText, fixButtonColor);
        }

        public static void DrawIMGUIErrorMessage(int errorCode, UnityEngine.Object targetObject = null)
        {
            if (errorCode != 0)
            {
                string textCode = string.Format("Error[{0}]", Mathf.Abs(errorCode));
                if (GPUIEditorTextUtility.TryGetGPUIText(textCode, out var gpuiText) && !string.IsNullOrEmpty(gpuiText.helpText))
                    EditorGUILayout.HelpBox(gpuiText.helpText, errorCode > 0 ? MessageType.Error : MessageType.Warning);
                else
                    EditorGUILayout.HelpBox(textCode, errorCode > 0 ? MessageType.Error : MessageType.Warning);
            }
        }

        public static void DrawIMGUISerializedProperty(Rect position, SerializedProperty prop, bool isShowHelpText)
        {
            if (prop == null)
            {
#if GPUIPRO_DEVMODE
                Debug.LogError(GPUIConstants.LOG_PREFIX + "Property is null!");
#endif
                return;
            }
            float labelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = GPUIEditorConstants.LABEL_WIDTH;
            string textCode = prop.name;
            if (GPUIEditorTextUtility.TryGetGPUIText(textCode, out GPUIEditorTextUtility.GPUIText gpuiText))
            {
                EditorGUI.PropertyField(position, prop, new GUIContent(gpuiText.title, gpuiText.tooltip));
                if (isShowHelpText)
                    DrawIMGUIHelpText(gpuiText.helpText);
            }
            else
                EditorGUI.PropertyField(position, prop);
            EditorGUIUtility.labelWidth = labelWidth;
        }

        public static void DrawIMGUISerializedProperty(SerializedProperty prop, bool isShowHelpText)
        {
            DrawIMGUISerializedProperty(prop, prop == null ? null : prop.name, isShowHelpText);
        }

        public static void DrawIMGUISerializedProperty(SerializedProperty prop, string textCode, bool isShowHelpText)
        {
            if (prop == null)
            {
#if GPUIPRO_DEVMODE
                Debug.LogError(GPUIConstants.LOG_PREFIX + "Property is null!");
#endif
                return;
            }
            float labelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = GPUIEditorConstants.LABEL_WIDTH;
            if (GPUIEditorTextUtility.TryGetGPUIText(textCode, out GPUIEditorTextUtility.GPUIText gpuiText))
            {
                EditorGUILayout.PropertyField(prop, new GUIContent(gpuiText.title, gpuiText.tooltip));
                if (isShowHelpText)
                    DrawIMGUIHelpText(gpuiText.helpText);
            }
            else
                EditorGUILayout.PropertyField(prop);
            EditorGUIUtility.labelWidth = labelWidth;
        }

        public static void DrawIMGUIHelpText(string text, MessageType messageType = MessageType.Info)
        {
            if (string.IsNullOrEmpty(text))
                return;
            bool isDisabled = !GUI.enabled;
            if (isDisabled)
                EditorGUI.EndDisabledGroup();
            GUIStyle helpBoxStyle = GUI.skin.GetStyle("HelpBox");
            helpBoxStyle.richText = true;
            GUIContent gUIContent = EditorGUIUtility.IconContent(GetHelpMessageIconName(messageType));
            gUIContent.text = text;
            EditorGUILayout.LabelField(gUIContent, helpBoxStyle);
            if (isDisabled)
                EditorGUI.BeginDisabledGroup(true);
        }

        private static string GetHelpMessageIconName(MessageType messageType)
        {
            switch (messageType)
            {
                case MessageType.Warning:
                    return "d_console.warnicon";
                case MessageType.Error:
                    return "d_console.erroricon";
            }
            return "d_console.infoicon";
        }

        public static void SetShowHelp(bool isShowHelpText, Button helpButton, List<GPUIHelpBox> helpBoxes)
        {
            if (helpButton != null)
            {
                if (isShowHelpText)
                {
                    helpButton.AddToClassList("gpui-help-button-active");
                    foreach (var helpBox in helpBoxes)
                        if (helpBox != null)
                            helpBox.RemoveFromClassList("gpui-hidden");
                    helpButton.tooltip = "Hide Help";
                }
                else
                {
                    helpButton.RemoveFromClassList("gpui-help-button-active");
                    foreach (var helpBox in helpBoxes)
                        if (helpBox != null)
                            helpBox.AddToClassList("gpui-hidden");
                    helpButton.tooltip = "Show Help";
                }
            }
        }

        public static Foldout DrawBoxContainer(VisualElement parentVE, string title, bool foldoutValue = true)
        {
            VisualElement container = new();
            container.style.marginTop = 5f;
            container.AddToClassList("gpui-border");
            container.AddToClassList("gpui-bg-light");
            parentVE.Add(container);

            Foldout foldout = new Foldout();
            foldout.value = foldoutValue;
            foldout.text = title;
            foldout.style.unityFontStyleAndWeight = FontStyle.Bold;
            container.Add(foldout);

            return foldout;
        }

        public static Foldout DrawBoxContainerWithUIStoredValue(VisualElement parentVE, string textCode, List<GPUIHelpBox> helpBoxes, Component component, string storedValueKey, bool showTooltip = false)
        {
            Foldout foldout = DrawBoxContainer(parentVE, textCode, false, helpBoxes, showTooltip);
            if (!string.IsNullOrEmpty(storedValueKey))
            {
                foldout.value = GPUIRenderingSystem.Editor_GetUIStoredValue(component, storedValueKey) > 0;
                foldout.RegisterValueChangedCallback((evt) => { if (evt.target == foldout) GPUIRenderingSystem.Editor_SetUIStoredValue(component, storedValueKey, evt.newValue ? 1 : 0); });
            }
            return foldout;
        }

        public static Foldout DrawBoxContainer(VisualElement parentVE, string textCode, bool foldoutValue, List<GPUIHelpBox> helpBoxes, bool showTooltip = false)
        {
            VisualElement boxContainerVE = new();
            boxContainerVE.AddToClassList("gpui-border");
            boxContainerVE.AddToClassList("gpui-bg-light");
            boxContainerVE.style.marginTop = 4;
            boxContainerVE.style.marginBottom = 4;
            boxContainerVE.style.marginLeft = 0;
            boxContainerVE.style.paddingTop = 0;
            boxContainerVE.style.paddingBottom = 0;
            parentVE.Add(boxContainerVE);
            GPUIEditorTextUtility.TryGetGPUIText(textCode, out GPUIEditorTextUtility.GPUIText gpuiText);
            Foldout foldout = new()
            {
                text = gpuiText.title,
                value = foldoutValue
            };
            if (showTooltip)
                foldout.tooltip = gpuiText.tooltip;
            foldout.style.paddingTop = 2;
            foldout.style.paddingBottom = 0;
            //foldout.style.unityFontStyleAndWeight = FontStyle.Bold;
            boxContainerVE.Add(foldout);
            if (helpBoxes != null)
                DrawHelpText(helpBoxes, gpuiText, parentVE);

            return foldout;
        }

        public static VisualElement DrawSerializedProperty(SerializedProperty prop)
        {
            return DrawSerializedProperty(prop, prop != null ? prop.name : null, null, out _);
        }

        public static VisualElement DrawSerializedProperty(SerializedProperty prop, out PropertyField propertyField)
        {
            return DrawSerializedProperty(prop, prop != null ? prop.name : null, null, out propertyField);
        }

        public static VisualElement DrawSerializedProperty(SerializedProperty prop, string textCode, List<GPUIHelpBox> helpBoxes, out PropertyField propertyField)
        {
            propertyField = null;
            if (prop == null)
            {
#if GPUIPRO_DEVMODE
                Debug.LogError(GPUIConstants.LOG_PREFIX + "Property is null!");
#endif
                return new VisualElement();
            }
            propertyField = new PropertyField(prop);
            propertyField.Bind(prop.serializedObject);
            VisualElement containerVE = new VisualElement();
            containerVE.name = prop.name + "Container";
            containerVE.AddToClassList("gpui-field");
            containerVE.Add(propertyField);
            if (GPUIEditorTextUtility.TryGetGPUIText(textCode, out GPUIEditorTextUtility.GPUIText gpuiText))
            {
                propertyField.label = gpuiText.title;
                propertyField.tooltip = gpuiText.tooltip;
                if (helpBoxes != null)
                    DrawHelpText(helpBoxes, gpuiText, containerVE);
                if (prop.propertyType != SerializedPropertyType.Vector3)
                    RegisterResizeLabelEvent(propertyField);
            }
            else
                propertyField.label = "";
            return containerVE;
        }

        public static void RegisterResizeLabelEvent(VisualElement visualElement)
        {
            visualElement.RegisterCallback<GeometryChangedEvent>(OnResizeLabelEvent);
        }

        public static void RegisterHideLabelEvent(VisualElement visualElement)
        {
            visualElement.RegisterCallback<GeometryChangedEvent>(OnHideLabelEvent);
        }

        private static void OnResizeLabelEvent(EventBase evt)
        {
            if (evt.target is not VisualElement visualElement)
                return;
            Label label = visualElement.Q<Label>();
            if (label != null)
            {
                label.style.width = GPUIEditorConstants.LABEL_WIDTH;
                label.style.minWidth = GPUIEditorConstants.LABEL_WIDTH;
                label.style.maxWidth = GPUIEditorConstants.LABEL_WIDTH;
                label.style.flexShrink = 0;
                label.style.flexGrow = 0;
                if (evt.target is PropertyField propertyField && !string.IsNullOrEmpty(propertyField.label))
                    label.text = propertyField.label;
            }
        }

        private static void OnHideLabelEvent(EventBase evt)
        {
            if (evt.target is not VisualElement visualElement)
                return;
            Label label = visualElement.Q<Label>();
            if (label != null)
            {
                label.style.display = DisplayStyle.None;
            }
        }

        public static VisualElement DrawSerializedProperty<T>(BaseField<T> field, SerializedProperty prop)
        {
            return DrawSerializedProperty(field, prop, prop.name, null);
        }

        public static VisualElement DrawSerializedProperty<T>(BaseField<T> field, SerializedProperty prop, string textCode, List<GPUIHelpBox> helpBoxes)
        {
            if (prop == null)
            {
                Debug.LogError(GPUIConstants.LOG_PREFIX + "Property is null");
                return new VisualElement();
            }
            field.BindProperty(prop);
            VisualElement containerVE = new VisualElement();
            containerVE.name = prop.name + "Container";
            containerVE.AddToClassList("gpui-field");
            containerVE.Add(field);
            if (GPUIEditorTextUtility.TryGetGPUIText(textCode, out GPUIEditorTextUtility.GPUIText gpuiText))
            {
                field.label = gpuiText.title;
                field.tooltip = gpuiText.tooltip;
                if (helpBoxes != null)
                    DrawHelpText(helpBoxes, gpuiText, containerVE);

                RegisterResizeLabelEvent(field);
            }
            else
                field.label = "";
            return containerVE;
        }

        public static VisualElement DrawMultiField<T>(BaseField<T> field, SerializedProperty arrayProp, List<int> selectedIndexes, string subPropPath, string textCode, List<GPUIHelpBox> helpBoxes, bool addHelpText)
        {
            if (arrayProp == null)
            {
#if GPUIPRO_DEVMODE
                Debug.LogError(GPUIConstants.LOG_PREFIX + "Array property is null");
#endif
                return null;
            }
            if (selectedIndexes == null)
            {
#if GPUIPRO_DEVMODE
                Debug.LogError(GPUIConstants.LOG_PREFIX + "Selected indexes is null");
#endif
                return null;
            }
            if (selectedIndexes.Count == 0)
            {
#if GPUIPRO_DEVMODE
                Debug.LogError(GPUIConstants.LOG_PREFIX + "No selection");
#endif
                return null;
            }

            List<SerializedProperty> props = new List<SerializedProperty>();
            if (string.IsNullOrEmpty(subPropPath))
            {
                for (int i = 0; i < selectedIndexes.Count; i++)
                    props.Add(arrayProp.GetArrayElementAtIndex(selectedIndexes[i]));
            }
            else
            {
                for (int i = 0; i < selectedIndexes.Count; i++)
                    props.Add(arrayProp.GetArrayElementAtIndex(selectedIndexes[i]).FindPropertyRelative(subPropPath));
            }

            return DrawMultiField(field, props, textCode, helpBoxes, addHelpText);
        }

        public static VisualElement DrawMultiField<T>(BaseField<T> field, List<SerializedProperty> props, string textCode, List<GPUIHelpBox> helpBoxes, bool addHelpText)
        {
            if (props == null)
            {
#if GPUIPRO_DEVMODE
                Debug.LogError(GPUIConstants.LOG_PREFIX + "Properties are null");
#endif
                return null;
            }
            if (props.Count == 0)
            {
#if GPUIPRO_DEVMODE
                Debug.LogError(GPUIConstants.LOG_PREFIX + "Property size is 0");
#endif
                return null;
            }

            object value0 = props[0].GetPropertyValue();
            bool isMixed = false;
            for (int i = 1; i < props.Count; i++)
            {
                object valueI = props[i].GetPropertyValue();
                if (valueI != value0)
                {
                    isMixed = true;
                    break;
                }
            }

            field.value = (T)value0;
            field.showMixedValue = isMixed; // showMixedValue should be set after setting the value
            field.RegisterValueChangedCallback(evt =>
            {
                for (int i = 0; i < props.Count; i++)
                {
                    props[i].SetPropertyValue(evt.newValue);
                    props[i].serializedObject.ApplyModifiedProperties();
                    props[i].serializedObject.Update();
                }
                field.showMixedValue = false;
            });

            RegisterResizeLabelEvent(field);
            return SetTooltipAndHelpText(field, textCode, helpBoxes, addHelpText);
        }

        public static VisualElement DrawMultiFieldWithValues<T>(BaseField<T> field, List<T> values, string textCode, List<GPUIHelpBox> helpBoxes, bool addHelpText, EventCallback<ChangeEvent<T>> changeEventCallback)
        {
            if (values == null)
            {
#if GPUIPRO_DEVMODE
                Debug.LogError(GPUIConstants.LOG_PREFIX + "Values are null");
#endif
                return null;
            }
            if (values.Count == 0)
            {
#if GPUIPRO_DEVMODE
                Debug.LogError(GPUIConstants.LOG_PREFIX + "Values size is 0");
#endif
                return null;
            }

            T value0 = values[0];
            bool isMixed = false;
            for (int i = 1; i < values.Count; i++)
            {
                if (!values[i].Equals(value0))
                {
                    isMixed = true;
                    break;
                }
            }

            field.value = value0;
            field.showMixedValue = isMixed;  // showMixedValue should be set after setting the value
            field.RegisterValueChangedCallback(evt =>
            {
                changeEventCallback.Invoke(evt);
                field.showMixedValue = false;
            });
            BaseField<T> f = field;
            RegisterResizeLabelEvent(field);
            return SetTooltipAndHelpText(field, textCode, helpBoxes, addHelpText);
        }

        public static VisualElement DrawField<T>(BaseField<T> field, T value, string textCode, List<GPUIHelpBox> helpBoxes, EventCallback<ChangeEvent<T>> changeEventCallback)
        {
            field.value = value;
            field.RegisterValueChangedCallback(changeEventCallback);
            VisualElement containerVE = new VisualElement();
            containerVE.name = textCode + "Container";
            containerVE.AddToClassList("gpui-field");
            containerVE.Add(field);
            if (GPUIEditorTextUtility.TryGetGPUIText(textCode, out GPUIEditorTextUtility.GPUIText gpuiText))
            {
                field.label = gpuiText.title;
                field.tooltip = gpuiText.tooltip;
                if (helpBoxes != null)
                    DrawHelpText(helpBoxes, gpuiText, containerVE);
                if (typeof(T) != typeof(Vector3))
                {
                    BaseField<T> f = field;
                    RegisterResizeLabelEvent(field);
                }
            }
            else
                field.label = "";
            return containerVE;
        }

        private static VisualElement SetTooltipAndHelpText<T>(BaseField<T> field, string textCode, List<GPUIHelpBox> helpBoxes, bool addHelpText)
        {
            VisualElement containerVE = field;
            if (addHelpText)
            {
                containerVE = new();
                containerVE.name = textCode + "Container";
                containerVE.Add(field);
            }
            containerVE.AddToClassList("gpui-field");

            if (GPUIEditorTextUtility.TryGetGPUIText(textCode, out GPUIEditorTextUtility.GPUIText gpuiText))
            {
                field.label = gpuiText.title;
                field.tooltip = gpuiText.tooltip;
                if (helpBoxes != null && addHelpText)
                    DrawHelpText(helpBoxes, gpuiText, containerVE);
            }
            else
                field.label = GPUIUtility.CamelToTitleCase(textCode);

            return containerVE;
        }

        public static bool DisplayDialog(string textCode, bool showCancelButton)
        {
            if (string.IsNullOrEmpty(textCode))
                return false;
            if (!GPUIEditorTextUtility.TryGetGPUIText(textCode, out GPUIEditorTextUtility.GPUIText gpuiText) || string.IsNullOrEmpty(gpuiText.helpText))
            {
                Debug.LogError(GPUIConstants.LOG_PREFIX + "Can not find dialog text for code: " + textCode);
                return false;
            }
            if (showCancelButton)
                return EditorUtility.DisplayDialog(gpuiText.title, gpuiText.HelpTextNoEscape, GPUIEditorConstants.Texts.ok, GPUIEditorConstants.Texts.cancel);
            else
                return EditorUtility.DisplayDialog(gpuiText.title, gpuiText.HelpTextNoEscape, GPUIEditorConstants.Texts.ok);
        }

        public static void DrawCustomLabel(string text, GUIStyle style, bool center = true)
        {
            if (center)
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
            }

            GUILayout.Label(text, style);

            if (center)
            {
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();
            }
        }

        public static void DrawColoredButton(GUIContent guiContent, Color backgroundColor, Color textColor, FontStyle fontStyle, Rect buttonRect, UnityAction clickAction,
            bool isRichText = false, bool dragDropEnabled = false, UnityAction<UnityEngine.Object> dropAction = null, GUIStyle style = null)
        {
            Color oldBGColor = GUI.backgroundColor;
            if (backgroundColor != default)
                GUI.backgroundColor = backgroundColor;
            if (style == null)
                style = new GUIStyle("button");
            if (textColor != default)
            {
                style.normal.textColor = textColor;
                style.active.textColor = textColor;
                style.hover.textColor = textColor;
                style.focused.textColor = textColor;
            }
            style.fontStyle = fontStyle;
            style.richText = isRichText;

            if (buttonRect == Rect.zero)
            {
                if (GUILayout.Button(guiContent, style))
                {
                    if (clickAction != null)
                        clickAction.Invoke();
                }
            }
            else
            {
                if (GUI.Button(buttonRect, guiContent, style))
                {
                    if (clickAction != null)
                        clickAction.Invoke();
                }
            }

            GUI.backgroundColor = oldBGColor;

            if (dragDropEnabled && dropAction != null)
            {
                Event evt = Event.current;
                switch (evt.type)
                {
                    case EventType.DragUpdated:
                    case EventType.DragPerform:
                        if (!buttonRect.Contains(evt.mousePosition))
                            return;

                        DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

                        if (evt.type == EventType.DragPerform)
                        {
                            DragAndDrop.AcceptDrag();

                            foreach (UnityEngine.Object dragged_object in DragAndDrop.objectReferences)
                            {
                                dropAction(dragged_object);
                            }
                        }
                        break;
                }
            }
        }

        public static Button CreateStyledButton(string text, Color baseColor)
        {
            var button = new Button();
            button.text = text;

            // Calculate hover and pressed colors
            Color hoverColor = AdjustBrightness(baseColor, 1.2f);
            Color pressedColor = AdjustBrightness(baseColor, 1.45f);

            // Set base style
            button.style.backgroundColor = baseColor;
            button.style.color = Color.white;
            //button.style.unityFontStyleAndWeight = FontStyle.Bold;
            //button.style.borderBottomWidth = 1;
            //button.style.borderTopWidth = 1;
            //button.style.borderLeftWidth = 1;
            //button.style.borderRightWidth = 1;
            //button.style.borderBottomColor = Color.black;
            //button.style.borderTopColor = Color.black;
            //button.style.borderLeftColor = Color.black;
            //button.style.borderRightColor = Color.black;

            // Register mouse events for interactivity
            button.RegisterCallback<MouseEnterEvent>(_ =>
            {
                button.style.backgroundColor = hoverColor;
            });

            button.RegisterCallback<MouseLeaveEvent>(_ =>
            {
                button.style.backgroundColor = baseColor;
            });

            button.RegisterCallback<PointerDownEvent>(_ =>
            {
                button.style.backgroundColor = pressedColor;
            });

            button.RegisterCallback<PointerUpEvent>(_ =>
            {
                // Assume mouse still over button
                button.style.backgroundColor = hoverColor;
            });

            return button;
        }

        private static Color AdjustBrightness(Color color, float brightnessFactor)
        {
            return new Color(
                Mathf.Clamp01(color.r * brightnessFactor),
                Mathf.Clamp01(color.g * brightnessFactor),
                Mathf.Clamp01(color.b * brightnessFactor),
                color.a
            );
        }

        public static void DrawIMGUIList<T>(ref bool showList, ref List<T> myList, string title, bool isShowSize = true)
        {
            // Create a foldout to toggle the display of the list
            showList = EditorGUILayout.Foldout(showList, title, true);

            if (showList)
            {
                // Ensure the list is not null
                if (myList == null)
                {
                    myList = new();
                }

                if (isShowSize)
                {
                    // Display the list size field
                    int newSize = EditorGUILayout.IntField("Size", myList.Count);

                    // If the size has changed, adjust the list
                    while (newSize > myList.Count)
                    {
                        myList.Add(default);
                    }
                    while (newSize < myList.Count)
                    {
                        myList.RemoveAt(myList.Count - 1);
                    }
                }

                EditorGUI.BeginDisabledGroup(true);
                EditorGUI.indentLevel++;
                // Display each element in the list
                for (int i = 0; i < myList.Count; i++)
                {
                    if (myList[i] is Component component)
                        EditorGUILayout.ObjectField("Element " + i, component, typeof(T), false);
                    else
                        EditorGUILayout.LabelField("Element " + i, myList[i].ToString());
                }
                EditorGUI.indentLevel--;
                EditorGUI.EndDisabledGroup();

                // Optional: Add a button to clear the list
                if (GUILayout.Button("Clear List"))
                {
                    myList.Clear();
                }
                EditorGUILayout.Space(5);
            }
        }

        public static void SetPrototypeSelected(ClickEvent evt, int index, int prototypeCount, List<int> selectedPrototypeIndexes, List<Button> prototypeButtons, Action DrawPrototypeButtons, Func<int, bool> HasError, Func<int, bool> IsNull, Action Callback)
        {
            if (index < 0 || index >= prototypeCount)
            {
                selectedPrototypeIndexes.Clear();
                DrawPrototypeButtons();
                return;
            }
            if (evt != null && evt.shiftKey)
            {
                if (selectedPrototypeIndexes.Count > 0)
                {
                    int lastIndex = selectedPrototypeIndexes[selectedPrototypeIndexes.Count - 1];
                    for (int i = Math.Min(index, lastIndex); i <= Math.Max(index, lastIndex); i++)
                    {
                        if (!selectedPrototypeIndexes.Contains(i))
                            selectedPrototypeIndexes.Add(i);
                    }
                }
                else
                    selectedPrototypeIndexes.Add(index);
            }
            else
            {
                if (!selectedPrototypeIndexes.Contains(index))
                {
                    if (evt == null || !evt.ctrlKey)
                        selectedPrototypeIndexes.Clear();
                    selectedPrototypeIndexes.Add(index);
                }
                else if (selectedPrototypeIndexes.Count > 1)
                {
                    if (evt == null || !evt.ctrlKey)
                    {
                        selectedPrototypeIndexes.Clear();
                        selectedPrototypeIndexes.Add(index);
                    }
                    else
                        selectedPrototypeIndexes.Remove(index);
                }
                else
                    selectedPrototypeIndexes.Clear();
            }

            for (int i = 0; i < prototypeButtons.Count; i++)
            {
                prototypeButtons[i].RemoveFromClassList("gpui-prototype-button-selected");
                prototypeButtons[i].RemoveFromClassList("gpui-prototype-button-error-selected");

                if (HasError(i))
                    prototypeButtons[i].AddToClassList("gpui-prototype-button-error");
            }

            for (int i = 0; i < selectedPrototypeIndexes.Count; i++)
            {
                int pi = selectedPrototypeIndexes[i];
                if (IsNull(pi))
                {
                    selectedPrototypeIndexes.RemoveAt(i);
                    DrawPrototypeButtons();
                    return;
                }
                if (HasError(pi))
                    prototypeButtons[pi].AddToClassList("gpui-prototype-button-error-selected");
                else if (selectedPrototypeIndexes.Contains(pi))
                    prototypeButtons[pi].AddToClassList("gpui-prototype-button-selected");
            }
            selectedPrototypeIndexes.Sort();
            Callback();
        }

        #endregion Custom Editor

        #region Version Control

        public static void VersionControlCheckout(UnityEngine.Object assetObject)
        {
            if (UnityEditor.VersionControl.Provider.enabled && UnityEditor.VersionControl.Provider.isActive)
            {
                VersionControlCheckout(AssetDatabase.GetAssetPath(assetObject));
            }
        }

        public static void VersionControlCheckout(string path)
        {
            if (UnityEditor.VersionControl.Provider.enabled && UnityEditor.VersionControl.Provider.isActive)
            {
                UnityEditor.VersionControl.Asset asset = UnityEditor.VersionControl.Provider.GetAssetByPath(path);
                if (asset == null)
                    return;

                if (UnityEditor.VersionControl.Provider.hasCheckoutSupport)
                {
                    UnityEditor.VersionControl.Task checkOutTask = UnityEditor.VersionControl.Provider.Checkout(asset, UnityEditor.VersionControl.CheckoutMode.Both);
                    checkOutTask.Wait();
                }
            }
        }

        #endregion Version Control

        #region UI Elements

        public static bool DrawMatrix4x4Fields(Rect rect, SerializedProperty matrix4x4Property, bool foldoutValue)
        {
            float fieldHeight = EditorGUIUtility.singleLineHeight * 2;
            float y = rect.y;
            foldoutValue = EditorGUI.Foldout(new Rect(rect.x, y, 200, EditorGUIUtility.singleLineHeight), foldoutValue, matrix4x4Property.displayName);
            y += EditorGUIUtility.singleLineHeight;

            if (foldoutValue)
            {
                Matrix4x4 matrix = GetMatrixValue(matrix4x4Property);

                const int decimals = 3;
                Vector3 position = matrix.GetPosition();
                Vector3 rotation = matrix.rotation.eulerAngles.Round(decimals);
                Vector3 scale = matrix.lossyScale.Round(decimals);

                EditorGUI.BeginChangeCheck();
                position = EditorGUI.Vector3Field(new Rect(rect.x, y, 300, fieldHeight), "Position", position);
                y += fieldHeight;
                rotation = EditorGUI.Vector3Field(new Rect(rect.x, y, 300, fieldHeight), "Rotation", rotation).Round(decimals);
                y += fieldHeight;
                scale = EditorGUI.Vector3Field(new Rect(rect.x, y, 300, fieldHeight), "Scale", scale).Round(decimals);
                if (EditorGUI.EndChangeCheck())
                    SetMatrix4x4PropertyValues(matrix4x4Property, Matrix4x4.TRS(position, Quaternion.Euler(rotation), scale));
            }

            return foldoutValue;
        }

        public static float OnMatrixListElementHeight(bool isSelected)
        {
            if (isSelected)
                return EditorGUIUtility.singleLineHeight * 7 + 5;
            return EditorGUIUtility.singleLineHeight;
        }

        public static VisualElement DrawMatrix4x4Fields(SerializedProperty matrix4x4Property)
        {
            Foldout foldout = new();
            foldout.text = matrix4x4Property.displayName;
            foldout.value = false;

            Matrix4x4 matrix = GetMatrixValue(matrix4x4Property);

            Vector3Field positionField = new("Position");
            positionField.value = matrix.GetPosition();
            Vector3Field rotationField = new("Rotation");
            rotationField.value = matrix.rotation.eulerAngles;
            Vector3Field scaleField = new("Scale");
            scaleField.value = matrix.lossyScale;

            void ValueChanged(ChangeEvent<Vector3> evt) => SetMatrix4x4PropertyValues(matrix4x4Property, Matrix4x4.TRS(positionField.value, Quaternion.Euler(rotationField.value), scaleField.value));

            positionField.RegisterValueChangedCallback(ValueChanged);
            rotationField.RegisterValueChangedCallback(ValueChanged);
            scaleField.RegisterValueChangedCallback(ValueChanged);

            foldout.Add(positionField);
            foldout.Add(rotationField);
            foldout.Add(scaleField);

            return foldout;
        }

        public static Matrix4x4 GetMatrixValue(SerializedProperty matrix4x4Property)
        {
            Matrix4x4 matrix = Matrix4x4.identity;
            for (int r = 0; r < 4; r++)
            {
                for (int c = 0; c < 4; c++)
                {
                    matrix[r, c] = matrix4x4Property.FindPropertyRelative("e" + r + c).floatValue;
                }
            }
            if (matrix == Matrix4x4.zero)
            {
                matrix = Matrix4x4.identity;
                SetMatrix4x4PropertyValues(matrix4x4Property, matrix);
            }

            return matrix;
        }

        public static void SetMatrix4x4PropertyValues(SerializedProperty matrix4x4Property, Matrix4x4 matrix4X4)
        {
            for (int r = 0; r < 4; r++)
            {
                for (int c = 0; c < 4; c++)
                {
                    matrix4x4Property.FindPropertyRelative("e" + r + c).floatValue = matrix4X4[r, c];
                }
            }
            matrix4x4Property.serializedObject.ApplyModifiedProperties();
        }

        public static void ChangeVisibilityWithToggle(PropertyField toggleField, VisualElement visibilityElement)
        {
            toggleField.RegisterCallback<SerializedPropertyChangeEvent>((evt) => { if (evt.changedProperty.boolValue) visibilityElement.AddToClassList("gpui-hidden"); else visibilityElement.RemoveFromClassList("gpui-hidden"); });
        }

        public static void SetVisible(this VisualElement visualElement, bool isVisible)
        {
            visualElement.style.display = isVisible ? DisplayStyle.Flex : DisplayStyle.None;
        }

        public static void SetHidden(this VisualElement visualElement, bool isHidden)
        {
            visualElement.style.visibility = isHidden ? Visibility.Hidden : Visibility.Visible;
        }

        public static bool IsVisible(this VisualElement visualElement)
        {
            return visualElement.style.display == DisplayStyle.Flex;
        }

        #endregion UI Elements

        #region Prefab System
        public static bool IsPrefabAsset(UnityEngine.Object pickerObject, out GameObject prefabObject, string warningTextCode, bool acceptModelPrefab)
        {
            return GPUIPrefabUtility.IsPrefabAsset(pickerObject, out prefabObject, acceptModelPrefab, warningTextCode, DisplayDialog);
        }
        #endregion Prefab System

        #region Mesh 

        private static MethodInfo _intersectRayMeshMethod;
        public static bool IntersectRayMesh(Ray ray, Mesh mesh, Matrix4x4 matrix, out RaycastHit hit)
        {
            if (_intersectRayMeshMethod == null)
                _intersectRayMeshMethod = typeof(HandleUtility).GetMethod("IntersectRayMesh", (BindingFlags.Static | BindingFlags.NonPublic));

            bool result = false;
            hit = default;
            if (_intersectRayMeshMethod != null)
            {
                var parameters = new object[] { ray, mesh, matrix, null };
                result = (bool)_intersectRayMeshMethod.Invoke(null, parameters);
                hit = (RaycastHit)parameters[3];
            }
            return result;
        }

        #endregion Mesh 

        #region Extensions

        private static object GetPropertyValue(this SerializedProperty property)
        {
            return property.propertyType switch
            {
                SerializedPropertyType.Integer => property.intValue,
                SerializedPropertyType.Boolean => property.boolValue,
                SerializedPropertyType.Float => property.floatValue,
                SerializedPropertyType.String => property.stringValue,
                SerializedPropertyType.Color => property.colorValue,
                SerializedPropertyType.ObjectReference => property.objectReferenceValue,
                SerializedPropertyType.LayerMask => property.intValue,
                SerializedPropertyType.Enum => property.enumValueIndex,
                SerializedPropertyType.Vector2 => property.vector2Value,
                SerializedPropertyType.Vector3 => property.vector3Value,
                SerializedPropertyType.Vector4 => property.vector4Value,
                SerializedPropertyType.Rect => property.rectValue,
                SerializedPropertyType.ArraySize => property.arraySize,
                SerializedPropertyType.Character => property.intValue,
                SerializedPropertyType.AnimationCurve => property.animationCurveValue,
                SerializedPropertyType.Bounds => property.boundsValue,
                SerializedPropertyType.Gradient => GetGradient(property),
                SerializedPropertyType.Quaternion => property.quaternionValue,
                SerializedPropertyType.ExposedReference => property.exposedReferenceValue,
                SerializedPropertyType.FixedBufferSize => property.fixedBufferSize,
                SerializedPropertyType.Vector2Int => property.vector2IntValue,
                SerializedPropertyType.Vector3Int => property.vector3IntValue,
                SerializedPropertyType.RectInt => property.rectIntValue,
                SerializedPropertyType.BoundsInt => property.boundsIntValue,
                SerializedPropertyType.ManagedReference => property.managedReferenceValue,
                SerializedPropertyType.Hash128 => property.hash128Value,
                _ => null
            };
        }

        private static void SetPropertyValue(this SerializedProperty property, object val)
        {
            switch (property.propertyType)
            {
                case SerializedPropertyType.Integer:
                    property.intValue = (int)val;
                    break;
                case SerializedPropertyType.Boolean:
                    property.boolValue = (bool)val;
                    break;
                case SerializedPropertyType.Float:
                    property.floatValue = (float)val;
                    break;
                case SerializedPropertyType.String:
                    property.stringValue = (string)val;
                    break;
                case SerializedPropertyType.Color:
                    property.colorValue = (Color)val;
                    break;
                case SerializedPropertyType.ObjectReference:
                    property.objectReferenceValue = (UnityEngine.Object)val;
                    break;
                case SerializedPropertyType.LayerMask:
                    property.intValue = (int)val;
                    break;
                case SerializedPropertyType.Enum:
                    property.enumValueIndex = (int)val;
                    break;
                case SerializedPropertyType.Vector2:
                    property.vector2Value = (Vector2)val;
                    break;
                case SerializedPropertyType.Vector3:
                    property.vector3Value = (Vector3)val;
                    break;
                case SerializedPropertyType.Vector4:
                    property.vector4Value = (Vector4)val;
                    break;
                case SerializedPropertyType.Rect:
                    property.rectValue = (Rect)val;
                    break;
                case SerializedPropertyType.ArraySize:
                    property.arraySize = (int)val;
                    break;
                case SerializedPropertyType.Character:
                    property.intValue = (int)val;
                    break;
                case SerializedPropertyType.AnimationCurve:
                    property.animationCurveValue = (AnimationCurve)val;
                    break;
                case SerializedPropertyType.Bounds:
                    property.boundsValue = (Bounds)val;
                    break;
                //case SerializedPropertyType.Gradient:
                //    property.intValue = (int)val;
                //    break;
                case SerializedPropertyType.Quaternion:
                    property.quaternionValue = (Quaternion)val;
                    break;
                case SerializedPropertyType.ExposedReference:
                    property.exposedReferenceValue = (UnityEngine.Object)val;
                    break;
                //case SerializedPropertyType.FixedBufferSize:
                //    property.intValue = (int)val;
                //    break;
                case SerializedPropertyType.Vector2Int:
                    property.vector2IntValue = (Vector2Int)val;
                    break;
                case SerializedPropertyType.Vector3Int:
                    property.vector3IntValue = (Vector3Int)val;
                    break;
                case SerializedPropertyType.RectInt:
                    property.rectIntValue = (RectInt)val;
                    break;
                case SerializedPropertyType.BoundsInt:
                    property.boundsIntValue = (BoundsInt)val;
                    break;
                case SerializedPropertyType.ManagedReference:
                    property.managedReferenceValue = val;
                    break;
                case SerializedPropertyType.Hash128:
                    property.hash128Value = (Hash128)val;
                    break;
            }
        }

        private static Gradient GetGradient(SerializedProperty gradientProperty)
        {
            PropertyInfo propertyInfo = typeof(SerializedProperty).GetProperty("gradientValue",
                BindingFlags.NonPublic | BindingFlags.Instance);

            if (propertyInfo == null) return null;

            return propertyInfo.GetValue(gradientProperty, null) as Gradient;
        }

        public static bool TryParseVersionArray(string version, out int[] versionArray)
        {
            string[] versionSplit = version.Split('.');
            versionArray = new int[3];
            if (versionSplit.Length != 3)
                return false;
            for (int i = 0; i < 3; i++)
            {
                if (int.TryParse(versionSplit[i], out int r))
                    versionArray[i] = r;
                else
                    return false;
            }
            return true;
        }

        public static bool IsValidPrototype(SerializedProperty prototypeSP)
        {
            return prototypeSP.FindPropertyRelative("isValid").boolValue;
        }

        public static System.Object GetTargetObjectFromPath(this SerializedProperty property)
        {
            var path = property.propertyPath.Replace(".Array.data[", "[");
            //Debug.Log(path);

            System.Object targetObject = property.serializedObject.targetObject;
            Type targetObjectType = targetObject.GetType();

            string[] fieldNames = path.Split('.');
            for (int i = 0; i < fieldNames.Length; i++)
            {
                string fieldName = fieldNames[i];
                int arrayIndex = -1;
                if (fieldName.Contains("["))
                {
                    arrayIndex = Convert.ToInt32(fieldName.Substring(fieldName.IndexOf("[")).Replace("[", "").Replace("]", ""));
                    fieldName = fieldName.Substring(0, fieldName.IndexOf("["));
                }
                FieldInfo info = targetObjectType.GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (info == null)
                    break;

                targetObject = info.GetValue(targetObject);
                targetObjectType = targetObject.GetType();

                if (arrayIndex >= 0)
                {
                    IEnumerable enumerable = targetObject as IEnumerable;
                    IEnumerator enumerator = enumerable.GetEnumerator();
                    do
                    {
                        enumerator.MoveNext();
                        arrayIndex--;
                    }
                    while (arrayIndex >= 0);

                    targetObject = enumerator.Current;
                    targetObjectType = targetObject.GetType();
                }
            }

            return targetObject;
        }

        public static void RegisterValueChangedCallbackDelayed(this PropertyField propertyField, EventCallback<SerializedPropertyChangeEvent> callback)
        {
            propertyField.schedule.Execute(() => propertyField.RegisterValueChangeCallback(callback));
        }

        public static void RegisterCallbackDelayed<TEventType>(this VisualElement visualElement, EventCallback<TEventType> callback) where TEventType : EventBase<TEventType>, new()
        {
            visualElement.schedule.Execute(() => visualElement.RegisterCallback(callback));
        }

        public static void SaveToTextFile(this string text, string filePath)
        {
            byte[] bytes = System.Text.Encoding.UTF8.GetBytes(text);
            VersionControlCheckout(filePath);
            System.IO.FileStream fs = System.IO.File.Create(filePath);
            fs.Write(bytes, 0, bytes.Length);
            fs.Close();
            AssetDatabase.ImportAsset(filePath, ImportAssetOptions.ForceUpdate);
            AssetDatabase.Refresh();
        }

        public static void SetToolbarButtonActive(this ToolbarButton toolbarButton, bool active)
        {
            if (active)
            {
                toolbarButton.SetEnabled(false);
                toolbarButton.RemoveFromClassList("unity-disabled");
                toolbarButton.AddToClassList("gpui-prototype-list-toolbar-button-active");
            }
            else
            {
                toolbarButton.SetEnabled(true);
                toolbarButton.RemoveFromClassList("gpui-prototype-list-toolbar-button-active");
            }
        }

        public static void ShowRSGDebugActionsMenu(GPUIRenderSourceGroup renderSourceGroup)
        {
            if (renderSourceGroup == null)
                return;
            GenericMenu menu = new GenericMenu();
            if (renderSourceGroup.IsLODColorDebuggingEnabled)
            {
                menu.AddItem(new GUIContent("Disable LOD Color Debugging"), false, () =>
                {
                    renderSourceGroup.SetLODColorDebuggingEnabled(false);
                });
            }
            else
            {
                menu.AddItem(new GUIContent("Enable LOD Color Debugging"), false, () =>
                {
                    renderSourceGroup.SetLODColorDebuggingEnabled(true);
                });
            }
            menu.ShowAsContext();
        }

        public static void WaitAndExecute(float seconds, Action action)
        {
            double startTime = EditorApplication.timeSinceStartup;

            void OnEditorUpdate()
            {
                if (EditorApplication.timeSinceStartup - startTime >= seconds)
                {
                    EditorApplication.update -= OnEditorUpdate;
                    action?.Invoke();
                }
            }

            EditorApplication.update += OnEditorUpdate;
        }

        #endregion Extensions
    }
}