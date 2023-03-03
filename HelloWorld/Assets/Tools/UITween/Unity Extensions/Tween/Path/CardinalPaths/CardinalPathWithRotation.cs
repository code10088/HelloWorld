using System;
using UnityEngine;

namespace UnityExtensions.Paths
{
    /// <summary>
    /// Cardinal 路径节点 (with Rotation)
    /// </summary>
    [Serializable]
    public class CardinalNodeWithRotation : CardinalNode, ICopyable<CardinalNodeWithRotation>
    {
        public Quaternion rotation;
        public bool lookTangent;


        public void SetRotation(Path path, int nodeIndex, Quaternion value)
        {
            if (lookTangent)
            {
                var loc = new Path.Location(nodeIndex, 0);
                if (path.circular)
                    loc.index = (nodeIndex + path.segmentCount) % path.segmentCount;
                else if (nodeIndex == path.nodeCount - 1)
                    loc.Set(nodeIndex - 1, 1);
                rotation = Quaternion.LookRotation(path.GetTangent(loc, Space.Self), value * Vector3.up);
            }
            else
            {
                rotation = value.normalized;
            }
        }


        public void SetLookTangent(Path path, int nodeIndex, bool value)
        {
            lookTangent = value;
            if (value)
            {
                SetRotation(path, nodeIndex, rotation);
            }
        }


        public void Copy(CardinalNodeWithRotation target)
        {
            base.Copy(target);
            rotation = target.rotation;
            lookTangent = target.lookTangent;
        }
    }


    /// <summary>
    /// 带旋转控制的 Cardinal 路径
    /// </summary>
    [AddComponentMenu("Paths/Cardinal Path (with Rotation)")]
    public partial class CardinalPathWithRotation : CardinalPath<CardinalNodeWithRotation>
    {
        public override void Reset()
        {
            base.Reset();
            node(0).lookTangent = true;
            node(1).lookTangent = true;
            node(0).rotation = Quaternion.identity;
            node(1).rotation = Quaternion.identity;
        }


        public Quaternion GetNodeRatation(int nodeIndex, Space space = Space.World)
        {
            if (space == Space.Self) return node(nodeIndex).rotation;
            else return TransformRotation(node(nodeIndex).rotation);
        }


        public void SetNodeRatation(int nodeIndex, Quaternion rotation, Space space = Space.World)
        {
            if (space == Space.World) rotation = InverseTransformRotation(rotation);
            node(nodeIndex).SetRotation(this, nodeIndex, rotation);
        }


        public bool IsNodeLookTangent(int nodeIndex)
        {
            return node(nodeIndex).lookTangent;
        }


        public void SetNodeLookTangent(int nodeIndex, bool lookTangent)
        {
            node(nodeIndex).SetLookTangent(this, nodeIndex, lookTangent);
        }


        public override void InsertNode(int nodeIndex)
        {
            base.InsertNode(nodeIndex);

            if (circular || (nodeIndex > 0 && nodeIndex < nodeCount - 1))
            {
                var prev = circularNode(nodeIndex - 1);
                var next = circularNode(nodeIndex + 1);

                node(nodeIndex).lookTangent = prev.lookTangent;
                node(nodeIndex).SetRotation(this, nodeIndex, Quaternion.SlerpUnclamped(prev.rotation, next.rotation, 0.5f));

                if (prev.lookTangent) prev.SetRotation(this, nodeIndex - 1, prev.rotation);
                if (next.lookTangent) next.SetRotation(this, nodeIndex + 1, next.rotation);
            }
            else
            {
                if (nodeIndex == 0)
                {
                    var next = node(1);
                    node(0).lookTangent = next.lookTangent;
                    node(0).SetRotation(this, 0, next.rotation);
                    if (next.lookTangent) next.SetRotation(this, 1, next.rotation);
                }
                else
                {
                    var prev = node(nodeIndex - 1);
                    node(nodeIndex).lookTangent = prev.lookTangent;
                    node(nodeIndex).SetRotation(this, nodeIndex, prev.rotation);
                    if (prev.lookTangent) prev.SetRotation(this, nodeIndex - 1, prev.rotation);
                }
            }
        }


        public override bool RemoveNode(int nodeIndex)
        {
            if (base.RemoveNode(nodeIndex))
            {
                if (circular || (nodeIndex > 0 && nodeIndex < nodeCount))
                {
                    var prev = circularNode(nodeIndex - 1);
                    var next = circularNode(nodeIndex);

                    if (prev.lookTangent) prev.SetRotation(this, nodeIndex - 1, prev.rotation);
                    if (next.lookTangent) next.SetRotation(this, nodeIndex, next.rotation);
                }
                else
                {
                    if (nodeIndex == 0)
                    {
                        var next = node(0);
                        if (next.lookTangent) next.SetRotation(this, 0, next.rotation);
                    }
                    else
                    {
                        var prev = node(nodeCount - 1);
                        if (prev.lookTangent) prev.SetRotation(this, nodeCount - 1, prev.rotation);
                    }
                }

                return true;
            }

            return false;
        }


        protected override void SetCircular(bool circular)
        {
            base.SetCircular(circular);

            var first = node(0);
            var last = node(nodeCount - 1);

            if (first.lookTangent) first.SetRotation(this, 0, first.rotation);
            if (last.lookTangent) last.SetRotation(this, nodeCount - 1, last.rotation);
        }


        public override void SetNodePosition(int nodeIndex, Vector3 position, Space space = Space.World)
        {
            base.SetNodePosition(nodeIndex, position, space);

            if (circular || (nodeIndex > 0 && nodeIndex < nodeCount - 1))
            {
                var prev = circularNode(nodeIndex - 1);
                var next = circularNode(nodeIndex + 1);

                if (prev.lookTangent) prev.SetRotation(this, nodeIndex - 1, prev.rotation);
                if (next.lookTangent) next.SetRotation(this, nodeIndex + 1, next.rotation);
            }
            else
            {
                if (node(nodeIndex).lookTangent) node(nodeIndex).SetRotation(this, nodeIndex, node(nodeIndex).rotation);

                if (nodeIndex == 0)
                {
                    var next = node(1);
                    if (next.lookTangent) next.SetRotation(this, 1, next.rotation);
                }
                else
                {
                    var prev = node(nodeIndex - 1);
                    if (prev.lookTangent) prev.SetRotation(this, nodeIndex - 1, prev.rotation);
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