using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityExtensions
{
    /// <summary>
    /// Extensions for Random.
    /// </summary>
    public static partial class Extensions
    {
        public static Vector2 OnUnitCircle(this ref Random random)
        {
            double a = random.Next01() * MathUtilities.TwoPi;
            return new Vector2((float)Math.Cos(a), (float)Math.Sin(a));
        }


        public static Vector3 OnUnitSphere(this ref Random random)
        {
            // http://mathworld.wolfram.com/SpherePointPicking.html

            double a = random.Next01() * MathUtilities.TwoPi;
            double cosB = random.Next01() * 2 - 1;
            double sinB = Math.Sqrt(1 - cosB * cosB);

            return new Vector3(
                (float)(Math.Cos(a) * sinB),
                (float)cosB,
                (float)(Math.Sin(a) * sinB));
        }


        public static Vector2 InsideUnitCircle(this ref Random random)
        {
            return random.OnUnitCircle() * (float)Math.Sqrt(random.Next01());
        }


        public static Vector3 InsideUnitSphere(this ref Random random)
        {
            return random.OnUnitSphere() * (float)Math.Pow(random.Next01(), 1.0 / 3.0);
        }


        public static Vector2 InsideEllipse(this ref Random random, Vector2 radius)
        {
            return Vector2.Scale(random.InsideUnitCircle(), radius);
        }


        public static Vector3 InsideEllipsoid(this ref Random random, Vector3 radius)
        {
            return Vector3.Scale(random.InsideUnitSphere(), radius);
        }


        public static float InsideRange(this ref Random random, Range range)
        {
            return random.Range(range.min, range.max);
        }


        public static Vector2 InsideRange2(this ref Random random, Range2 range2)
        {
            return new Vector2(
                random.Range(range2.x.min, range2.x.max),
                random.Range(range2.y.min, range2.y.max));
        }


        public static Vector3 InsideRange3(this ref Random random, Range3 range3)
        {
            return new Vector3(
                random.Range(range3.x.min, range3.x.max),
                random.Range(range3.y.min, range3.y.max),
                random.Range(range3.z.min, range3.z.max));
        }


        /// <summary>
        /// Choose one element in a group. If sum of probabilities is smaller than 1, the probability of last element will be increased.
        /// </summary>
        /// <param name="getProbability"> The probability of every element. </param>
        /// <returns> The index of the chosen one. </returns>
        public static int Choose(this ref Random random, Func<int, float> getProbability, int startIndex, int count)
        {
            int lastIndex = startIndex + count - 1;
            float rest = (float)random.Next01();
            float current;

            for (; startIndex < lastIndex; startIndex++)
            {
                current = getProbability(startIndex);
                if (rest < current) return startIndex;
                else rest -= current;
            }

            return lastIndex;
        }


        /// <summary>
        /// Choose one element in a group. If sum of probabilities is smaller than 1, the probability of last element will be increased.
        /// </summary>
        /// <param name="probabilities"> The probability of every element. </param>
        /// <param name="count"> A negative or zero value means all elements after start index. </param>
        /// <returns> The index of the chosen one. </returns>
        public static int Choose(this ref Random random, IList<float> probabilities, int startIndex = 0, int count = 0)
        {
            if (count < 1 || count > probabilities.Count - startIndex)
            {
                count = probabilities.Count - startIndex;
            }

            int lastIndex = startIndex + count - 1;
            float rest = (float)random.Next01();
            float current;

            for (; startIndex < lastIndex; startIndex++)
            {
                current = probabilities[startIndex];
                if (rest < current) return startIndex;
                else rest -= current;
            }

            return lastIndex;
        }


        /// <summary>
        /// Random sort the list.
        /// </summary>
        /// <param name="count"> A negative or zero value means all elements after start index. </param>
        public static void Sort<T>(this ref Random random, IList<T> list, int startIndex = 0, int count = 0)
        {
            int lastIndex = startIndex + count;
            if (lastIndex <= startIndex || lastIndex > list.Count)
            {
                lastIndex = list.Count;
            }

            lastIndex -= 1;

            T temp;
            int swapIndex;

            for (int i = startIndex; i < lastIndex; i++)
            {
                swapIndex = random.Range(i, lastIndex + 1);
                temp = list[i];
                list[i] = list[swapIndex];
                list[swapIndex] = temp;
            }
        }

    } // class Extensions

} // namespace UnityExtensions