using UnityEngine;

namespace UnityExtensions.Tween
{
    /// <summary>
    /// 预定义插值器
    /// </summary>
    public partial struct Interpolator
    {
        /// <summary>
        /// 线性插值
        /// </summary>
        /// <param name="t"> 单位化的时间, 即 [0, 1] 范围的数值 </param>
        /// <returns> 插值结果 </returns>
        public static float Linear(float t)
        {
            return t;
        }


        /// <summary>
        /// 加速插值
        /// </summary>
        /// <param name="t"> 单位化的时间, 即 [0, 1] 范围的数值 </param>
        /// <returns> 插值结果 </returns>
        public static float Accelerate(float t)
        {
            return t * t;
        }


        /// <summary>
        /// 加速插值 Weakly
        /// </summary>
        /// <param name="t"> 单位化的时间, 即 [0, 1] 范围的数值 </param>
        /// <returns> 插值结果 </returns>
        public static float AccelerateWeakly(float t)
        {
            return t * t * (2f - t);
        }


        /// <summary>
        /// 加速插值 Strongly
        /// </summary>
        /// <param name="t"> 单位化的时间, 即 [0, 1] 范围的数值 </param>
        /// <returns> 插值结果 </returns>
        public static float AccelerateStrongly(float t)
        {
            return t * t * t;
        }


        /// <summary>
        /// 加速插值
        /// </summary>
        /// <param name="t"> 单位化的时间, 即 [0, 1] 范围的数值 </param>
        /// <param name="strength"> [0, 1] 范围的强度 </param>
        /// <returns> 插值结果 </returns>
        public static float Accelerate(float t, float strength)
        {
            return t* t * ((2f - t) * (1f - strength) + t * strength);
        }


        /// <summary>
        /// 减速插值
        /// </summary>
        /// <param name="t"> 单位化的时间, 即 [0, 1] 范围的数值 </param>
        /// <returns> 插值结果 </returns>
        public static float Decelerate(float t)
        {
            return (2f - t) * t;
        }


        /// <summary>
        /// 减速插值 Weakly
        /// </summary>
        /// <param name="t"> 单位化的时间, 即 [0, 1] 范围的数值 </param>
        /// <returns> 插值结果 </returns>
        public static float DecelerateWeakly(float t)
        {
            t = 1f - t;
            return 1f - t * t * (2f - t);
        }


        /// <summary>
        /// 减速插值 Strongly
        /// </summary>
        /// <param name="t"> 单位化的时间, 即 [0, 1] 范围的数值 </param>
        /// <returns> 插值结果 </returns>
        public static float DecelerateStrongly(float t)
        {
            t = 1f - t;
            return 1f - t * t * t;
        }


        /// <summary>
        /// 减速插值
        /// </summary>
        /// <param name="t"> 单位化的时间, 即 [0, 1] 范围的数值 </param>
        /// <param name="strength"> [0, 1] 范围的强度 </param>
        /// <returns> 插值结果 </returns>
        public static float Decelerate(float t, float strength)
        {
            t = 1f - t;
            return 1f - t * t * ((2f - t) * (1f - strength) + t * strength);
        }


        /// <summary>
        /// 先加速后减速插值
        /// </summary>
        /// <param name="t"> 单位化的时间, 即 [0, 1] 范围的数值 </param>
        /// <returns> 插值结果 </returns>
        public static float AccelerateDecelerate(float t)
        {
            return (3f - t - t) * t * t; 
        }


        /// <summary>
        /// 先加速后减速插值 Weakly
        /// </summary>
        /// <param name="t"> 单位化的时间, 即 [0, 1] 范围的数值 </param>
        /// <returns> 插值结果 </returns>
        public static float AccelerateDecelerateWeakly(float t)
        {
            float tt = t * t;
            return ((-6f * t + 15f) * tt - 14f * t + 6f) * tt;
        }


        /// <summary>
        /// 先加速后减速插值 Strongly
        /// </summary>
        /// <param name="t"> 单位化的时间, 即 [0, 1] 范围的数值 </param>
        /// <returns> 插值结果 </returns>
        public static float AccelerateDecelerateStrongly(float t)
        {
            return ((6f * t - 15f) * t + 10f) * t * t * t;
        }


