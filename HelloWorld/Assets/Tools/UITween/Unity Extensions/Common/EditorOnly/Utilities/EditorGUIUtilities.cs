#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace UnityExtensions.Editor
{
    /// <summary>
    /// Menu item states.
    /// </summary>
    public enum MenuItemState
    {
        Normal,
        Selected,
        Disabled,
    }


    /// <summary>
    /// Utilities for editor GUI.
    /// </summary>
    public partial struct EditorGUIUtilities
    {
        static GUIContent _tempContent = new GUIContent();

        static GUIStyle _buttonStyle;
        static GUIStyle _buttonLeftStyle;
        static GUIStyle _buttonMiddleStyle;
        static GUIStyle _buttonRightStyle;
        static GUIStyle _middleCenterLabelStyle;
        static GUIStyle _middleLeftLabelStyle;

        static Texture2D _paneOptionsIconDark;
        static Texture2D _paneOptionsIconLight;

        static int _dragState;
        static Vector2 _dragPos;


        public static GUIStyle buttonStyle
        {
            get
            {
                if (_buttonStyle == null) _buttonStyle = "Button";
                return _buttonStyle;
            }
        }


        public static GUIStyle buttonLeftStyle
        {
            get
            {
                if (_buttonLeftStyle == null) _buttonLeftStyle = "ButtonLeft";
                return _buttonLeftStyle;
            }
        }


        public static GUIStyle buttonMiddleStyle
        {
            get
            {
                if (_buttonMiddleStyle == null) _buttonMiddleStyle = "ButtonMid";
                return _buttonMiddleStyle;
            }
        }


        public static GUIStyle buttonRightStyle
        {
            get
            {
                if (_buttonRightStyle == null) _buttonRightStyle = "ButtonRight";
                return _buttonRightStyle;
            }
        }


        public static GUIStyle middleCenterLabelStyle
        {
            get
            {
                if (_middleCenterLabelStyle == null)
                {
                    _middleCenterLabelStyle = new GUIStyle(EditorStyles.label);
                    _middleCenterLabelStyle.alignment = TextAnchor.MiddleCenter;
                }
                return _middleCenterLabelStyle;
            }
        }


        public static GUIStyle middleLeftLabelStyle
        {
            get
            {
                if (_middleLeftLabelStyle == null)
                {
                    _middleLeftLabelStyle = new GUIStyle(EditorStyles.label);
                    _middleLeftLabelStyle.alignment = TextAnchor.MiddleLeft;
                }
                return _middleLeftLabelStyle;
            }
        }


        public static Texture2D paneOptionsIcon
        {
            get
            {
                return EditorGUIUtility.isProSkin ? paneOptionsIconDark : paneOptionsIconLight;
            }
        }


        public static Texture2D paneOptionsIconDark
        {
            get
            {
                if (_paneOptionsIconDark == null)
                {
                    _paneOptionsIconDark = (Texture2D)EditorGUIUtility.Load("Builtin Skins/DarkSkin/Images/pane options.png");
                }
                return _paneOptionsIconDark;
            }
        }


        public static Texture2D paneOptionsIconLight
        {
            get
            {
                if (_paneOptionsIconLight == null)
                {
                    _paneOptionsIconLight = (Texture2D)EditorGUIUtility.Load("Builtin Skins/LightSkin/Images/pane options.png");
                }
                return _paneOptionsIconLight;
            }
        }


        public static Color labelNormalColor
        {
            get { return EditorStyles.label.normal.textColor; }
        }


        /// <summary>
        /// Get a temporary GUIContent（use this to avoid GC).
        /// </summary>
        public static GUIContent TempContent(string text = null, Texture image = null, string tooltip = null)
        {
            _tempContent.text = text;
            _tempContent.image = image;
            _tempContent.tooltip = tooltip;

            return _tempContent;
        }


        /// <summary>
        /// Draw a rect wireframe.
        /// </summary>
        public static void DrawWireRect(Rect rect, Color color, float borderWidth = 1f)
        {
            Rect border = new Rect(rect.x, rect.y, rect.width, borderWidth);
            EditorGUI.DrawRect(border, color);
            border.y = rect.yMax - borderWidth;
            EditorGUI.DrawRect(border, color);
            border.yMax = border.yMin;
            border.yMin = rect.yMin + borderWidth;
            border.width = borderWidth;
            EditorGUI.DrawRect(border, color);
            border.x = rect.xMax - borderWidth;
            EditorGUI.DrawRect(border, color);
        }


        /// <summary>
        /// Draw a indented button.
        /// </summary>
        public static bool IndentedButton(string text)
        {
            var rect = EditorGUILayout.GetControlRect(true);
            rect.xMin += EditorGUIUtility.labelWidth;
            return GUI.Button(rect, text, EditorStyles.miniButton);
        }


        /// <summary>
        /// Draw a indented toggle-button.
        /// </summary>
        public static bool IndentedToggleButton(string text, bool value)
        {
            var rect = EditorGUILayout.GetControlRect(true);
            rect.xMin += EditorGUIUtility.labelWidth;
            return GUI.Toggle(rect, value, text, EditorStyles.miniButton);
        }


        public static Vector2 SingleLineVector2Field(Rect rect, Vector2 value, GUIContent label)
        {
            rect = EditorGUI.PrefixLabel(rect, label);
            using (LabelWidthScope.New(14))
            {
                using (IndentLevelScope.New(0, false))
                {
                    rect.width = (rect.width - 4) * 0.5f;
                    value.x = EditorGUI.FloatField(rect, "X", value.x);
                    rect.x = rect.xMax + 4;
                    value.y = EditorGUI.FloatField(rect, "Y", value.y);
                }
            }
            return value;
        }


        public static Vector2 SingleLineVector2Field(Rect rect, Vector2 value, GUIContent label, float aspectRatio)
        {
            rect = EditorGUI.PrefixLabel(rect, label);
            using (LabelWidthScope.New(14))
            {
                using (IndentLevelScope.New(0, false))
                {
                    rect.width = (rect.width - 4) * 0.5f;
                    using (var scope = ChangeCheckScope.New())
                    {
                        value.x = EditorGUI.FloatField(rect, "X", value.x);
                        if (scope.changed) value.y = value.x / aspectRatio;
                    }

                    rect.x = rect.xMax + 4;
                    using (var scope = ChangeCheckScope.New())
                    {
                        value.y = EditorGUI.FloatField(rect, "Y", value.y);
                        if (scope.changed) value.x = value.y * aspectRatio;
                    }
                }
            }
            return value;
        }


        public static void SingleLineVector2Field(Rect rect, SerializedProperty property, GUIContent label)
        {
            property.vector2Value = SingleLineVector2Field(rect, property.vector2Value, label);
        }


        public static void SingleLineVector2Field(Rect rect, SerializedProperty property, GUIContent label, float aspectRatio)
        {
            property.vector2Value = SingleLineVector2Field(rect, property.vector2Value, label, aspectRatio);
        }


        public static Vector2 SingleLineVector2FieldLayout(Vector2 value, GUIContent label)
        {
            return SingleLineVector2Field(EditorGUILayout.GetControlRect(true, EditorGUIUtility.singleLineHeight), value, label);
        }


        public static void SingleLineVector2FieldLayout(SerializedProperty property, GUIContent label)
        {
            property.vector2Value = SingleLineVector2FieldLayout(property.vector2Value, label);
        }


        /// <summary>
        /// Drag mouse to change value.
        /// </summary>
        public static float DragValue(Rect rect, float value, float sensitivity)
        {
            int id = GUIUtility.GetControlID(FocusType.Passive);
            Event current = Event.current;

            switch (current.GetTypeForControl(id))
            {
                case EventType.MouseDown:
                    if (rect.Contains(current.mousePosition) && current.button == 0)
                    {
                        EditorGUIUtility.editingTextField = false;
                        GUIUtility.hotControl = id;
                        _dragState = 1;
                        _dragPos = current.mousePosition;
                        current.Use();
                        EditorGUIUtility.SetWantsMouseJumping(1);
                    }
                    break;

                case EventType.MouseUp:
                    if (GUIUtility.hotControl == id && _dragState != 0)
                    {
                        GUIUtility.hotControl = 0;
                        _dragState = 0;
                        current.Use();
                        EditorGUIUtility.SetWantsMouseJumping(0);
                    }
                    break;

                case EventType.MouseDrag:
                    if (GUIUtility.hotControl != id)
                    {
                        break;
                    }
                    switch (_dragState)
                    {
                        case 1:
                            if ((Event.current.mousePosition - _dragPos).sqrMagnitude > 4)
                            {
                                _dragState = 2;
                            }
                            current.Use();
                            break;

                        case 2:
                            value += HandleUtility.niceMouseDelta * sensitivity;
                            value = MathUtilities.RoundToSignificantDigitsFloat(value, 6);
                            GUI.changed = true;
                            current.Use();
                            break;
                    }
                    break;

                case EventType.Repaint:
                    EditorGUIUtility.AddCursorRect(rect, MouseCursor.SlideArrow);
                    break;
            }

            return value;
        }


        /// <summary>
        /// Drag mouse to change value.
        /// </summary>
        public static float DragValue(Rect rect, GUIContent content, float value, float sensitivity, GUIStyle style)
        {
            GUI.Label(rect, content, style);
            return DragValue(rect, value, sensitivity);
        }


        /// <summary>
        /// Draw a progress bar that can be dragged.
        /// </summary>
        public static float DragProgress(
            Rect rect,
            float value01,
            Color backgroundColor,
            Color foregroundColor,
            bool draggable = true)
        {
            var progressRect = rect;
            progressRect.width = Mathf.Round(progressRect.width * value01);

            EditorGUI.DrawRect(rect, backgroundColor);
            EditorGUI.DrawRect(progressRect, foregroundColor);

            int id = GUIUtility.GetControlID(FocusType.Passive);
            Event current = Event.current;

            switch (current.GetTypeForControl(id))
            {
                case EventType.MouseDown:
                    if (rect.Contains(current.mousePosition) && current.button == 0)
                    {
                        EditorGUIUtility.editingTextField = false;
                        GUIUtility.hotControl = id;
                        _dragState = 1;

                        if (draggable)
                        {
                            float offset = current.mousePosition.x - rect.x + 1f;
                            value01 = Mathf.Clamp01(offset / rect.width);
                        }

                        GUI.changed = true;

                        current.Use();
                    }
                    break;

                case EventType.MouseUp:
                    if (GUIUtility.hotControl == id && _dragState != 0)
                    {
                        GUIUtility.hotControl = 0;
                        _dragState = 0;
                        current.Use();
                    }
                    break;

                case EventType.MouseDrag:
                    if (GUIUtility.hotControl != id)
                    {
                        break;
                    }
                    if (_dragState != 0)
                    {
                        if (draggable)
                        {
                            float offset = current.mousePosition.x - rect.x + 1f;
                            value01 = Mathf.Clamp01(offset / rect.width);
                        }

                        GUI.changed = true;

                        current.Use();
                    }
                    break;

                case EventType.Repaint:
                    if (draggable) EditorGUIUtility.AddCursorRect(rect, MouseCursor.SlideArrow);
                    break;
            }

            return value01;
        }


        /// <summary>
        /// Draw a progress bar that can be dragged.
        /// </summary>
        public static float DragProgress(
            Rect rect,
            float value01,
            Color backgroundColor,
            Color foregroundColor,
            Color borderColor,
            bool drawForegroundBorder = false,
            bool draggable = true)
        {
            float result = DragProgress(rect, value01, backgroundColor, foregroundColor, draggable);

            DrawWireRect(rect, borderColor);

            if (drawForegroundBorder)
            {
                rect.width = Mathf.Round(rect.width * value01);
                if (rect.width > 0f)
                {
                    rect.xMin = rect.xMax - 1f;
                    EditorGUI.DrawRect(rect, borderColor);
                }
            }

            return result;
        }


        /// <summary>
        /// Create a menu.
        /// </summary>
        /// <param name="itemCount"> Number of items, include separators and childs. </param>
        /// <param name="getItemContent"> Get content of an item, a separator must ends with '/' </param>
        /// <param name="getItemState"> Get state of an item </param>
        /// <returns> The created menu, use DropDown or ShowAsContext to show it. </returns>
        public static GenericMenu CreateMenu(
            int itemCount,
            Func<int, GUIContent> getItemContent,
            Func<int, MenuItemState> getItemState,
            Action<int> onSelect)
        {
            GenericMenu menu = new GenericMenu();
            GUIContent content;
            MenuItemState state;

            for(int i=0; i<itemCount; i++)
            {
                content = getItemContent(i);
                if(content.text.EndsWith("/"))
                {
                    menu.AddSeparator(content.text.Substring(0, content.text.Length - 1));
                }
                else
                {
                    state = getItemState(i);
                    if(state == MenuItemState.Disabled)
                    {
                        menu.AddDisabledItem(content);
                    }
                    else
                    {
                        int index = i;
                        menu.AddItem(content, state == MenuItemState.Selected, () => onSelect(index));
                    }
                }
            }

            return menu;
        }


        /// <summary>
        /// Create a menu.
        /// </summary>
        /// <param name="itemCount"> Number of items, include separators and childs. </param>
        /// <param name="getItemContent"> Get content of an item, a separator must ends with '/' </param>
        /// <param name="getItemState"> Get state of an item </param>
        /// <returns> The created menu, use DropDown or ShowAsContext to show it. </returns>
        public static GenericMenu CreateMenu<T>(
            IEnumerable<T> items,
            Func<T, GUIContent> getItemContent,
            Func<T, MenuItemState> getItemState,
            Action<T> onSelect)
        {
            GenericMenu menu = new GenericMenu();
            GUIContent content;
            MenuItemState state;

            foreach (var i in items)
            {
                content = getItemContent(i);
                if (content.text.EndsWith("/"))
                {
                    menu.AddSeparator(content.text.Substring(0, content.text.Length - 1));
                }
                else
                {
                    state = getItemState(i);
                    if (state == MenuItemState.Disabled)
                    {
                        menu.AddDisabledItem(content);
                    }
                    else
                    {
                        T current = i;
                        menu.AddItem(content, state == MenuItemState.Selected, () => onSelect(current));
                    }
                }
            }

            return menu;
        }

    } // struct EditorGUIUtilities

} // namespace UnityExtensions.Editor

#endif // UNITY_EDITOR