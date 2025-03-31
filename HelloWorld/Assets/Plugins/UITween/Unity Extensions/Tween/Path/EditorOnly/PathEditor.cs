#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;
using UnityExtensions.Editor;
using UnityExtensions.Paths.Editor;
using System;

namespace UnityExtensions.Paths
{
    public partial class Path
    {
        const float _segmentWidth = 3f;

        static Color _segmentColor = new Color(1, 0.25f, 0.5f);
        static Color _arrowColor = new Color(1, 0.75f, 0f);

        protected static Color previewRotationColor = new Color(0.6f, 1f, 0.3f, 0.5f);
        protected static Color handlesRotationColor = new Color(0.6f, 1f, 0.3f);


        [SerializeField]
        bool _alwaysVisible = default;


        [DrawGizmo(GizmoType.Selected | GizmoType.NonSelected)]
        static void DrawGizmos(Path target, GizmoType type)
        {
            if ((type & GizmoType.Selected) != 0 || target._alwaysVisible || FloatingWindow.target == target)
            {
                Matrix4x4 matrix = Matrix4x4.TRS(
                    target.transform.position, target.transform.rotation,
                    new Vector3(target._worldScale, target._worldScale, target._worldScale));

                using (HandlesMatrixScope.New(matrix))
                {
                    target.DrawLocalGizmos(ref matrix);
                }
            }
        }


        protected virtual void DrawLocalGizmos(ref Matrix4x4 matrix)
        {
            // 绘制路径

            int count = segmentCount;
            for (int i = 0; i < count; i++)
            {
                this[i].Draw(_segmentColor, _segmentWidth);
            }

            // 绘制箭头

            using (HandlesColorScope.New(_arrowColor))
            {
                Vector3 point = this[count - 1].GetPoint(1f);
                float scale = HandleUtility.GetHandleSize(point) / Mathf.Abs(_worldScale);
                Vector3 vector = new Vector3(0.06f, 0f, -0.2f) * scale;
                var tangent = this[count - 1].GetTangent(1f);
                var rotation = Quaternion.LookRotation(tangent, matrix.inverse.MultiplyPoint(Camera.current.transform.position) - point);
                HandlesUtilities.DrawAALine(point, point + rotation * vector);
                vector.x = -vector.x;
                HandlesUtilities.DrawAALine(point, point + rotation * vector);
            }
        }


        protected abstract Type floatingWindowType
        {
            get;
        }


        /// <summary>
        /// Path Inspector
        /// </summary>
        [CustomEditor(typeof(Path), true)]
        protected class Editor : BaseEditor<Path>
        {
            public override void OnInspectorGUI()
            {
                // Edit Button

                var rect = EditorGUILayout.GetControlRect(true, 23);
                rect.x += EditorGUIUtility.labelWidth;
                rect.width = 33;

                using (var scope = ChangeCheckScope.New())
                {
                    bool edit = GUI.Toggle(rect, FloatingWindow.target == target, EditorGUIUtility.IconContent("EditCollider"), EditorGUIUtilities.buttonStyle);
                    rect.x = rect.xMax + 5;
                    rect.width = 140;
                    EditorGUI.LabelField(rect, "Edit Path", EditorGUIUtilities.middleLeftLabelStyle);
                    if (scope.changed) FloatingWindow.target = edit ? target : null;
                }

                // circular

                using (var scope = ChangeCheckScope.New(target))
                {
                    bool circular = EditorGUILayout.Toggle("Circular", target.circular);
                    if (scope.changed) target.circular = circular;
                }

                // World Scale

                using (var scope = ChangeCheckScope.New(target))
                {
                    float value = EditorGUILayout.FloatField(
                        EditorGUIUtilities.TempContent("World Scale", null, "Use \"World Scale\" to scale the path instead of scales of transform."),
                        target.worldScale);
                    if (scope.changed) target.worldScale = value;
                }

                // Length Error

                using (var scope = ChangeCheckScope.New(target))
                {
                    float value = EditorGUILayout.FloatField("Length Error", target.lengthError);
                    if (scope.changed) target.lengthError = value;
                }

                // Length

                if (target.isSamplesValid)
                {
                    EditorGUILayout.FloatField("Length", target.length);
                }
                else
                {
                    rect = EditorGUILayout.GetControlRect();
                    EditorGUI.LabelField(rect, "Length");
                    rect.xMin += EditorGUIUtility.labelWidth;

                    // Calculate Button

                    if (GUI.Button(rect, "Calculate", EditorStyles.miniButton))
                    {
                        Undo.RecordObject(target, "Calculate");
                        target.ValidateSamples();
                    }
                }

                // Visible

                using (var scope = ChangeCheckScope.New(target))
                {
                    bool value = EditorGUIUtilities.IndentedToggleButton("Always Visible", target._alwaysVisible);
                    if (scope.changed) target._alwaysVisible = value;
                }
            }

        } // class Editor


