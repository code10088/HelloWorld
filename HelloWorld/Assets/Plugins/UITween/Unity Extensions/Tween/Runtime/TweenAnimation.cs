#pragma warning disable CS0414

using System;
using UnityEngine;

namespace UnityExtensions.Tween
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public sealed class TweenAnimationAttribute : Attribute
    {
        public readonly string menu;
        public readonly string name;

        public TweenAnimationAttribute(string menu, string name)
        {
            this.menu = menu;
            this.name = name;
        }
    }

    [Serializable]
    public abstract partial class TweenAnimation
    {
        public bool enabled = true;

        [SerializeField]
        float _minNormalizedTime = 0f;

        [SerializeField]
        float _maxNormalizedTime = 1f;

        [SerializeField]
        bool _holdBeforeStart = true;

        [SerializeField]
        bool _holdAfterEnd = true;

        [SerializeField]
        CustomizableInterpolator _interpolator = default;

        [SerializeField]
        bool _foldout = true;   // Editor Only

        [SerializeField]
        string _comment = null; // Editor Only

        public float minNormalizedTime
        {
            get { return _minNormalizedTime; }
            set
            {
                _minNormalizedTime = Mathf.Clamp01(value);
                _maxNormalizedTime = Mathf.Clamp(_maxNormalizedTime, _minNormalizedTime, 1f);
            }
        }


        public float maxNormalizedTime
        {
            get { return _maxNormalizedTime; }
            set
            {
                _maxNormalizedTime = Mathf.Clamp01(value);
                _minNormalizedTime = Mathf.Clamp(_minNormalizedTime, 0f, _maxNormalizedTime);
            }
        }


        public bool holdBeforeStart
        {
            get => _holdBeforeStart;
            set => _holdBeforeStart = value;
        }


        public bool holdAfterEnd
        {
            get => _holdAfterEnd;
            set => _holdAfterEnd = value;
        }


        public void Sample(float normalizedTime)
        {
            if (normalizedTime < _minNormalizedTime)
            {
                if (_holdBeforeStart) normalizedTime = 0f;
                else return;
            }
            else if (normalizedTime > _maxNormalizedTime)
            {
                if (_holdAfterEnd) normalizedTime = 1f;
                else return;
            }
            else
            {
                if (_maxNormalizedTime == _minNormalizedTime) normalizedTime = 1f;
                else normalizedTime = (normalizedTime - _minNormalizedTime) / (_maxNormalizedTime - _minNormalizedTime);
            }

            Interpolate(_interpolator[normalizedTime]);
        }


        public abstract void Interpolate(float factor);

    } // class TweenAnimation

} // UnityExtensions.Tween
