#if UNITY_EDITOR

using System;
using UnityEngine;
using UnityEditor;

namespace UnityExtensions.Editor
{
    public struct LabelWidthScope : IDisposable
    {
        float _orginal;

        public static LabelWidthScope New(float value)
        {
            var scope = new LabelWidthScope { _orginal = EditorGUIUtility.labelWidth };
            EditorGUIUtility.labelWidth = value;
            return scope;
        }

        void IDisposable.Dispose()
        {
            EditorGUIUtility.labelWidth = _orginal;
        }
    }


    public struct WideModeScope : IDisposable
    {
        bool _orginal;

        public static WideModeScope New(bool value)
        {
            var scope = new WideModeScope { _orginal = EditorGUIUtility.wideMode };
            EditorGUIUtility.wideMode = value;
            return scope;
        }

        void IDisposable.Dispose()
        {
            EditorGUIUtility.wideMode = _orginal;
        }
    }


    public struct IndentLevelScope : IDisposable
    {
        int _indent;

        public static IndentLevelScope New(int indentLevel = 1, bool relative = true)
        {
            var scope = new IndentLevelScope { _indent = EditorGUI.indentLevel };
            EditorGUI.indentLevel = relative ? (scope._indent + indentLevel) : indentLevel;
            return scope;
        }

        void IDisposable.Dispose()
        {
            EditorGUI.indentLevel = _indent;
        }
    }


    public struct HandlesColorScope : IDisposable
    {
        Color _orginal;

        public static HandlesColorScope New(Color value)
        {
            var scope = new HandlesColorScope { _orginal = Handles.color };
            Handles.color = value;
            return scope;
        }

        void IDisposable.Dispose()
        {
            Handles.color = _orginal;
        }
    }


    public struct HandlesMatrixScope : IDisposable
    {
        Matrix4x4 _orginal;

        public static HandlesMatrixScope New(Matrix4x4 value)
        {
            var scope = new HandlesMatrixScope { _orginal = Handles.matrix };
            Handles.matrix = value;
            return scope;
        }

        void IDisposable.Dispose()
        {
            Handles.matrix = _orginal;
        }
    }


    public struct GizmosColorScope : IDisposable
    {
        Color _orginal;

        public static GizmosColorScope New(Color value)
        {
            var scope = new GizmosColorScope { _orginal = Gizmos.color };
            Gizmos.color = value;
            return scope;
        }

        void IDisposable.Dispose()
        {
            Gizmos.color = _orginal;
        }
    }


    public struct GizmosMatrixScope : IDisposable
    {
        Matrix4x4 _orginal;

        public static GizmosMatrixScope New(Matrix4x4 value)
        {
            var scope = new GizmosMatrixScope { _orginal = Gizmos.matrix };
            Gizmos.matrix = value;
            return scope;
        }

        void IDisposable.Dispose()
        {
            Gizmos.matrix = _orginal;
        }
    }


    public struct DisabledScope : IDisposable
    {
        public static DisabledScope New(bool disabled)
        {
            EditorGUI.BeginDisabledGroup(disabled);
            return new DisabledScope();
        }

        public void Dispose()
        {
            EditorGUI.EndDisabledGroup();
        }
    }


    public struct ChangeCheckScope : IDisposable
    {
        bool _end;
        bool _changed;
        UnityEngine.Object _undoRecordObject;

        public bool changed
        {
            get
            {
                if (!_end)
                {
                    _end = true;
                    _changed = EditorGUI.EndChangeCheck();
                    if (_changed && _undoRecordObject)
                    {
                        Undo.RecordObject(_undoRecordObject, _undoRecordObject.name);
                    }
                }
                return _changed;
            }
        }

        public static ChangeCheckScope New(UnityEngine.Object undoRecordObject = null)
        {
            EditorGUI.BeginChangeCheck();
            return new ChangeCheckScope
            {
                _end = false,
                _changed = false,
                _undoRecordObject = undoRecordObject
            };
        }

        void IDisposable.Dispose()
        {
            if (!_end)
            {
                _end = true;
                _changed = EditorGUI.EndChangeCheck();
            }
        }
    }


    public struct HandlesGUIScope : IDisposable
    {
        public static HandlesGUIScope New()
        {
            Handles.BeginGUI();
            return new HandlesGUIScope();
        }

        void IDisposable.Dispose()
        {
            Handles.EndGUI();
        }
    }


    public struct HorizontalLayoutScope : IDisposable
    {
        public static HorizontalLayoutScope New()
        {
            EditorGUILayout.BeginHorizontal();
            return new HorizontalLayoutScope();
        }

        public static HorizontalLayoutScope New(params GUILayoutOption[] options)
        {
            EditorGUILayout.BeginHorizontal(options);
            return new HorizontalLayoutScope();
        }

        public static HorizontalLayoutScope New(GUIStyle style, params GUILayoutOption[] options)
        {
            EditorGUILayout.BeginHorizontal(style, options);
            return new HorizontalLayoutScope();
        }

        void IDisposable.Dispose()
        {
            EditorGUILayout.EndHorizontal();
        }
    }


    public struct VerticalLayoutScope : IDisposable
    {
        public static VerticalLayoutScope New()
        {
            EditorGUILayout.BeginVertical();
            return new VerticalLayoutScope();
        }

        public static VerticalLayoutScope New(params GUILayoutOption[] options)
        {
            EditorGUILayout.BeginVertical(options);
            return new VerticalLayoutScope();
        }

        public static VerticalLayoutScope New(GUIStyle style, params GUILayoutOption[] options)
        {
            EditorGUILayout.BeginVertical(style, options);
            return new VerticalLayoutScope();
        }

        void IDisposable.Dispose()
        {
            EditorGUILayout.EndVertical();
        }
    }

} // namespace UnityExtensions.Editor

#endif // UNITY_EDITOR