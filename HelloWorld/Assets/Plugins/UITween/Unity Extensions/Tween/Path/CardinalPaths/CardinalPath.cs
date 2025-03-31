using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityExtensions.Paths
{
    /// <summary>
    /// Cardinal Path
    /// </summary>
    public abstract partial class CardinalPath<Node> : Path<Node> where Node : CardinalNode, ICopyable<Node>, new()
    {
        // 更新样条
        void UpdateSegment(int segmentIndex)
        {
            segmentIndex = circularIndex(segmentIndex);

            int p0 = circular ? circularIndex(segmentIndex - 1) : Mathf.Max(0, segmentIndex - 1);
            int p3 = circular ? circularIndex(segmentIndex + 2) : Mathf.Min(nodeCount - 1, segmentIndex + 2);

            SetLocalCardinalSegment(
                segmentIndex,
                node(p0).position,
                node(segmentIndex).position,
                circularNode(segmentIndex + 1).position,
                node(p3).position,
                node(segmentIndex).tension);
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
            node(1).position = new Vector3(0, 0, 10);

            UpdateSegment(0);
        }


        // 实现时注意：内部 segment list count 是闭合路径下的数量，非闭合路径不使用最后一段
        protected override void SetCircular(bool circular)
        {
            if (circular)
            {
                UpdateSegment(0);
                UpdateSegment(nodeCount - 1);
                UpdateSegment(nodeCount - 2);
            }
            else
            {
                UpdateSegment(nodeCount - 2);
                UpdateSegment(0);
            }
        }


        /// <summary>
        /// 插入节点. 根据给定的参数初始化节点
        /// </summary>
        /// <param name="space"> 参数或返回值的相对空间 </param>
        public void InsertNode(int nodeIndex, Vector3 point, Space space = Space.World)
        {
            if (space == Space.World) point = InverseTransformPoint(point);

            var n = InsertNodeInternal(nodeIndex);
            n.position = point;

            if (circular)
            {
                n.tension = circularNode(nodeIndex - 1).tension;
                UpdateSegment(nodeIndex);
                UpdateSegment(nodeIndex-1);
                UpdateSegment(nodeIndex-2);
                UpdateSegment(nodeIndex+1);
            }
            else
            {
                if (nodeIndex == 0) n.tension = node(1).tension;
                else n.tension = node(nodeIndex - 1).tension;
                
                if (nodeIndex >= 2) UpdateSegment(nodeIndex-2);
                if (nodeIndex >= 1) UpdateSegment(nodeIndex-1);
                if (nodeIndex <= nodeCount-2) UpdateSegment(nodeIndex);
                if (nodeIndex <= nodeCount-3) UpdateSegment(nodeIndex+1);
            }
        }


        /// <summary>
        /// 插入节点. 自动初始化节点数据
        /// </summary>
        public override void InsertNode(int nodeIndex)
        {
            Vector3 point;

            if (circular || (nodeIndex > 0 && nodeIndex < nodeCount))
            {
                point = circularNode(nodeIndex - 1).GetPoint(0.5f);
            }
            else
            {
                if (nodeIndex == 0)
                {
                    point = node(0).position * 2 - node(1).position;
                }
                else
                {
                    point = node(nodeCount-1).position * 2 - node(nodeCount-2).position;
                }
            }

            InsertNode(nodeIndex, point, Space.Self);
        }


        /// <summary>
        /// 移除节点. 节点数量超过 2 个的情况下才会执行
        /// </summary>
        public override bool RemoveNode(int nodeIndex)
        {
            if (nodeCount > 2)
            {
                RemoveNodeInternal(nodeIndex);

                if (circular)
                {
                    UpdateSegment(nodeIndex);
                    UpdateSegment(nodeIndex - 1);
                    UpdateSegment(nodeIndex - 2);
                }
                else
                {
                    if (nodeIndex >= 2) UpdateSegment(nodeIndex - 2);
                    if (nodeIndex >= 1) UpdateSegment(nodeIndex - 1);
                    if (nodeIndex <= nodeCount-2) UpdateSegment(nodeIndex);
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
        /// <returns> 节点的位置 </returns>
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
        /// <param name="position"> 节点的位置 </param>
        public virtual void SetNodePosition(int nodeIndex, Vector3 position, Space space = Space.World)
        {
            if (space == Space.World) position = InverseTransformPoint(position);
            node(nodeIndex).position = position;

            if (circular)
            {
                UpdateSegment(nodeIndex);
                UpdateSegment(nodeIndex+1);
                UpdateSegment(nodeIndex-1);
                UpdateSegment(nodeIndex-2);
            }
            else
            {
                if (nodeIndex >= 2) UpdateSegment(nodeIndex - 2);
                if (nodeIndex >= 1) UpdateSegment(nodeIndex - 1);
                if (nodeIndex <= segmentCount-1) UpdateSegment(nodeIndex);
                if (nodeIndex <= segmentCount-2) UpdateSegment(nodeIndex + 1);
            }
        }


        /// <summary>
        /// 获取路径段的张力
        /// </summary>
        public float GetSegmentTension(int segmentIndex)
        {
            return node(segmentIndex).tension;
        }


        /// <summary>
        /// 设置路径段的张力
        /// </summary>
        public void SetSegmentTension(int segmentIndex, float tension)
        {
            node(segmentIndex).tension = Mathf.Clamp01(tension);
            UpdateSegment(segmentIndex);
        }


        /// <summary>
        /// 通过一个 List 来重新初始化路径的所有节点
        /// </summary>
        /// <param name="nodes"> 节点列表 </param>
        /// <param name="circular"> 路径是否首尾相接 </param>
        /// <param name="startIndex"> 第一个节点的下标 </param>
        /// <param name="count"> 节点总数, 非正值表示直到列表尾部 </param>
        /// <param name="space"> 参数或返回值的相对空间 </param>
        /// <returns> 如果操作成功返回 true, 否则返回 false </returns>
        public bool SetNodes(IList<Vector3> nodes, bool circular, int startIndex = 0, int count = 0, Space space = Space.World)
        {
            if (count <= 0) count = nodes.Count;

            if (startIndex >= 0 && count >= 2 && startIndex + count <= nodes.Count)
            {
                int targetSegmentCount = circular ? count : count - 1;

                // 移除多余的节点
                while (segmentCount > targetSegmentCount)
                {
                    RemoveNodeInternal(segmentCount - 1);
                }

                // 添加缺少的节点
                while (segmentCount < targetSegmentCount)
                {
                    InsertNodeInternal(segmentCount);
                }

                // 设置节点
                for (int i = 0; i < count; i++)
                {
                    node(i).position = space == Space.Self ? nodes[startIndex + i] : InverseTransformPoint(nodes[startIndex + i]);
                    node(i).tension = 0.5f;
                }

                this.circular = circular;

                // 更新所有路径段
                for (int i = 0; i < segmentCount; i++)
                {
                    UpdateSegment(i);
                }

                return true;
            }

            return false;
        }

    } // class CardinalPath


    /// <summary>
    /// CardinalPath
    /// </summary>
    [AddComponentMenu("Paths/Cardinal Path")]
    public partial class CardinalPath : CardinalPath<CardinalNode>
    {
    }

} // namespace UnityExtensions.Paths