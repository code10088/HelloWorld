using System;
using UnityEngine;

namespace UnityExtensions.Tween
{
    /// <summary>
    /// Interpolator
    /// </summary>
    [Serializable]
    public partial struct Interpolator
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
            Sine
        }


        public Type type;
        [Range(0, 1)]
        public float strength;


        internal static readonly Func<float, float, float>[] _interpolators =
        {
            (t, s) => t,
            Accelerate,
            Decelerate,
            AccelerateDecelerate,
            Anticipate,
            Overshoot,
            AnticipateOvershoot,
            Bounce,
            (t, s) => Parabolic(t),
            (t, s) => Sine(t)
        };


        /// <summary>
        /// Calculate interpolation value
        /// </summary>
        /// <param name="t"> normalized time </param>
        /// <returns> result </returns>
        public float this[float t]
        {
            get { return _interpolators[(int)type](t, strength); }
        }


        public Interpolator(Type type, float strength = 0.5f)
        {
            this.type = type;
            this.strength = strength;
        }

    } // struct Interpolator

} // namespace UnityExtensions.Tween