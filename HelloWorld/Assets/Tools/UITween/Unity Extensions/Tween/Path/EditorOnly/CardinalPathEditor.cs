#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;
using UnityExtensions.Editor;
using System;

namespace UnityExtensions.Paths
{
    public abstract partial class CardinalPath<Node>
    {
        protected abstract class CardinalPathFloatingWindow<T> : FloatingWindow<T> where T : CardinalPath<Node>
        {
            bool _showTension;


            protected override void OnEnable()
            {
                base.OnEnable();
                _showTension = false;
            }


            protected override float height => 180;


            protected override void OnMoveToolWindowGUI(T path)
            {
                EditorGUILayout.GetControlRect(false, 0);
                _showTension = GUILayout.Toggle(_showTension, "Tension-sliders", EditorStyles.miniButton);

                EditorGUILayout.LabelField("Position", EditorStyles.centeredGreyMiniLabel);

                using (var scope = ChangeCheckScope.New(path))
                {
                    Vector3 point = path.GetNodePosition(selectedNode, Space.Self);

                    using (LabelWidthScope.New(EditorGUIUtility.singleLineHeight))
                    {
                        point.x = EditorGUILayout.FloatField("X", point.x);
                        point.y = EditorGUILayout.FloatField("Y", point.y);
                        point.z = EditorGUILayout.FloatField("Z", point.z);
                    }

                    if (scope.changed)
                    {
                        path.SetNodePosition(selectedNode, point, Space.Self);
                    }
                }
            }


            protected override Vector3 GetSceneGUIFocus(T path)
            {
                return path.GetNodePosition(selectedNode);
            }


            protected override void OnMoveToolSceneGUI(T path)
            {
                if (_showTension)
                {
                    for (int i = 0; i < path.segmentCount; i++)
                    {
                        float tension = path.GetSegmentTension(i);

                        var point1 = path.GetNodePosition(i);
                        var point2 = path.GetNodePosition((i + 1) % path.nodeCount);
                        point1 = point1 * 0.75f + point2 * 0.25f;
                        point2 = point1 / 3f + point2 * (2f / 3f);

                        var point = (point2 - point1) * tension + point1;

                        Handles.color = new Color(1f, 1f, 1f, 0.5f);
                        HandlesUtilities.DrawAALine(point1, point2, 4);

                        float capSize = HandleUtility.GetHandleSize(point) * FloatingWindow.capSize;

                        using (var scope = ChangeCheckScope.New(path))
                        {
                            Handles.color = capNormalColor;
                            point = Handles.FreeMoveHandle(point, Quaternion.identity, capSize, snap, Handles.CircleHandleCap);

                            tension = MathUtilities.ClosestPointOnSegmentFactor(point, point1, point2);
                            //tension = (float)Math.Round(tension, 2);

                            //using (new HandlesGUIScope(0))
                            //{
                            //    var pos2D = HandleUtility.WorldToGUIPoint(point);
                            //    var rect = new Rect(pos2D.x + 12, pos2D.y - 9, 32, 18);
                            //    EditorGUI.DrawRect(rect, new Color(0, 0, 0, 0.5f));
                            //    using (new GUIContentColorScope(Color.white))
                            //    {
                            //        EditorGUI.LabelField(rect, tension.ToString(), EditorStyles.whiteLabel);
                            //    }
                            //}

                            if (scope.changed)
                            {
                                path.SetSegmentTension(i, tension);
                            }
                        }
                    }
                }

                int count = path.nodeCount;
                for (int i = 0; i < count; i++)
                {
                    var position = path.GetNodePosition(i);

                    float capSize = HandleUtility.GetHandleSize(position) * FloatingWindow.capSize;

                    if (selectedNode == i)
                    {
                        using (var scope = ChangeCheckScope.New(path))
                        {
                            Handles.color = capSelectedColor;
                            if (selectedTool == 0) position = Handles.FreeMoveHandle(position, Quaternion.identity, capSize * 2, snap, Handles.RectangleHandleCap);
                            else position = Handles.PositionHandle(position, path.transform.rotation);
                            if (scope.changed) path.SetNodePosition(i, position);
                        }
                    }
                    else
                    {
                        Handles.color = capNormalColor;
                        if (Handles.Button(position, Quaternion.identity, capSize, capSize, Handles.DotHandleCap))
                        {
                            selectedNode = i;
                        }
                    }
                }
            }

        } // class CardinalPathFloatingWindow

    } // class CardinalPath<Node>


    public partial class CardinalPath
    {
        protected override Type floatingWindowType => typeof(CardinalPathFloatingWindow);

        class CardinalPathFloatingWindow : CardinalPathFloatingWindow<CardinalPath>
        {
        }


        [ContextMenu("Convert to 'Cardinal Path with Rotation'")]
        void Convert()
        {
            var path = Undo.AddComponent<CardinalPathWithRotation>(gameObject);
            Copy(path, this);
            Undo.DestroyObjectImmediate(this);
        }
    }

} // namespace UnityExtensions.Paths

#endif