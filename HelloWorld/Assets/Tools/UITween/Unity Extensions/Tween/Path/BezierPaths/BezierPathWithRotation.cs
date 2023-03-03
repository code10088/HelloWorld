using System;
using UnityEngine;

namespace UnityExtensions.Paths
{
    /// <summary>
    /// 贝塞尔路径节点 (with Rotation)
    /// </summary>
    [Serializable]
    public class BezierNodeWithRotation : BezierNode, ICopyable<BezierNodeWithRotation>
    {
        [SerializeField]
        Quaternion _rotation = Quaternion.identity;

        [SerializeField]
        bool _lookTangent = false;


        public Quaternion rotation
        {
            get { return _rotation; }
            set
            {
                if (_lookTangent)
                {
                    _rotation = Quaternion.LookRotation(forwardTangent, value * Vector3.up);
                }
                else
                {
                    _rotation = value.normalized;
                }
            }
        }


        public bool lookTangent
        {
            get { return _lookTangent; }
            set
            {
                if (value != _lookTangent)
                {
                    _lookTangent = value;
                    if (value)
                    {
                        rotation = _rotation;
                    }
                }
            }
        }


        protected override void OnForwardTangentChanged()
        {
            if (_lookTangent)
            {
                rotation = _rotation;
            }
        }


        public void Copy(BezierNodeWithRotation target)
        {
            base.Copy(target);
            _rotation = target._rotation;
            _lookTangent = target._lookTangent;
        }
    }


    /// <summary>
    /// 带旋转控制的贝塞尔路径
    /// </summary>
    [AddComponentMenu("Paths/Bezier Path (with Rotation)")]
    public partial class BezierPathWithRotation : BezierPath<BezierNodeWithRotation>
    {
        public override void Reset()
        {
            base.Reset();
            node(0).lookTangent = true;
            node(1).lookTangent = true;
        }


        public Quaternion GetNodeRatation(int nodeIndex, Space space = Space.World)
        {
            if (space == Space.Self) return node(nodeIndex).rotation;
            else return TransformRotation(node(nodeIndex).rotation);
        }


        public void SetNodeRatation(int nodeIndex, Quaternion rotation, Space space = Space.World)
        {
            if (space == Space.World) rotation = InverseTransformRotation(rotation);
            node(nodeIndex).rotation = rotation;
        }


        public bool IsNodeLookTangent(int nodeIndex)
        {
            return node(nodeIndex).lookTangent;
        }


        public void SetNodeLookTangent(int nodeIndex, bool lookTangent)
        {
            node(nodeIndex).lookTangent = lookTangent;
        }


        public override void InsertNode(int nodeIndex)
        {
            base.InsertNode(nodeIndex);
            if (circular || (nodeIndex > 0 && nodeIndex < nodeCount - 1))
            {
                node(nodeIndex).rotation = Quaternion.SlerpUnclamped(circularNode(nodeIndex - 1).rotation, circularNode(nodeIndex + 1).rotation, 0.5f);
                node(nodeIndex).lookTangent = circularNode(nodeIndex - 1).lookTangent;
            }
            else
            {
                if (nodeIndex == 0)
                {
                    node(0).rotation = node(1).rotation;
                    node(0).lookTangent = node(1).lookTangent;
                }
                else
                {
                    node(nodeIndex).rotation = node(nodeIndex-1).rotation;
                    node(nodeIndex).lookTangent = node(nodeIndex-1).lookTangent;
                }
            }
        }


        public Quaternion GetRotation(Location location, Space space = Space.World)
        {
            var node1 = node(location.index);
            var node2 = circularNode(location.index + 1);
            Quaternion result;

            if (node1.lookTangent && node2.lookTangent)
                result = Quaternion.LookRotation(GetTangent(location, Space.Self),
                    Vector3.Slerp(node1.rotation * Vector3.up, node2.rotation * Vector3.up, location.time));
            else
                result = Quaternion.Slerp(node1.rotation, node2.rotation, location.time);

            if (space == Space.World)
                return TransformRotation(result);
            else
                return result;
        }


        public override void SetTransform(Transform target, float length, ref Location location)
        {
            base.SetTransform(target, length, ref location);
            target.rotation = GetRotation(location);
        }
    }

} // namespace UnityExtensions.Paths