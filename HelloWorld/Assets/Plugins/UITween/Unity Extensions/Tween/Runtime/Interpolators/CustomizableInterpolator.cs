using System;
using UnityEngine;

namespace UnityExtensions.Tween
{
    /// <summary>
    /// Customizable Interpolator
    /// </summary>
    [Serializable]
    public struct CustomizableInterpolator
    {
        public enum Type
        {
            Linear = 0,
            Accelerate,
            Decelerate,
            AccelerateDecelerate,
            Anticipate,
            Overshoot,
            AnticipateOvershoot,
            Bounce,
            Parabolic,
            Sine,

            CustomCurve = -1
        }


        public Type type;
        [Range(0, 1)]
        public float strength;
        public AnimationCurve customCurve;


        /// <summary>
        /// Calculate interpolation value
        /// </summary>
        /// <param name="t"> normalized time </param>
        /// <returns> result </returns>
        public float this[float t]
        {
            get { return type == Type.CustomCurve ? customCurve.Evaluate(t) : Interpolator._interpolators[(int)type](t, strength); }
        }


        public CustomizableInterpolator(Type type, float strength = 0.5f, AnimationCurve customCurve = null)
        {
            this.type = type;
            this.strength = strength;
            this.customCurve = customCurve;
        }

    } // struct CustomizableInterpolator

} // namespace UnityExtensions.Tween