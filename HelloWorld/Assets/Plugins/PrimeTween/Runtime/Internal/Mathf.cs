using System;
using System.Runtime.CompilerServices;

namespace PrimeTween {
    internal static class Mathf {
        internal const float PI = 3.1415927f;
        static volatile float FloatMinNormal = 1.1754944E-38f;
        static volatile float FloatMinDenormal = float.Epsilon;
        static bool IsFlushToZeroEnabled = FloatMinDenormal == 0.0;
        static readonly float Epsilon = IsFlushToZeroEnabled ? FloatMinNormal : FloatMinDenormal;

        internal static float Min(float a, float b) => a < b ? a : b;
        internal static float Max(float a, float b) => a > b ? a : b;
        internal static bool Approximately(float a, float b) => Abs(b - a) < Max(1E-06f * Max(Abs(a), Abs(b)), Epsilon * 8f);
        internal static float Abs(float f) => f < 0f ? -f : f;
        internal static int Abs(int value) => Math.Abs(value);
        internal static float InverseLerp(float a, float b, float value) => a != b ? Clamp01((value - a) / (b - a)) : 0.0f;

        internal static float Clamp01(float value)
        {
            if (value < 0.0)
                return 0.0f;
            return value > 1.0 ? 1f : value;
        }

        internal static float Sqrt(float f) => (float) Math.Sqrt(f);
        internal static float Lerp(float a, float b, float t) => a + (b - a) * Clamp01(t);
        internal static float Pow(float f, float p) => (float)Math.Pow(f, p);
        internal static float Asin(float f) => (float)Math.Asin(f);
        internal static float Sin(float f) => (float)Math.Sin(f);
        internal static float Cos(float f) => (float) Math.Cos(f);
        internal static float Acos(float f) => (float)Math.Acos(f);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static float Sign(float f) => f >= 0.0 ? 1f : -1f;

        internal static float Clamp(float value, float min, float max)
        {
            if (value < min)
                value = min;
            else if (value > max)
                value = max;
            return value;
        }

        internal static int RoundToInt(float f) => (int)Math.Round(f);
        internal static float LerpUnclamped(float a, float b, float t) => a + (b - a) * t;
    }
}