        /// <summary>
        /// 先加速后减速插值
        /// </summary>
        /// <param name="t"> 单位化的时间, 即 [0, 1] 范围的数值 </param>
        /// <param name="strength"> [0, 1] 范围的强度 </param>
        /// <returns> 插值结果 </returns>
        public static float AccelerateDecelerate(float t, float strength)
        {
            float tt = t * t;
            float ttt6_15tt = (6f * t - 15f) * tt;
            return ((6f - ttt6_15tt - 14f * t) * (1f - strength) + (ttt6_15tt + 10f * t) * strength) * tt;
        }


        /// <summary>
        /// 反弹加速插值
        /// </summary>
        /// <param name="t"> 单位化的时间, 即 [0, 1] 范围的数值 </param>
        /// <param name="strength"> [0, 1] 范围的强度 </param>
        /// <returns> 插值结果 </returns>
        public static float Anticipate(float t, float strength = 0.5f)
        {
            float a = 2f + strength * 2f;
            return (a * t - a + 1f) * t * t;
        }


        /// <summary>
        /// 减速反弹插值
        /// </summary>
        /// <param name="t"> 单位化的时间, 即 [0, 1] 范围的数值 </param>
        /// <param name="strength"> [0, 1] 范围的强度 </param>
        /// <returns> 插值结果 </returns>
        public static float Overshoot(float t, float strength = 0.5f)
        {
            t = 1f - t;
            float a = 2f + strength * 2f;
            return 1f - (a * t - a + 1f) * t * t;
        }


        /// <summary>
        /// 先反弹加速后减速反弹插值
        /// </summary>
        /// <param name="t"> 单位化的时间, 即 [0, 1] 范围的数值 </param>
        /// <param name="strength"> [0, 1] 范围的强度 </param>
        /// <returns> 插值结果 </returns>
        public static float AnticipateOvershoot(float t, float strength = 0.5f)
        {
            float d = -6f - 12f * strength;
            return ((((6f - d - d) * t + (5f * d - 15f)) * t + (10f - 4f * d)) * t + d) * t * t;
        }


        /// <summary>
        /// 弹跳插值
        /// </summary>
        /// <param name="t"> 单位化的时间, 即 [0, 1] 范围的数值 </param>
        /// <param name="strength"> [0, 1] 范围的强度 </param>
        /// <returns> 插值结果 </returns>
        public static float Bounce(float t, float strength = 0.5f)
        {
            float k = 0.3f + 0.4f * strength;
            float kk = k * k;
            float a = 1f + (k + k) * (1f + k + kk);

            float tmp;

            if (t < 1f / a)
            {
                tmp = a * t;
                return tmp * tmp;
            }
            if (t < (1f + k + k) / a)
            {
                tmp = a * t - 1f - k;
                return 1f - kk + tmp * tmp;
            }
            if (t < (1f + (k + kk) * 2f) / a)
            {
                tmp = a * t - 1f - k - k - kk;
                return 1f - kk * kk + tmp * tmp;
            }

            tmp = a * t - 1f - 2 * (k + kk) - kk * k;
            return 1f - kk * kk * kk + tmp * tmp;
        }


        /// <summary>
        /// 抛物线插值
        /// </summary>
        /// <param name="t"> 单位化的时间, 即 [0, 1] 范围的数值 </param>
        /// <returns> 插值结果 </returns>
        public static float Parabolic(float t)
        {
            return 4f * t * (1f - t);
        }


        /// <summary>
        /// 正弦插值
        /// </summary>
        /// <param name="t"> 单位化的时间, 即 [0, 1] 范围的数值 </param>
        /// <returns> 插值结果 </returns>
        public static float Sine(float t)
        {
            return Mathf.Sin((t + t + 1.5f) * Mathf.PI) * 0.5f + 0.5f;
        }

    } // struct Interpolator

} // namespace UnityExtensions.Tween