        /// <summary>
        /// Floating Window
        /// </summary>
        protected abstract class FloatingWindow : EditorWindow
        {
            static FloatingWindow _activeInstance;

            protected static Color capNormalColor = new Color(0.2f, 0.7f, 1f);
            protected static Color capSelectedColor = new Color(1f, 0.75f, 0f);
            protected const float capSize = 0.05f;
            protected static Vector3 snap = new Vector3(0.01f, 0.01f, 0.01f);


            protected int selectedTool { get; private set; }    // 0: pan, 1: 3D, 2: rotate
            protected int selectedNode;


            public static Path target
            {
                get
                {
                    if (!_activeInstance) return null;

                    var go = Selection.activeGameObject;
                    if (!go) return null;

                    return go.GetComponent<Path>();
                }
                set
                {
                    if (target == value) return;

                    if (value)
                    {
                        Selection.activeGameObject = value.gameObject;

                        _activeInstance = GetWindow(value.floatingWindowType, true, "Path") as FloatingWindow;
                        _activeInstance.minSize = _activeInstance.maxSize = new Vector2(108, _activeInstance.height);
                        _activeInstance.ShowUtility();
                    }
                    else
                    {
                        _activeInstance.Close();
                    }
                }
            }


            protected abstract float height { get; }
            protected virtual bool disableRotateTool => true;


            protected abstract void OnWindowGUI(Path path);
            protected abstract void OnSceneGUI(Path path);
            protected abstract Vector3 GetSceneGUIFocus(Path path);


            protected virtual void OnEnable()
            {
                SceneView.duringSceneGui += OnSceneGUI;
                Selection.selectionChanged += Close;
                Tools.hidden = true;
                _activeInstance = this;

                selectedTool = 0;
            }


            protected virtual void OnDisable()
            {
                SceneView.duringSceneGui -= OnSceneGUI;
                Selection.selectionChanged -= Close;
                Tools.hidden = false;
                _activeInstance = null;

                UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
            }


