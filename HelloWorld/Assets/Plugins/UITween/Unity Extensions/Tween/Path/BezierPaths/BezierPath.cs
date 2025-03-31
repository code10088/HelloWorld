using UnityEngine;

namespace UnityExtensions.Paths
{
    /// <summary>
    /// 贝塞尔路径泛型基类
    /// </summary>
    public abstract partial class BezierPath<Node> : Path<Node> where Node : BezierNode, ICopyable<Node>, new()
    {
        // 更新样条
        void UpdateSegment(int segmentIndex)
        {
            var n0 = circularNode(segmentIndex);
            var n1 = circularNode(segmentIndex + 1);

            SetLocalBezierSegment(
                circularIndex(segmentIndex),
                n0.position,
                n0.forwardControlPoint,
                n1.backControlPoint, 
                n1.position);
        }

        
        /// <summary>
        /// 重置 (初始化) 路径
        /// </summary>
        // 实现时注意：子类实现需保证至少含有一个有效路径段
        public override void Reset()
        {
            base.Reset();

            InsertNodeInternal(0);
            InsertNodeInternal(1);

            node(0).position = default;
            node(0).broken = false;
            node(1).position = new Vector3(0, 0, 10);
            node(1).broken = false;

            UpdateSegment(0);
        }


        // 实现时注意：内部 segment list count 是闭合路径下的数量，非闭合路径不使用最后一段
        protected override void SetCircular(bool circular)
        {
            if (circular)
            {
                UpdateSegment(nodeCount - 1);
            }
        }


        /// <summary>
        /// 插入节点. 根据给定的参数初始化节点
        /// </summary>
        /// <param name="space"> 参数或返回值的相对空间 </param>
        public void InsertNode(int nodeIndex, Vector3 position, Vector3 forwardTangent, Vector3 backTangent, bool broken, Space space = Space.World)
        {
            if (space == Space.World)
            {
                position = InverseTransformPoint(position);
                forwardTangent = InverseTransformVector(forwardTangent);
                backTangent = InverseTransformVector(backTangent);
            }

            var n = InsertNodeInternal(nodeIndex);

            n.position = position;
            n.forwardTangent = forwardTangent;
            n.backTangent = backTangent;
            n.broken = broken;

            if (circular || nodeIndex < nodeCount - 1) UpdateSegment(nodeIndex);
            if (circular || nodeIndex > 0) UpdateSegment(nodeIndex - 1);
        }


        /// <summary>
        /// 插入节点. 自动初始化节点数据
        /// </summary>
        public override void InsertNode(int nodeIndex)
        {
            Vector3 position, forwardTangent, backTangent;

            if (circular || (nodeIndex > 0 && nodeIndex < nodeCount))
            {
                position = circularNode(nodeIndex - 1).GetPoint(0.5f);
                Vector3 tangent = circularNode(nodeIndex - 1).GetTangent(0.5f);
                forwardTangent = tangent * circularNode(nodeIndex).backTangent.magnitude;
                backTangent = - tangent * circularNode(nodeIndex - 1).forwardTangent.magnitude;
            }
            else
            {
                if (nodeIndex == 0)
                {
                    var first = node(0);
                    position = first.position + (first.position - node(1).position).magnitude * first.backTangent.normalized;
                    backTangent = first.backTangent;
                    forwardTangent = -backTangent;
                }
                else
                {
                    var last = node(nodeIndex - 1);
                    position = last.position + (last.position - node(nodeIndex - 2).position).magnitude * last.forwardTangent.normalized;
                    forwardTangent = last.forwardTangent;
                    backTangent = -forwardTangent;
                }
            }

            InsertNode(nodeIndex, position, forwardTangent, backTangent, false, Space.Self);
        }


        /// <summary>
        /// 移除节点. 节点数量超过 2 个的情况下才会执行
        /// </summary>
        public override bool RemoveNode(int nodeIndex)
        {
            if (nodeCount > 2)
            {
                RemoveNodeInternal(nodeIndex);

                if (circular || nodeIndex > 0)
                {
                    UpdateSegment(nodeIndex - 1);
                }

                return true;
            }
            return false;
        }


        /// <summary>
        /// 获取节点的位置
        /// </summary>
        /// <param name="space"> 参数或返回值的相对空间 </param>
        /// <param name="nodeIndex"> 节点索引 </param>
        /// <returns> 节点位置 </returns>
        public Vector3 GetNodePosition(int nodeIndex, Space space = Space.World)
        {
            if (space == Space.Self) return node(nodeIndex).position;
            else return TransformPoint(node(nodeIndex).position);
        }


        /// <summary>
        /// 设置节点的位置
        /// </summary>
        /// <param name="space"> 参数或返回值的相对空间 </param>
        /// <param name="nodeIndex"> 节点索引 </param>
        /// <param name="position"> 节点位置 </param>
        public void SetNodePosition(int nodeIndex, Vector3 position, Space space = Space.World)
        {
            if (space == Space.World) position = InverseTransformPoint(position);
            node(nodeIndex).position = position;

            if (circular || nodeIndex < nodeCount - 1) UpdateSegment(nodeIndex);
            if (circular || nodeIndex > 0) UpdateSegment(nodeIndex - 1);
        }


        /// <summary>
        /// 获取节点前方向切线
        /// </summary>
        /// <param name="space"> 参数或返回值的相对空间 </param>
        /// <param name="nodeIndex"> 节点索引 </param>
        /// <returns> 节点前方向切线 </returns>
        public Vector3 GetNodeForwardTangent(int nodeIndex, Space space = Space.World)
        {
            if (space == Space.Self) return node(nodeIndex).forwardTangent;
            else return TransformVector(node(nodeIndex).forwardTangent);
        }


