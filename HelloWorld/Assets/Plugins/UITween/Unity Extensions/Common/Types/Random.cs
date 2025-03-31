using System;

namespace UnityExtensions
{
    /// <summary>
    /// Random (Use Random.Create to create a random object)
    /// </summary>
    [Serializable]
    public struct Random
    {
        /// <summary>
        /// seed
        /// </summary>
        public uint seed;


        // Internal static instance, used for assign seed for created instance
        static Random _static = Create((uint)(DateTime.Now.Ticks & 0x_FFFF_FFFF));


        // Update seed
        void Next()
        {
            //
            // https://en.wikipedia.org/wiki/Lehmer_random_number_generator
            //
            // 1.In original implementation，the value range of seed is [1, 2147483646], here we use (seed % 2147483646U + 1U) to convert the seed value into that range.
            // 2.Convert to UInt64 to avoid overflow.
            // 3.The result need be compatible with step 1, so minus 1 at last.
            //
            seed = (uint)((seed % 2147483646U + 1U) * 48271UL % 2147483647UL) - 1U;
        }


        /// <summary>
        /// Get next random double value in range [0, 1).
        /// <summary>
        public double Next01()
        {
            Next();

            //
            // Value range of seed is [0, 2147483645] now
            //

            uint value = (uint)(seed / 2147483646.0 * 1073741824.0);

            //value = ((value & 0xAAAA_AAAA) >> 1) | ((value & 0x5555_5555) << 1);

            value = ((value & 0x3FFF_8000) >> 15) | ((value & 0x0000_7FFF) << 15);

            return value / 1073741824.0;
        }


        /// <summary>
        /// Use a specified seed to create a Random instance.
        /// </summary>
        public static Random Create(uint seed)
        {
            return new Random { seed = seed };
        }


        /// <summary>
        /// Use a random seed to create a Random instance.
        /// The seeds are different even if create many instances in a very short time.
        /// </summary>
        public static Random Create()
        {
            _static.Next();
            return new Random { seed = ~_static.seed };
        }


        /// <summary>
        /// Get next random float value in range [0, 1).
        /// </summary>
        public float Range01()
        {
            return (float)Next01();
        }


        /// <summary>
        /// Get next random float value in a specified range.
        /// </summary>
        /// <param name="minValue"> The minimum value (included) </param>
        /// <param name="maxValue"> The maximum value (excluded) </param>
        public float Range(float minValue, float maxValue)
        {
            return minValue + (maxValue - minValue) * (float)Next01();
        }


        /// <summary>
        /// Get next random int value in a specified range.
        /// </summary>
        /// <param name="minValue"> The minimum value (included) </param>
        /// <param name="maxValue"> The maximum value (excluded) </param>
        public int Range(int minValue, int maxValue)
        {
            return minValue + (int)((maxValue - minValue) * Next01());
        }


        /// <summary>
        /// Test a random event with specified probability whether it occurs or not.
        /// </summary>
        /// <param name="probability"> [0f, 1f] </param>
        public bool Test(float probability)
        {
            return Next01() < probability;
        }


        /// <summary>
        /// Get next Gaussian Distribution value (The probabilities of this value in range of μ±σ, μ±2σ and μ±3σ are 68.27%, 95.45%, 99.73%).
        /// </summary>
        /// <param name="averageValue"> The average value of the distribution (μ in N(μ, σ^2)). </param>
        /// <param name="standardDeviation"> The standard deviation of the distribution (σ in N(μ, σ^2)). </param>
        /// <returns> The range of result is μ±∞ in theory. </returns>
        public float Gaussian(float averageValue, float standardDeviation)
        {
            //
            // https://en.wikipedia.org/wiki/Box-Muller_transform
            //
            return averageValue + standardDeviation * (float)
                (
                    Math.Sqrt(-2 * Math.Log(1 - Next01())) * Math.Sin(MathUtilities.TwoPi * Next01())
                );
        }

    } // struct Random

} // namespace UnityExtensions