            void OnGUI()
            {
                var path = target;
                if (path && path.floatingWindowType == GetType())
                {
                    selectedNode = Mathf.Clamp(selectedNode, 0, path.nodeCount - 1);

                    EditorGUILayout.LabelField("Tools", EditorStyles.centeredGreyMiniLabel);

                    using (GUIContentColorScope.New(EditorGUIUtilities.labelNormalColor))
                    {
                        var rect = EditorGUILayout.GetControlRect(false, 25);

                        rect.width /= 3;
                        if (GUI.Toggle(rect, selectedTool == 0,
                            EditorGUIUtilities.TempContent(null, EditorResources.instance.moveToolPan, "Pan Move Tool"),
                            EditorGUIUtilities.buttonLeftStyle))
                            selectedTool = 0;

                        rect.x = rect.xMax;
                        if (GUI.Toggle(rect, selectedTool == 1,
                            EditorGUIUtilities.TempContent(null, EditorResources.instance.moveTool3D, "3D Move Tool"),
                            EditorGUIUtilities.buttonMiddleStyle))
                            selectedTool = 1;

                        using (DisabledScope.New(disableRotateTool))
                        {
                            rect.x = rect.xMax;
                            if (GUI.Toggle(rect, selectedTool == 2,
                                EditorGUIUtilities.TempContent(null, EditorResources.instance.RotateTool, "Rotate Tool"),
                                EditorGUIUtilities.buttonRightStyle))
                                selectedTool = 2;
                        }

                        EditorGUILayout.GetControlRect(false, 0);

                        rect = EditorGUILayout.GetControlRect(false, 25);

                        rect.width /= 3;
                        if (GUI.Button(rect,
                            EditorGUIUtilities.TempContent(null, EditorResources.instance.addNodeBack, "Add Node Back"),
                            EditorGUIUtilities.buttonLeftStyle))
                        {
                            Undo.RecordObject(path, "Add Node Back");
                            path.InsertNode(selectedNode);
                        }

                        using (DisabledScope.New(path.nodeCount == 2))
                        {
                            rect.x = rect.xMax;
                            if (GUI.Button(rect,
                                EditorGUIUtilities.TempContent(null, EditorResources.instance.removeNode, "Remove Node"),
                                EditorGUIUtilities.buttonMiddleStyle))
                            {
                                Undo.RecordObject(path, "Remove Node");
                                path.RemoveNode(selectedNode);
                                selectedNode = Mathf.Clamp(selectedNode - 1, 0, path.nodeCount - 1);
                            }
                        }

                        rect.x = rect.xMax;
                        if (GUI.Button(rect,
                            EditorGUIUtilities.TempContent(null, EditorResources.instance.addNodeForward, "Add Node Forward"),
                            EditorGUIUtilities.buttonRightStyle))
                        {
                            Undo.RecordObject(path, "Add Node Forward");
                            selectedNode++;
                            path.InsertNode(selectedNode);
                        }
                    }

                    OnWindowGUI(path);
                }
                else Close();
            }


            void OnSceneGUI(SceneView scene)
            {
                var path = target;
                if (path && path.floatingWindowType == GetType())
                {
                    HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));

                    selectedNode = Mathf.Clamp(selectedNode, 0, path.nodeCount - 1);

                    // 居中选择
                    if (Event.current.type == EventType.KeyDown)
                    {
                        if (Event.current.character == 'f' || Event.current.character == 'F')
                        {
                            Event.current.Use();
                            SceneView.lastActiveSceneView.LookAt(GetSceneGUIFocus(path));
                        }
                    }

                    OnSceneGUI(path);

                    Repaint();
                }
                else Close();
            }

        } // class FloatingWindow


        /// <summary>
        /// Floating Window<T>
        /// </summary>
        protected abstract class FloatingWindow<T> : FloatingWindow where T : Path
        {
            protected sealed override void OnWindowGUI(Path path)
            {
                if (selectedTool == 0 || selectedTool == 1)
                    OnMoveToolWindowGUI(path as T);
                else
                    OnRotateToolWindowGUI(path as T);
            }


            protected sealed override void OnSceneGUI(Path path)
            {
                if (selectedTool == 0 || selectedTool == 1)
                    OnMoveToolSceneGUI(path as T);
                else
                    OnRotateToolSceneGUI(path as T);
            }


            protected override Vector3 GetSceneGUIFocus(Path path)
            {
                return GetSceneGUIFocus(path as T);
            }


            protected abstract void OnMoveToolWindowGUI(T path);
            protected abstract void OnMoveToolSceneGUI(T path);

            protected virtual void OnRotateToolWindowGUI(T path) { }
            protected virtual void OnRotateToolSceneGUI(T path) { }

            protected abstract Vector3 GetSceneGUIFocus(T path);
        }


    } // class Path

} // UnityExtensions.Paths

#endif // UNITY_EDITOR