        /// <summary>
        /// 设置节点前方向切线
        /// </summary>
        /// <param name="space"> 参数或返回值的相对空间 </param>
        /// <param name="nodeIndex"> 节点索引 </param>
        /// <param name="tangent"> 节点前方向切线 </param>
        public void SetNodeForwardTangent(int nodeIndex, Vector3 tangent, Space space = Space.World)
        {
            if (space == Space.World) tangent = InverseTransformVector(tangent);
            node(nodeIndex).forwardTangent = tangent;

            if (circular || nodeIndex < nodeCount - 1) UpdateSegment(nodeIndex);
            if ((circular || nodeIndex > 0) && !node(nodeIndex).broken) UpdateSegment(nodeIndex - 1);
        }


        /// <summary>
        /// 获取节点后方向切线
        /// </summary>
        /// <param name="space"> 参数或返回值的相对空间 </param>
        /// <param name="nodeIndex"> 节点索引 </param>
        /// <returns> 节点后方向切线 </returns>
        public Vector3 GetNodeBackTangent(int nodeIndex, Space space = Space.World)
        {
            if (space == Space.Self) return node(nodeIndex).backTangent;
            else return TransformVector(node(nodeIndex).backTangent);
        }


        /// <summary>
        /// 设置节点后方向切线
        /// </summary>
        /// <param name="space"> 参数或返回值的相对空间 </param>
        /// <param name="nodeIndex"> 节点索引 </param>
        /// <param name="forwardTangent"> 节点后方向切线 </param>
        public void SetNodeBackTangent(int nodeIndex, Vector3 tangent, Space space = Space.World)
        {
            if (space == Space.World) tangent = InverseTransformVector(tangent);
            node(nodeIndex).backTangent = tangent;

            if (circular || nodeIndex > 0) UpdateSegment(nodeIndex - 1);
            if ((circular || nodeIndex < nodeCount - 1) && !node(nodeIndex).broken) UpdateSegment(nodeIndex);
        }


        /// <summary>
        /// 获取节点前方向控制点位置
        /// </summary>
        /// <param name="space"> 参数或返回值的相对空间 </param>
        /// <param name="nodeIndex"> 节点索引 </param>
        /// <returns> 节点前方向控制点位置 </returns>
        public Vector3 GetNodeForwardControlPoint(int nodeIndex, Space space = Space.World)
        {
            if (space == Space.Self) return node(nodeIndex).forwardControlPoint;
            else return TransformPoint(node(nodeIndex).forwardControlPoint);
        }


        /// <summary>
        /// 设置节点前方向控制点位置
        /// </summary>
        /// <param name="space"> 参数或返回值的相对空间 </param>
        /// <param name="nodeIndex"> 节点索引 </param>
        /// <param name="position"> 节点前方向控制点位置 </param>
        public void SetNodeForwardControlPoint(int nodeIndex, Vector3 position, Space space = Space.World)
        {
            if (space == Space.World) position = InverseTransformPoint(position);
            node(nodeIndex).forwardControlPoint = position;

            if (circular || nodeIndex < nodeCount - 1) UpdateSegment(nodeIndex);
            if ((circular || nodeIndex > 0) && !node(nodeIndex).broken) UpdateSegment(nodeIndex - 1);
        }


        /// <summary>
        /// 获取节点后方向控制点位置
        /// </summary>
        /// <param name="space"> 参数或返回值的相对空间 </param>
        /// <param name="nodeIndex"> 节点索引 </param>
        /// <returns> 节点后方向控制点位置 </returns>
        public Vector3 GetNodeBackControlPoint(int nodeIndex, Space space = Space.World)
        {
            if (space == Space.Self) return node(nodeIndex).backControlPoint;
            else return TransformPoint(node(nodeIndex).backControlPoint);
        }


        /// <summary>
        /// 设置节点后方向控制点位置
        /// </summary>
        /// <param name="space"> 参数或返回值的相对空间 </param>
        /// <param name="nodeIndex"> 节点索引 </param>
        /// <param name="position"> 节点后方向控制点位置 </param>
        public void SetNodeBackControlPoint(int nodeIndex, Vector3 position, Space space = Space.World)
        {
            if (space == Space.World) position = InverseTransformPoint(position);
            node(nodeIndex).backControlPoint = position;

            if (circular || nodeIndex > 0) UpdateSegment(nodeIndex - 1);
            if ((circular || nodeIndex < nodeCount - 1) && !node(nodeIndex).broken) UpdateSegment(nodeIndex);
        }


        /// <summary>
        /// 节点是否为拐点
        /// </summary>
        /// <param name="nodeIndex"> 节点索引 </param>
        /// <returns> 节点是否为拐点 </returns>
        public bool IsNodeBroken(int nodeIndex)
        {
            return node(nodeIndex).broken;
        }


        /// <summary>
        /// 设置节点是否为拐点
        /// </summary>
        /// <param name="nodeIndex"> 节点索引 </param>
        /// <param name="broken"> 节点是否为拐点 </param>
        public void SetNodeBroken(int nodeIndex, bool broken)
        {
            node(nodeIndex).broken = broken;

            if (!broken)
            {
                if (circular || nodeIndex < nodeCount - 1) UpdateSegment(nodeIndex);
                if (circular || nodeIndex > 0) UpdateSegment(nodeIndex - 1);
            }
        }

    } // class BezierPath


    /// <summary>
    /// 贝塞尔路径
    /// </summary>
    [AddComponentMenu("Paths/Bezier Path")]
    public partial class BezierPath : BezierPath<BezierNode>
    {
    }

} // namespace UnityExtensions.Paths