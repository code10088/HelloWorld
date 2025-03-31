#if UNITY_EDITOR

using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityExtensions.Editor;

namespace UnityExtensions.Tween.Editor
{
    /// <summary>
    /// Interpolator Drawer
    /// </summary>
    [CustomPropertyDrawer(typeof(Interpolator))]
    [CustomPropertyDrawer(typeof(CustomizableInterpolator))]
    class InterpolatorDrawer : PropertyDrawer
    {
        // 最后采样时的数据缓存
        int _lastType;
        float _lastStrength;

        float _minValue, _maxValue;
        List<Vector3> _samples = new List<Vector3>(64);


        static GUIStyle _buttonStype;
        public static GUIStyle buttonStyle
        {
            get
            {
                if (_buttonStype == null)
                {
                    _buttonStype = new GUIStyle(GUIStyle.none);
                    _buttonStype.clipping = TextClipping.Clip;
                }
                return _buttonStype;
            }
        }


        // 采样
        void Sample(int type, float strength, int maxSegments, float maxError)
        {
            if (_samples.Count == 0
                    || type != _lastType
                    || strength != _lastStrength)
            {
                _lastType = type;
                _lastStrength = strength;
                _samples.Clear();

                var interpolator = new Interpolator((Interpolator.Type)type, strength);

                // 添加第一个点

                Vector3 point = new Vector3(0, interpolator[0]);
                _samples.Add(point);

                // 添加其他点

                Vector3 lastSample = point, lastEvaluate = point;
                _minValue = _maxValue = point.y;

                float minSlope = float.MinValue;
                float maxSlope = float.MaxValue;

                for (int i = 1; i <= maxSegments; i++)
                {
                    point.x = i / (float)maxSegments;
                    point.y = interpolator[point.x];

                    if (_minValue > point.y) _minValue = point.y;
                    if (_maxValue < point.y) _maxValue = point.y;

                    maxSlope = Mathf.Min((point.y - lastSample.y + maxError) / (point.x - lastSample.x), maxSlope);
                    minSlope = Mathf.Max((point.y - lastSample.y - maxError) / (point.x - lastSample.x), minSlope);

                    if (minSlope >= maxSlope)
                    {
                        _samples.Add(lastSample = lastEvaluate);
                        maxSlope = (point.y - lastSample.y + maxError) / (point.x - lastSample.x);
                        minSlope = (point.y - lastSample.y - maxError) / (point.x - lastSample.x);
                    }

                    lastEvaluate = point;
                }

                // 添加最后一个点

                _samples.Add(point);
                if (_minValue > point.y) _minValue = point.y;
                if (_maxValue < point.y) _maxValue = point.y;

                // 计算绘制的边界值

                if (_maxValue - _minValue < 1f)
                {
                    if (_minValue < 0f)
                    {
                        _maxValue = _minValue + 1f;
                    }
                    else if (_maxValue > 1f)
                    {
                        _minValue = _maxValue - 1f;
                    }
                    else
                    {
                        _minValue = 0f;
                        _maxValue = 1f;
                    }
                }
            }
        }


        // 绘制曲线
        void DrawCurve(Rect rect, bool drawStrength)
        {
            EditorGUI.DrawRect(rect, new Color(0.2f, 0.2f, 0.2f));

            Vector2 origin = new Vector2(rect.x + 1, rect.y + 1);
            Vector2 scale = new Vector2(rect.width - 2, (rect.height - 2) / (_maxValue - _minValue));

            if (_maxValue > 0f && _minValue < 1f)
            {
                float yMin = origin.y + (_maxValue - Mathf.Min(_maxValue, 1f)) * scale.y;
                float yMax = origin.y + (_maxValue - Mathf.Max(_minValue, 0f)) * scale.y;
                Rect rect01 = new Rect(rect.x, yMin, rect.width, yMax - yMin);
                EditorGUI.DrawRect(rect01, new Color(1f, 1f, 1f, 0.15f));
            }

            if (drawStrength)
            {
                EditorGUI.DrawRect(new Rect(rect.x + (rect.width - 1) * _lastStrength, rect.y, 1, rect.height), new Color(1, 0.33f, 0));
            }

            Vector3 last = _samples[0];
            last.x = origin.x + last.x * scale.x;
            last.y = origin.y + (_maxValue - last.y) * scale.y;

            using (HandlesColorScope.New(new Color(0f, 1f, 0f)))
            {
                Vector3 point;

                for (int i = 1; i < _samples.Count; i++)
                {
                    point = _samples[i];
                    point.x = origin.x + point.x * scale.x;
                    point.y = origin.y + (_maxValue - point.y) * scale.y;

                    HandlesUtilities.DrawAALine(last, point);
                    last = point;
                }
            }

            EditorGUIUtilities.DrawWireRect(rect, new Color(0, 0, 0, 0.4f));
        }


        public override bool CanCacheInspectorGUI(SerializedProperty property)
        {
            return false;
        }


        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUIUtility.singleLineHeight * 2 + 2;
        }


        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            position = EditorGUI.PrefixLabel(position, label);

            var typeProp = property.FindPropertyRelative("type");
            int type = typeProp.intValue;
            var strengthProp = property.FindPropertyRelative("strength");

            var buttonRect = new Rect(position.x, position.y, EditorGUIUtilities.paneOptionsIconDark.width, EditorGUIUtilities.paneOptionsIconDark.height);
            using (var scope = ChangeCheckScope.New())
            {
                EditorGUIUtility.AddCursorRect(buttonRect, MouseCursor.Arrow);

                System.Enum newType;
                if (fieldInfo.FieldType == typeof(CustomizableInterpolator))
                {
                    newType = EditorGUI.EnumPopup(buttonRect, GUIContent.none, (CustomizableInterpolator.Type)type, buttonStyle);
                }
                else
                {
                    newType = EditorGUI.EnumPopup(buttonRect, GUIContent.none, (Interpolator.Type)type, buttonStyle);
                }

                if (scope.changed)
                {
                    typeProp.intValue = type = (int)(CustomizableInterpolator.Type)newType;
                    strengthProp.floatValue = 0.5f;
                }
            }

            if ((CustomizableInterpolator.Type)type == CustomizableInterpolator.Type.CustomCurve)
            {
                EditorGUIUtility.AddCursorRect(position, MouseCursor.Zoom);
                EditorGUI.PropertyField(position, property.FindPropertyRelative("customCurve"), GUIContent.none);
            }
            else
            {
                bool drawStrength;

                switch ((CustomizableInterpolator.Type)type)
                {
                    case CustomizableInterpolator.Type.Linear:
                    case CustomizableInterpolator.Type.Parabolic:
                    case CustomizableInterpolator.Type.Sine:
                        drawStrength = false;
                        break;

                    default:
                        strengthProp.floatValue = EditorGUIUtilities.DragProgress(position, strengthProp.floatValue, default, default);
                        drawStrength = true;
                        break;
                }

                if (Event.current.type == EventType.Repaint)
                {
                    Sample(type, strengthProp.floatValue, Mathf.Min((int)position.width, 256), 0.002f);
                    DrawCurve(position, drawStrength);
                }
            }

            EditorGUI.LabelField(buttonRect, EditorGUIUtilities.TempContent(image: EditorGUIUtilities.paneOptionsIconDark), GUIStyle.none);
        }

    } // class InterpolatorDrawer

} // namespace UnityExtensions.Tween.Editor

#endif // UNITY_EDITOR