using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityExtensions.Paths
{
    /// <summary>
    /// 三次样条
    /// </summary>
    [Serializable]
    public class CubicSpline : ICopyable<CubicSpline>
    {
        [SerializeField] Vector3 _f0 = default;                   // t^0 系数
        [SerializeField] Vector3 _f1 = default;                   // t^1 系数
        [SerializeField] Vector3 _f2 = default;                   // t^2 系数
        [SerializeField] Vector3 _f3 = default;                   // t^3 系数

        [SerializeField] float _lengthError = 0.01f;    // 长度误差
        [SerializeField] List<Vector2> _samples = default;        // t 对应长度的采样表

        const int _minSegments = 6;                     // 最小分段数
        const int _maxSegments = 1000000;               // 最大分段数
        const float _segmentsFactor = 0.2f;             // 分段系数


        public const float minLengthError = 0.001f;
        public const float maxLengthError = 1000f;


        /// <summary>
        /// Bezier Curve
        /// </summary>
        public void SetBezierCurve(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3)
        {
            p2 = (p2 - p1) * 3f;

            _f0 = p0;
            _f1 = 3f * (p1 - p0);
            _f2 = p2 - _f1;
            _f3 = p3 - p2 - p0;

            _samples?.Clear();
        }


        /// <summary>
        /// Cardinal Curve
        /// </summary>
        public void SetCardinalCurve(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float tension = 0.5f)
        {
            _f0 = p1;
            _f1 = (p2 - p0) * tension;

            p2 -= p1;
            p2 += p2;
            p3 = (p3 - p1) * tension;

            _f3 = p3 - p2 + _f1;
            _f2 = p2 * 0.5f - _f3 - _f1;

            _samples?.Clear();
        }


        /// <summary>
        /// 获取点坐标
        /// </summary>
        public Vector3 GetPoint(float t)
        {
            return _f0 + t * _f1 + t * t * _f2 + t * t * t * _f3;
        }


        /// <summary>
        /// 一阶导
        /// </summary>
        public Vector3 GetDerivative(float t)
        {
            return _f1 + 2f * t * _f2 + 3f * t * t * _f3;
        }


        /// <summary>
        /// 二阶导
        /// </summary>
        public Vector3 GetSecondDerivative(float t)
        {
            return _f2 + _f2 + 6f * t * _f3;
        }


        /// <summary>
        /// 获取切线. 切线是单位向量, 如果切线不存在返回 Vector3.zero
        /// </summary>
        public Vector3 GetTangent(float t)
        {
            Vector3 tangent = GetDerivative(t);
            if (tangent != Vector3.zero)
            {
                return tangent.normalized;
            }
            if (t < 0.020001f)
            {
                return (GetDerivative(t + 0.02f) * 2f - GetDerivative(t + 0.04f)).normalized;
            }
            if (t > 0.979999f)
            {
                return (GetDerivative(t - 0.02f) * 2f - GetDerivative(t - 0.04f)).normalized;
            }
            return (GetDerivative(t - 0.02f) + GetDerivative(t + 0.02f)).normalized;
        }


        /// <summary>
        /// 曲线总长度
        /// </summary>
        public float length
        {
            get
            {
                if (!isSamplesValid) ValidateSamples();
                return _samples[_samples.Count - 1].y;
            }
        }


        /// <summary>
        /// 长度误差
        /// </summary>
        public float lengthError
        {
            get { return _lengthError; }
            set
            {
                _lengthError = Mathf.Clamp(value, minLengthError, maxLengthError);
                _samples?.Clear();
            }
        }


        /// <summary>
        /// 采样数据是否有效
        /// </summary>
        public bool isSamplesValid
        {
            get { return _samples != null && _samples.Count != 0; }
        }


        /// <summary>
        /// 使采样数据无效
        /// </summary>
        public void InvalidateSamples()
        {
            _samples?.Clear();
        }


        /// <summary>
        /// 使采样数据有效
        /// </summary>
        public void ValidateSamples()
        {
            if (isSamplesValid) return;

            // 估算长度
            Vector3 lastPoint = _f0;
            Vector3 currentPoint;
            float length = 0f;

            for (int i = 1; i <= _minSegments; i++)
            {
                currentPoint = GetPoint(i / (float)_minSegments);
                length += (currentPoint - lastPoint).magnitude;
                lastPoint = currentPoint;
            }

            // 计算分段数
            int segments = Mathf.Clamp((int)(_segmentsFactor / _lengthError * length), _minSegments, _maxSegments);

            // 准备采样
            Vector2 lastSample = Vector2.zero;
            Vector2 lastValue = lastSample;
            Vector2 currentValue = lastValue;

            float minSlope = float.MinValue;
            float maxSlope = float.MaxValue;

            lastPoint = _f0;
            if (_samples == null) _samples = new List<Vector2>((int)(segments * 0.1f) + 4);
            _samples.Add(lastSample);

            // 执行采样
            for (int i = 1; i <= segments; i++)
            {
                // 计算当前值
                currentValue.x = i / (float)segments;
                currentPoint = GetPoint(currentValue.x);
                currentValue.y += (currentPoint - lastPoint).magnitude;
                lastPoint = currentPoint;

                // 更新斜率范围
                minSlope = Mathf.Max((currentValue.y - lastSample.y - _lengthError) / (currentValue.x - lastSample.x), minSlope);
                maxSlope = Mathf.Min((currentValue.y - lastSample.y + _lengthError) / (currentValue.x - lastSample.x), maxSlope);

                if (minSlope >= maxSlope)
                {
                    // 添加采样
                    _samples.Add(lastSample = lastValue);
                    minSlope = (currentValue.y - lastSample.y - _lengthError) / (currentValue.x - lastSample.x);
                    maxSlope = (currentValue.y - lastSample.y + _lengthError) / (currentValue.x - lastSample.x);
                }

                // 记录当前值
                lastValue = currentValue;
            }

            // 添加最后一个采样
            _samples.Add(lastValue);
        }


        /// <summary>
        /// 获取长度
        /// </summary>
        public float GetLength(float t)
        {
            if (!isSamplesValid) ValidateSamples();

            if (t >= 1f) return _samples[_samples.Count - 1].y;
            if (t <= 0f) return 0f;

            int index = (int)(t * _samples.Count);
            Vector2 start;

            if (_samples[index].x > t)
            {
                while (_samples[--index].x > t) ;
                start = _samples[index++];
            }
            else
            {
                while (_samples[++index].x < t) ;
                start = _samples[index - 1];
            }

            return start.y + (t - start.x) * (_samples[index].y - start.y) / (_samples[index].x - start.x);
        }


        /// <summary>
        /// 根据长度获取曲线位置参数 t
        /// </summary>
        public float GetLocationByLength(float length)
        {
            if (!isSamplesValid) ValidateSamples();

            if (length >= _samples[_samples.Count - 1].y) return 1f;
            if (length <= 0f) return 0f;

            int index = (int)(length / _samples[_samples.Count - 1].y * _samples.Count);
            Vector2 start;

            if (_samples[index].y > length)
            {
                while (_samples[--index].y > length) ;
                start = _samples[index++];
            }
            else
            {
                while (_samples[++index].y < length) ;
                start = _samples[index - 1];
            }

            return start.x + (length - start.y) * (_samples[index].x - start.x) / (_samples[index].y - start.y);
        }


        /// <summary>
        /// 求曲线上与给定点最近的点
        /// </summary>
        public float GetClosestLocation(Vector3 point, int segments)
        {
            Vector3 last = _f0;
            Vector3 current;
            float closest01;
            float sqrMagnitude;
            float bestSqrMagnitude = float.MaxValue;
            float bestT = 0f;

            for (int i = 1; i <= segments; i++)
            {
                current = GetPoint((float)i / segments);
                closest01 = MathUtilities.ClosestPointOnSegmentFactor(point, last, current);
                sqrMagnitude = (last + (current - last) * closest01 - point).sqrMagnitude;

                if (sqrMagnitude < bestSqrMagnitude)
                {
                    bestSqrMagnitude = sqrMagnitude;
                    bestT = (i - 1f + closest01) / segments;
                }

                last = current;
            }

            return bestT;
        }


        public void Copy(CubicSpline target)
        {
            _f0 = target._f0;
            _f1 = target._f1;
            _f2 = target._f2;
            _f3 = target._f3;

            _lengthError = target._lengthError;
            if (target._samples != null)
                _samples = new List<Vector2>(target._samples);
        }


#if UNITY_EDITOR

        // 在编辑器中绘制曲线
        public void Draw(Color color, float width)
        {
            var endPos = _f0 + _f1 + _f2 + _f3;
            var startTan = _f0 + _f1 / 3f;
            var endTan = _f0 + (_f1 + _f1 + _f2) / 3f;

            var zTest = UnityEditor.Handles.zTest;

            UnityEditor.Handles.zTest = UnityEngine.Rendering.CompareFunction.LessEqual;
            UnityEditor.Handles.DrawBezier(_f0, endPos, startTan, endTan, color, null, width);

            UnityEditor.Handles.zTest = UnityEngine.Rendering.CompareFunction.Greater;
            color.a *= 0.25f;
            UnityEditor.Handles.DrawBezier(_f0, endPos, startTan, endTan, color, null, width);

            UnityEditor.Handles.zTest = zTest;
        }

#endif

    } // class CubicSpline

} // namespace UnityExtensions.Paths