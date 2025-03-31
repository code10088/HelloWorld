using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityExtensions.Paths
{
    /// <summary>
    /// 路径基类. 路径是 Cubic Spline 的有序组合
    /// </summary>
    public abstract partial class Path : ScriptableComponent
    {
        // 节点. 一个节点包含一段三次样条
        [Serializable]
        public class Node : CubicSpline, ICopyable<Node>
        {
            public float pathLength;        // 从路径起点到此段终点的路径长度


            public void Copy(Node target)
            {
                base.Copy(target);
                pathLength = target.pathLength;
            }
        }


        /// <summary>
        /// 路径点位置
        /// </summary>
        [Serializable]
        public struct Location
        {
            public int index;       // 路径段索引
            public float time;      // 路径段时间 (三次样条参数 t)

            public Location(int index, float time)
            {
                this.index = index;
                this.time = time;
            }

            public void Set(int index, float time)
            {
                this.index = index;
                this.time = time;
            }
        }


        [SerializeField] float _worldScale = default;
        [SerializeField] bool _circular = default;
        [SerializeField] float _localLengthError = default;
        [SerializeField] int _firstInvalidPathLengthIndex = default;


        /// <summary>
        /// 世界空间缩放. 缩放不可为 0
        /// </summary>
        public float worldScale
        {
            get { return _worldScale; }
            set
            {
                if (value < 0f)
                {
                    _worldScale = Mathf.Clamp(value, -MathUtilities.Million, -MathUtilities.OneMillionth);
                }
                else
                {
                    _worldScale = Mathf.Clamp(value, MathUtilities.OneMillionth, MathUtilities.Million);
                }
            }
        }


        /// <summary>
        /// 世界缩放的绝对值
        /// </summary>
        public float absWorldScale
        {
            get { return _worldScale >= 0f ? _worldScale : -_worldScale; }
        }


        /// <summary>
        /// 路径是否首尾相接
        /// </summary>
        public bool circular
        {
            get { return _circular; }
            set
            {
                if (_circular != value)
                {
                    _circular = value;
                    SetCircular(value);
                }
            }
        }


        /// <summary>
        /// 路径本地空间长度误差
        /// </summary>
        public float localLengthError
        {
            get { return _localLengthError; }
            set
            {
                value = Mathf.Clamp(value, CubicSpline.minLengthError, CubicSpline.maxLengthError);

                if (value != _localLengthError)
                {
                    _localLengthError = value;
                    int count = segmentCount;
                    for (int i = 0; i < count; i++)
                    {
                        this[i].lengthError = value;
                    }
                    _firstInvalidPathLengthIndex = 0;
                }
            }
        }


        /// <summary>
        /// 路径长度误差
        /// </summary>
        public float lengthError
        {
            get { return _localLengthError * absWorldScale; }
            set { localLengthError = value / absWorldScale; }
        }


        /// <summary>
        /// 路径本地空间总长度
        /// </summary>
        public float localLength
        {
            get
            {
                int lastSegmentIndex = segmentCount - 1;
                ValidatePathLength(lastSegmentIndex);
                return this[lastSegmentIndex].pathLength;
            }
        }


        /// <summary>
        /// 路径总长度
        /// </summary>
        public float length
        {
            get
            {
                return localLength * absWorldScale;
            }
        }


        /// <summary>
        /// 节点总数
        /// </summary>
        public abstract int nodeCount { get; }


        /// <summary>
        /// 路径段总数
        /// </summary>
        public int segmentCount
        {
            // 非闭合路径不使用最后一段
            get { return _circular ? nodeCount : (nodeCount - 1); }
        }


        /// <summary>
        /// 路径长度采样是否有效
        /// </summary>
        public bool isSamplesValid
        {
            get { return _firstInvalidPathLengthIndex >= segmentCount; }
        }


        protected abstract Node this[int i] { get; }


        /// <summary>
        /// 插入节点. 自动初始化节点数据
        /// </summary>
        public abstract void InsertNode(int nodeIndex);


        /// <summary>
        /// 移除节点. 节点数量超过 2 个的情况下才会执行
        /// </summary>
        public abstract bool RemoveNode(int nodeIndex);


        /// <summary>
        /// 重置 (初始化) 路径
        /// </summary>
        // 实现时注意：子类实现需保证至少含有一个有效路径段
        public virtual void Reset()
        {
            _worldScale = 1f;
            _circular = false;
            _localLengthError = 0.01f;
            _firstInvalidPathLengthIndex = 0;
        }


        // 实现时注意：内部 segment list count 是闭合路径下的数量，非闭合路径不使用最后一段
        protected abstract void SetCircular(bool circular);


        // 设置 BezierPath 参数
        protected void SetLocalBezierSegment(int segmentIndex, Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3)
        {
            this[segmentIndex].SetBezierCurve(p0, p1, p2, p3);

            if (_firstInvalidPathLengthIndex > segmentIndex)
            {
                _firstInvalidPathLengthIndex = segmentIndex;
            }
        }


        // 设置 CardinalPath 参数
        protected void SetLocalCardinalSegment(int segmentIndex, Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float tension)
        {
            this[segmentIndex].SetCardinalCurve(p0, p1, p2, p3, tension);

            if (_firstInvalidPathLengthIndex > segmentIndex)
            {
                _firstInvalidPathLengthIndex = segmentIndex;
            }
        }


        // 如果路径未初始化 (比如在运行时代码中创建的路径), 在这里完成初始化
        void Awake()
        {
            if (_localLengthError == 0f) Reset();
        }


        // 确保从路径起点到路径段终点的路径长度是有效的
        void ValidatePathLength(int segmentIndex)
        {
            for (; _firstInvalidPathLengthIndex <= segmentIndex; _firstInvalidPathLengthIndex++)
            {
                if (_firstInvalidPathLengthIndex == 0)
                {
                    this[0].pathLength = this[0].length;
                }
                else
                {
                    this[_firstInvalidPathLengthIndex].pathLength =
                        this[_firstInvalidPathLengthIndex].length +
                        this[_firstInvalidPathLengthIndex - 1].pathLength;
                }
            }
        }


        /// <summary>
        /// 对路径长度采样
        /// </summary>
        public void ValidateSamples()
        {
            ValidatePathLength(segmentCount - 1);
        }


        /// <summary>
        /// 清除路径长度采样
        /// </summary>
        public void InvalidateSamples()
        {
            int count = segmentCount;
            for (int i = 0; i < count; i++)
            {
                this[i].InvalidateSamples();
            }
            _firstInvalidPathLengthIndex = 0;
        }


        /// <summary>
        /// 将路径本地坐标转换为世界坐标
        /// 物体的缩放被忽略, 使用路径的 worldScale 替代
        /// </summary>
        /// <param name="localPoint"> 本地坐标 </param>
        /// <returns> 路径本地点对应的世界坐标 </returns>
        public Vector3 TransformPoint(Vector3 localPoint)
        {
            return transform.TransformDirection(_worldScale * localPoint) + transform.position;
        }


        /// <summary>
        /// 将路径本地向量转换为世界向量
        /// 物体的缩放被忽略, 使用路径的 worldScale 替代
        /// 转换前后向量长度可能发生变化
        /// </summary>
        /// <param name="localVector"> 本地向量 </param>
        /// <returns> 路径本地向量对应的世界向量 </returns>
        public Vector3 TransformVector(Vector3 localVector)
        {
            return transform.TransformDirection(_worldScale * localVector);
        }


        /// <summary>
        /// 将路径本地方向转换为世界方向
        /// 物体的缩放被忽略, 路径的 worldScale 符号影响转换结果
        /// 转换前后向量长度不变
        /// </summary>
        /// <param name="localDirection"> 本地方向 </param>
        /// <returns> 路径本地方向对应的世界方向 </returns>
        public Vector3 TransformDirection(Vector3 localDirection)
        {
            return transform.TransformDirection(Mathf.Sign(_worldScale) * localDirection);
        }


        /// <summary>
        /// 将路径本地旋转转换为世界旋转
        /// 路径的 worldScale 符号影响转换结果
        /// </summary>
        /// <param name="localRotation"> 本地旋转 </param>
        /// <returns> 本地旋转对应的世界旋转 </returns>
        public Quaternion TransformRotation(Quaternion localRotation)
        {
            if (_worldScale >= 0f) return transform.rotation * localRotation;
            else return transform.rotation * Quaternion.Inverse(localRotation);
        }


        /// <summary>
        /// 将世界坐标转换为路径本地坐标
        /// 物体的缩放被忽略, 使用路径的 worldScale 替代
        /// </summary>
        /// <param name="worldPoint"> 世界坐标 </param>
        /// <returns> 世界坐标对应的路径本地点 </returns>
        public Vector3 InverseTransformPoint(Vector3 worldPoint)
        {
            return transform.InverseTransformDirection(worldPoint - transform.position) / _worldScale;
        }


        /// <summary>
        /// 将世界向量转换为路径本地向量
        /// 物体的缩放被忽略, 使用路径的 worldScale 
        /// 转换前后向量长度可能发生变化
        /// </summary>
        /// <param name="worldVector"> 世界向量 </param>
        /// <returns> 世界向量对应的路径本地向量 </returns>
        public Vector3 InverseTransformVector(Vector3 worldVector)
        {
            return transform.InverseTransformDirection(worldVector) / _worldScale;
        }


        /// <summary>
        /// 将世界方向转换为路径本地方向
        /// 物体的缩放被忽略, 路径的 worldScale 符号影响转换结果
        /// 转换前后向量长度不变
        /// </summary>
        /// <param name="worldDirection"> 世界方向 </param>
        /// <returns> 世界方向对应的路径本地方向 </returns>
        public Vector3 InverseTransformDirection(Vector3 worldDirection)
        {
            return transform.InverseTransformDirection(worldDirection) * Mathf.Sign(_worldScale);
        }


        /// <summary>
        /// 将世界旋转转换为路径本地旋转
        /// 路径的 worldScale 符号影响转换结果
        /// </summary>
        /// <param name="worldRotation"> 世界旋转 </param>
        /// <returns> 世界旋转对应的本地旋转 </returns>
        public Quaternion InverseTransformRotation(Quaternion worldRotation)
        {
            if (_worldScale >= 0f) return Quaternion.Inverse(transform.rotation) * worldRotation;
            else return Quaternion.Inverse(Quaternion.Inverse(transform.rotation) * worldRotation);
        }


        /// <summary>
        /// 获取路径上指定位置的点坐标
        /// </summary>
        /// <param name="location"> 路径位置 </param>
        /// <param name="space"> 参数或返回值的相对空间 </param>
        /// <returns> 指定位置的点坐标 </returns>
        public Vector3 GetPoint(Location location, Space space = Space.World)
        {
            Vector3 point = this[location.index].GetPoint(location.time);
            if (space == Space.Self) return point;
            else return TransformPoint(point);
        }


        /// <summary>
        /// 获取路径上指定位置的一阶导数
        /// </summary>
        /// <param name="location"> 路径位置 </param>
        /// <param name="space"> 参数或返回值的相对空间 </param>
        /// <returns> 指定位置的一阶导数 </returns>
        public Vector3 GetDerivative(Location location, Space space = Space.World)
        {
            Vector3 derivative = this[location.index].GetDerivative(location.time);
            if (space == Space.Self) return derivative;
            else return TransformVector(derivative);
        }


        /// <summary>
        /// 获取路径上指定位置的二阶导数
        /// </summary>
        /// <param name="location"> 路径位置 </param>
        /// <param name="space"> 参数或返回值的相对空间 </param>
        /// <returns> 指定位置的二阶导数 </returns>
        public Vector3 GetSecondDerivative(Location location, Space space = Space.World)
        {
            Vector3 secondDerivative = this[location.index].GetSecondDerivative(location.time);
            if (space == Space.Self) return secondDerivative;
            else return TransformVector(secondDerivative);
        }


        /// <summary>
        /// 获取路径上指定位置的切线. 切线是单位向量, 如果切线不存在返回 Vector3.zero
        /// </summary>
        /// <param name="location"> 路径位置 </param>
        /// <param name="space"> 参数或返回值的相对空间 </param>
        /// <returns> 指定位置的切线 </returns>
        public Vector3 GetTangent(Location location, Space space = Space.World)
        {
            Vector3 tangent = this[location.index].GetTangent(location.time);
            if (space == Space.Self) return tangent;
            else return TransformDirection(tangent);
        }


        /// <summary>
        /// 获取从路径起点到路径上指定位置的路径长度
        /// </summary>
        /// <param name="location"> 路径位置 </param>
        /// <param name="space"> 参数或返回值的相对空间 </param>
        public float GetLength(Location location, Space space = Space.World)
        {
            ValidatePathLength(location.index);
            float len = this[location.index].GetLength(location.time);
            if (location.index != 0)
            {
                len += this[location.index - 1].pathLength;
            }
            if (space == Space.Self) return len;
            else return len * absWorldScale;
        }


        /// <summary>
        /// 根据从路径起点开始的路径长度, 获取路径上的位置
        /// </summary>
        /// <param name="length"> 从路径起点开始的路径长度. 对于环状路径, 该长度可以为负值或大于路径长度的值 </param>
        /// <param name="startSegmentIndex"> 建议起始查找的路径段索引（负值表示无建议）</param>
        /// <param name="space"> 参数或返回值的相对空间 </param>
        /// <returns> 路径在指定长度处的位置 </returns>
        public Location GetLocationByLength(float length, int startSegmentIndex = -1, Space space = Space.World)
        {
            int lastSegmentIndex = segmentCount - 1;
            ValidatePathLength(lastSegmentIndex);
            float totalLength = this[lastSegmentIndex].pathLength;

            if (space == Space.World) length /= absWorldScale;

            Location location = default;

            // 处理计算长度
            if (circular)
            {
                length = (totalLength + length % totalLength) % totalLength;
            }
            else
            {
                if (length <= 0)
                {
                    return location;
                }

                if (length >= totalLength)
                {
                    location.index = lastSegmentIndex;
                    location.time = 1f;
                    return location;
                }
            }

            // 如果建议开始索引无效, 则重新估算开始索引
            if (startSegmentIndex < 0 || startSegmentIndex > lastSegmentIndex)
            {
                location.index = (int)(length / totalLength * lastSegmentIndex);
            }
            else
            {
                location.index = startSegmentIndex;
            }

            // 查找样条索引, 并计算样条位置 t
            if (this[location.index].pathLength > length)
            {
                do
                {
                    if (location.index == 0)
                    {
                        location.time = this[0].GetLocationByLength(length);
                        return location;
                    }

                } while (this[--location.index].pathLength > length);

                location.time = this[location.index + 1].GetLocationByLength(length - this[location.index].pathLength);
                location.index++;
            }
            else
            {
                while (this[++location.index].pathLength < length) ;

                location.time = this[location.index].GetLocationByLength(length - this[location.index - 1].pathLength);
            }

            return location;
        }


        /// <summary>
        /// 求路径上与给定点最近的位置
        /// </summary>
        /// <param name="point"> 给定点坐标 </param>
        /// <param name="stepLength"> 每分步长度 </param>
        /// <param name="space"> 参数或返回值的相对空间 </param>
        public Location GetClosestLocation(Vector3 point, float stepLength, Space space = Space.World)
        {
            if (space == Space.World)
            {
                point = InverseTransformPoint(point);
                stepLength /= absWorldScale;
            }

            float bestSqrMagnitude = float.MaxValue;
            float currentSqrMagnitude;
            float currentTime;

            Location location = new Location();
            Node segment;

            int count = segmentCount;
            for (int i = 0; i < count; i++)
            {
                segment = this[i];
                currentTime = segment.GetClosestLocation(point, Mathf.CeilToInt(segment.length / stepLength));
                currentSqrMagnitude = (segment.GetPoint(currentTime) - point).sqrMagnitude;

                if (currentSqrMagnitude < bestSqrMagnitude)
                {
                    location.index = i;
                    location.time = currentTime;
                    bestSqrMagnitude = currentSqrMagnitude;
                }
            }

            return location;
        }


        public virtual void SetTransform(Transform target, float length, ref Location location)
        {
            location = GetLocationByLength(length, location.index);
            target.position = GetPoint(location);
        }


        protected static void CopyBaseData(Path path1, Path path2)
        {
            path1._worldScale = path2._worldScale;
            path1._circular = path2._circular;
            path1._localLengthError = path2._localLengthError;
            path1._firstInvalidPathLengthIndex = path2._firstInvalidPathLengthIndex;
        }

    } // class Path


    /// <summary>
    /// 泛型路径基类
    /// </summary>
    public abstract class Path<Node> : Path where Node : Path.Node, ICopyable<Node>, new()
    {
        [SerializeField] List<Node> _nodes = default;


        public sealed override int nodeCount => _nodes.Count;
        protected sealed override Path.Node this[int i] => _nodes[i];


        protected int circularIndex(int i) => (i + _nodes.Count) % _nodes.Count;
        protected Node node(int i) => _nodes[i];
        protected Node circularNode(int i) => _nodes[(i + _nodes.Count) % _nodes.Count];


        // Node List 插入节点
        protected Node InsertNodeInternal(int nodeIndex)
        {
            var node = new Node();
            node.lengthError = localLengthError;

            _nodes.Insert(nodeIndex, node);
            return node;
        }


        // Node List 移除节点
        protected void RemoveNodeInternal(int nodeIndex)
        {
            _nodes.RemoveAt(nodeIndex);
        }


        /// <summary>
        /// 重置 (初始化) 路径
        /// </summary>
        // 实现时注意：子类实现需保证至少含有一个有效路径段
        public override void Reset()
        {
            base.Reset();
            _nodes = new List<Node>(8);
        }


        protected static void Copy<N1, N2>(Path<N1> path1, Path<N2> path2)
            where N1 : Node, ICopyable<N1>, new()
            where N2 : Node, ICopyable<N2>, new()
        {
            int count = path2._nodes.Count;
            path1._nodes = new List<N1>(count);
            for (int i = 0; i < count; i++)
            {
                var node = new N1();
                (node as ICopyable<Node>).Copy(path2._nodes[i]);
                path1._nodes.Add(node);
            }

            CopyBaseData(path1, path2);
        }

    } // class Path<Node>

} // namespace UnityExtensions.Paths