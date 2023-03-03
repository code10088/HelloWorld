#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;
using UnityExtensions.Editor;
using System;

namespace UnityExtensions.Paths
{
    public abstract partial class BezierPath<Node>
    {
        protected abstract class BezierPathFloatingWindow<T> : FloatingWindow<T> where T : BezierPath<Node>
        {
            int _selectedType;     // -1: BackControlPoint, 0: MiddleControlPoint, 1: ForwardControlPoint


            protected override void OnEnable()
            {
                base.OnEnable();
                _selectedType = 0;
            }


            protected override float height => 180;


            protected override void OnMoveToolWindowGUI(T path)
            {
                string label = _selectedType == 0 ? "Position" : (_selectedType == 1 ? "Forward Ctrl-point" : "Back Ctrl-point");

                EditorGUILayout.LabelField(label, EditorStyles.centeredGreyMiniLabel);

                using (var scope = ChangeCheckScope.New(path))
                {
                    Vector3 point;

                    if (_selectedType == -1) point = path.GetNodeBackControlPoint(selectedNode, Space.Self);
                    else if (_selectedType == 0) point = path.GetNodePosition(selectedNode, Space.Self);
                    else point = path.GetNodeForwardControlPoint(selectedNode, Space.Self);

                    using (LabelWidthScope.New(EditorGUIUtility.singleLineHeight))
                    {
                        point.x = EditorGUILayout.FloatField("X", point.x);
                        point.y = EditorGUILayout.FloatField("Y", point.y);
                        point.z = EditorGUILayout.FloatField("Z", point.z);
                    }

                    if (scope.changed)
                    {
                        if (_selectedType == -1) path.SetNodeBackControlPoint(selectedNode, point, Space.Self);
                        else if (_selectedType == 0) path.SetNodePosition(selectedNode, point, Space.Self);
                        else path.SetNodeForwardControlPoint(selectedNode, point, Space.Self);
                    }
                }

                using (var scope = ChangeCheckScope.New(path))
                {
                    bool broken = GUILayout.Toggle(path.IsNodeBroken(selectedNode), "Broken", EditorStyles.miniButton);
                    if (scope.changed) path.SetNodeBroken(selectedNode, broken);
                }
            }


            protected override Vector3 GetSceneGUIFocus(T path)
            {
                if (_selectedType == -1) return path.GetNodeBackControlPoint(selectedNode);
                else if (_selectedType == 0) return path.GetNodePosition(selectedNode);
                else return path.GetNodeForwardControlPoint(selectedNode);
            }


            protected override void OnMoveToolSceneGUI(T path)
            {
                int count = path.nodeCount;
                for (int i = 0; i < count; i++)
                {
                    // 获取控制点位置
                    var middle = path.GetNodePosition(i);
                    float capSize;

                    var back = path.GetNodeBackControlPoint(i);
                    var forward = path.GetNodeForwardControlPoint(i);

                    // 绘制切线
                    Handles.color = selectedNode == i ? new Color(1f, 1f, 1f, 0.9f) : new Color(1f, 1f, 1f, 0.3f);
                    HandlesUtilities.DrawAALine(back, middle);
                    HandlesUtilities.DrawAALine(forward, middle);

                    // 绘制后控制点
                    capSize = HandleUtility.GetHandleSize(back) * FloatingWindow.capSize;

                    if (_selectedType == -1 && selectedNode == i)
                    {
                        using (var scope = ChangeCheckScope.New(path))
                        {
                            Handles.color = capSelectedColor;
                            if (selectedTool == 0) back = Handles.FreeMoveHandle(back, Quaternion.identity, capSize * 2, snap, Handles.RectangleHandleCap);
                            else if (selectedTool == 1) back = Handles.PositionHandle(back, path.transform.rotation);
                            if (scope.changed) path.SetNodeBackControlPoint(i, back);
                        }
                    }
                    else
                    {
                        Handles.color = capNormalColor;
                        if (Handles.Button(back, Quaternion.identity, capSize, capSize, Handles.DotHandleCap))
                        {
                            selectedNode = i;
                            _selectedType = -1;
                        }
                    }


                    // 绘制前控制点
                    capSize = HandleUtility.GetHandleSize(forward) * FloatingWindow.capSize;

                    if (_selectedType == 1 && selectedNode == i)
                    {
                        using (var scope = ChangeCheckScope.New(path))
                        {
                            Handles.color = capSelectedColor;
                            if (selectedTool == 0) forward = Handles.FreeMoveHandle(forward, Quaternion.identity, capSize * 2, snap, Handles.RectangleHandleCap);
                            else if (selectedTool == 1) forward = Handles.PositionHandle(forward, path.transform.rotation);
                            if (scope.changed) path.SetNodeForwardControlPoint(i, forward);
                        }
                    }
                    else
                    {
                        Handles.color = capNormalColor;
                        if (Handles.Button(forward, Quaternion.identity, capSize, capSize, Handles.DotHandleCap))
                        {
                            selectedNode = i;
                            _selectedType = 1;
                        }
                    }

                    // 绘制中控制点
                    capSize = HandleUtility.GetHandleSize(middle) * FloatingWindow.capSize;

                    if (_selectedType == 0 && selectedNode == i)
                    {
                        using (var scope = ChangeCheckScope.New(path))
                        {
                            Handles.color = capSelectedColor;
                            if (selectedTool == 0) middle = Handles.FreeMoveHandle(middle, Quaternion.identity, capSize * 2, snap, Handles.RectangleHandleCap);
                            else if (selectedTool == 1) middle = Handles.PositionHandle(middle, path.transform.rotation);
                            if (scope.changed) path.SetNodePosition(i, middle);
                        }
                    }
                    else
                    {
                        Handles.color = capNormalColor;
                        if (Handles.Button(middle, Quaternion.identity, capSize, capSize, Handles.DotHandleCap))
                        {
                            selectedNode = i;
                            _selectedType = 0;
                        }
                    }
                }
            }

        } // class BezierPathFloatingWindow

    } // class BezierPath<Node>


    public partial class BezierPath
    {
        protected override Type floatingWindowType => typeof(BezierPathFloatingWindow);

        class BezierPathFloatingWindow : BezierPathFloatingWindow<BezierPath>
        {
        }


        [ContextMenu("Convert to 'Bezier Path with Rotation'")]
        void Convert()
        {
            var path = Undo.AddComponent<BezierPathWithRotation>(gameObject);
            Copy(path, this);
            Undo.DestroyObjectImmediate(this);
        }
    }

} // namespace UnityExtensions.Paths

#endif