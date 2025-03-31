using UnityEngine;

namespace UnityExtensions
{
    /// <summary>
    /// ColorUtilities
    /// </summary>
    public struct ColorUtilities
    {
        /// <summary>
        /// Get the perceived brightness of a LDR color (alpha is ignored).
        /// (http://alienryderflex.com/hsp.html)
        /// </summary>
        public static float GetPerceivedBrightness(Color color)
        {
            return Mathf.Sqrt(
                0.241f * color.r * color.r +
                0.691f * color.g * color.g +
                0.068f * color.b * color.b);
        }


        /// <summary>
        /// Convert a LDR color value to an ARGB32 format uint value.
        /// </summary>
        public static uint ToARGB32(Color c)
        {
            return ((uint)(c.a * 255) << 24)
                 | ((uint)(c.r * 255) << 16)
                 | ((uint)(c.g * 255) << 8)
                 | ((uint)(c.b * 255));
        }


        /// <summary>
        /// Convert an ARGB32 format uint value to a color value.
        /// </summary>
        public static Color FromARGB32(uint argb)
        {
            return new Color(
                ((argb >> 16) & 0xFF) / 255f,
                ((argb >> 8) & 0xFF) / 255f,
                ((argb) & 0xFF) / 255f,
                ((argb >> 24) & 0xFF) / 255f);

        }


        /// <summary>
        /// Convert a hue value to a color vlue.
        /// 0-red; 0.167-yellow; 0.333-green; 0.5-cyan; 0.667-blue; 0.833-magenta; 1-red
        /// </summary>
        public static Color HueToColor(float hue)
        {
            return new Color(
                HueToGreen(hue + 1f / 3f),
                HueToGreen(hue),
                HueToGreen(hue - 1f / 3f));

            float HueToGreen(float h)
            {
                h = ((h % 1f + 1f) % 1f) * 6f;

                if (h < 1f) return h;
                if (h < 3f) return 1f;
                if (h < 4f) return (4f - h);
                return 0f;
            }
        }

    } // struct ColorUtilities

} // namespace UnityExtensions