using System;
using UnityEngine;

namespace UnityExtensions.Paths
{
    /// <summary>
    /// 贝塞尔路径节点
    /// </summary>
    [Serializable]
    public class BezierNode : Path.Node, ICopyable<BezierNode>
    {
        public Vector3 position;
        [SerializeField] Vector3 _forwardTangent = Vector3.forward;
        [SerializeField] Vector3 _backTangent = Vector3.back;
        [SerializeField] bool _broken = true;


        public Vector3 forwardTangent
        {
            get { return _forwardTangent; }
            set
            {
                _forwardTangent = value;
                OnForwardTangentChanged();
                if (!_broken)
                {
                    float length = value.magnitude;
                    if (length > MathUtilities.OneMillionth)
                    {
                        _backTangent = -_backTangent.magnitude / length * value;
                        OnBackTangentChanged();
                    }
                }
            }
        }


        public Vector3 backTangent
        {
            get { return _backTangent; }
            set
            {
                _backTangent = value;
                OnBackTangentChanged();
                if (!_broken)
                {
                    float length = value.magnitude;
                    if (length > MathUtilities.OneMillionth)
                    {
                        _forwardTangent = -_forwardTangent.magnitude / length * value;
                        OnForwardTangentChanged();
                    }
                }
            }
        }


        public Vector3 forwardControlPoint
        {
            get { return position + _forwardTangent; }
            set { forwardTangent = value - position; }
        }


        public Vector3 backControlPoint
        {
            get { return position + _backTangent; }
            set { backTangent = value - position; }
        }


        public bool broken
        {
            get { return _broken; }
            set
            {
                if (_broken != value)
                {
                    _broken = value;
                    if (!value)
                    {
                        float forwardLength = _forwardTangent.magnitude;
                        float backLength = _backTangent.magnitude;
                        if (forwardLength > MathUtilities.OneMillionth && backLength > MathUtilities.OneMillionth)
                        {
                            _forwardTangent = Vector3.Slerp(_forwardTangent,
                                -forwardLength / backLength * _backTangent,
                                backLength / (forwardLength + backLength));

                            OnForwardTangentChanged();

                            _backTangent = -backLength / forwardLength * _forwardTangent;

                            OnBackTangentChanged();
                        }
                    }
                }
            }
        }


        protected virtual void OnForwardTangentChanged() { }
        protected virtual void OnBackTangentChanged() { }


        public void Copy(BezierNode target)
        {
            base.Copy(target);
            position = target.position;
            _forwardTangent = target._forwardTangent;
            _backTangent = target._backTangent;
            _broken = target._broken;
        }

    } // class BezierNode

} // namespace UnityExtensions.Paths