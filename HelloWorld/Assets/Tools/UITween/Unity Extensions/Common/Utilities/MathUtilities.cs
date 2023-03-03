using System;
using UnityEngine;

namespace UnityExtensions
{
    /// <summary>
    /// MathUtilities
    /// </summary>
    public struct MathUtilities
    {
        public const float Sqrt2 = 1.41421356f;
        public const float Sqrt3 = 1.73205081f;
        public const float TwoPi = 6.28318531f;
        public const float HalfPi = 1.57079633f;
        public const float OneMillionth = 1e-6f;
        public const float Million = 1e6f;


        public static Vector3 Add(Vector3 a, float b)
        {
            a.x += b;
            a.y += b;
            a.z += b;
            return a;
        }


        public static Vector3 Sub(Vector3 a, float b)
        {
            a.x -= b;
            a.y -= b;
            a.z -= b;
            return a;
        }


        public static Vector2 Add(Vector2 a, float b)
        {
            a.x += b;
            a.y += b;
            return a;
        }


        public static Vector2 Sub(Vector2 a, float b)
        {
            a.x -= b;
            a.y -= b;
            return a;
        }


        public static Vector2 Clamp01(Vector2 value)
        {
            value.x = Mathf.Clamp01(value.x);
            value.y = Mathf.Clamp01(value.y);
            return value;
        }


        public static Vector3 Clamp01(Vector3 value)
        {
            value.x = Mathf.Clamp01(value.x);
            value.y = Mathf.Clamp01(value.y);
            value.z = Mathf.Clamp01(value.z);
            return value;
        }


        public static Vector2 Clamp(Vector2 value, float min, float max)
        {
            value.x = Mathf.Clamp(value.x, min, max);
            value.y = Mathf.Clamp(value.y, min, max);
            return value;
        }


        public static Vector2 Clamp(Vector2 value, Vector2 min, Vector2 max)
        {
            value.x = Mathf.Clamp(value.x, min.x, max.x);
            value.y = Mathf.Clamp(value.y, min.y, max.y);
            return value;
        }


        public static Vector3 Clamp(Vector3 value, float min, float max)
        {
            value.x = Mathf.Clamp(value.x, min, max);
            value.y = Mathf.Clamp(value.y, min, max);
            value.z = Mathf.Clamp(value.z, min, max);
            return value;
        }


        public static Vector3 Clamp(Vector3 value, Vector3 min, Vector3 max)
        {
            value.x = Mathf.Clamp(value.x, min.x, max.x);
            value.y = Mathf.Clamp(value.y, min.y, max.y);
            value.z = Mathf.Clamp(value.z, min.z, max.z);
            return value;
        }


        /// <summary>
        /// 2^n
        /// </summary>
        public static double Exp2(double n)
        {
            return Math.Exp(n * 0.69314718055994530941723212145818);
        }


        /// <summary>
        /// Keeps the specified significant digits and rounds off the rest.
        /// (a double has 15-17 significant digits)
        /// </summary>
        public static double RoundToSignificantDigits(double value, int digits)
        {
            if (value == 0.0) return 0.0;

            int intDigits = (int)Math.Floor(Math.Log10(Math.Abs(value))) + 1;

            if (intDigits <= digits) return Math.Round(value, digits - intDigits);

            double scale = Math.Pow(10, intDigits - digits);

            return Math.Round(value / scale) * scale;
        }


        /// <summary>
        /// Keeps the specified significant digits and rounds off the rest.
        /// (a float has 6-9 significant digits)
        /// </summary>
        public static float RoundToSignificantDigitsFloat(float value, int digits)
        {
            return (float)RoundToSignificantDigits(value, digits);
        }


        /// <summary>
        /// Linear map to 0-1
        /// </summary>
        public static float Linear01(float value, float min, float max)
        {
            return (value - min) / (max - min);
        }


        /// <summary>
        /// Linear map to 0-1 (Clamped)
        /// </summary>
        public static float Linear01Clamped(float value, float min, float max)
        {
            return Mathf.Clamp01((value - min) / (max - min));
        }


        /// <summary>
        /// Linear map to outputMin-outputMax
        /// </summary>
        public static float Linear(float value, float min, float max, float outputMin, float outputMax)
        {
            return (value - min) / (max - min) * (outputMax - outputMin) + outputMin;
        }


        /// <summary>
        /// Linear map to outputMin-outputMax (Clamped)
        /// </summary>
        public static float LinearClamped(float value, float min, float max, float outputMin, float outputMax)
        {
            return Mathf.Clamp01((value - min) / (max - min)) * (outputMax - outputMin) + outputMin;
        }


        public static Rect Lerp(Rect a, Rect b, float t)
        {
            return new Rect(
                Vector2.Lerp(a.position, b.position, t),
                Vector2.Lerp(a.size, b.size, t));
        }


        /// <summary>
        /// Cross two Vector2
        /// </summary>
        /// <returns> The z value of the result Vector3 (x, y are zero) </returns>
        public static float Cross(Vector2 lhs, Vector2 rhs)
        {
            return lhs.x * rhs.y - lhs.y * rhs.x;
        }


        /// <summary>
        /// Project a point onto a plane.
        /// </summary>
        public static Vector3 ProjectOnPlane(Vector3 point, Vector3 planePoint, Vector3 planeNormal)
        {
            float normalSqrMagnitude = planeNormal.sqrMagnitude;
            if (normalSqrMagnitude == 0) return point;
            return Vector3.Dot(planePoint - point, planeNormal) / normalSqrMagnitude * planeNormal + point;
        }


        /// <summary>
        /// Get the point of intersection of a ray and  a plane
        /// </summary>
        /// <returns> 0 means non result, -1 means the result on the inversed direction of ray，1 means normal result </returns>
        public static int RayIntersectPlane(Vector3 rayOrigin, Vector3 rayDirection, Vector3 planePoint, Vector3 planeNormal, out Vector3 result)
        {
            float cos = Vector3.Dot(planeNormal, rayDirection);
            float distance = Vector3.Dot(rayOrigin - planePoint, planeNormal);

            if (cos < 0f)
            {
                if (distance >= 0f)
                {
                    result = rayOrigin + distance / cos * rayDirection;
                    return 1;
                }
            }
            else if (cos > 0f)
            {
                if (distance <= 0f)
                {
                    result = rayOrigin - distance / cos * rayDirection;
                    return -1;
                }
            }

            result = rayOrigin;
            return 0;
        }


        /// <summary>
        /// Get the closest point to the specified point on a ray. Returned value is 't' in "origin + direction * t".
        /// </summary>
        public static float ClosestPointOnRayFactor(Vector3 point, Vector3 origin, Vector3 direction)
        {
            float t = direction.sqrMagnitude;
            if (t == 0f) return 0f;

            return Mathf.Max(Vector3.Dot(point - origin, direction) / t, 0f);
        }


        /// <summary>
        /// Get the closest point to the specified point on a ray.
        /// </summary>
        public static Vector3 ClosestPointOnRay(Vector3 point, Vector3 origin, Vector3 direction)
        {
            return origin + direction * ClosestPointOnRayFactor(point, origin, direction);
        }


        /// <summary>
        /// Get the closest point to the specified point on a segment. Returned value is 't' in "start + (end - start) * t".
        /// </summary>
        public static float ClosestPointOnSegmentFactor(Vector2 point, Vector2 start, Vector2 end)
        {
            Vector2 direction = end - start;

            float t = direction.sqrMagnitude;
            if (t == 0f) return 0f;

            return Mathf.Clamp01(Vector2.Dot(point - start, direction) / t);
        }


        /// <summary>
        /// Get the closest point to the specified point on a segment.
        /// </summary>
        public static Vector2 ClosestPointOnSegment(Vector2 point, Vector2 start, Vector2 end)
        {
            return start + (end - start) * ClosestPointOnSegmentFactor(point, start, end);
        }


        /// <summary>
        /// Get the closest point to the specified point on a segment. Returned value is 't' in "start + (end - start) * t".
        /// </summary>
        public static float ClosestPointOnSegmentFactor(Vector3 point, Vector3 start, Vector3 end)
        {
            Vector3 direction = end - start;

            float t = direction.sqrMagnitude;
            if (t == 0f) return 0f;

            return Mathf.Clamp01(Vector3.Dot(point - start, direction) / t);
        }


        /// <summary>
        /// Get the closest point to the specified point on a segment.
        /// </summary>
        public static Vector3 ClosestPointOnSegment(Vector3 point, Vector3 start, Vector3 end)
        {
            return start + (end - start) * ClosestPointOnSegmentFactor(point, start, end);
        }



        /// <summary>
        /// Get the closest point inside a circle.
        /// </summary>
        public static Vector3 ClosestPointInCircle(Vector3 point, Vector3 center, Vector3 normal, float radius)
        {
            point = ProjectOnPlane(point, center, normal);
            normal = point - center;
            float sqrMagnitude = normal.sqrMagnitude;
            if (sqrMagnitude > radius * radius)
            {
                return radius / Mathf.Sqrt(sqrMagnitude) * normal + center;
            }
            else return point;
        }


        /// <summary>
        /// Get the closest point inside a sphere.
        /// </summary>
        public static Vector3 ClosestPointInSphere(Vector3 point, Vector3 center, float radius)
        {
            Vector3 direction = point - center;
            float sqrMagnitude = direction.sqrMagnitude;
            if (sqrMagnitude > radius * radius)
            {
                return radius / Mathf.Sqrt(sqrMagnitude) * direction + center;
            }
            else return point;
        }


        /// <summary>
        /// Get the closest point inside a axis aligned bounds.
        /// </summary>
        public static Vector3 ClosestPointInBounds(Vector3 point, Vector3 boundsMin, Vector3 boundsMax)
        {
            point.x = Mathf.Clamp(point.x, boundsMin.x, boundsMax.x);
            point.y = Mathf.Clamp(point.y, boundsMin.y, boundsMax.y);
            point.z = Mathf.Clamp(point.z, boundsMin.z, boundsMax.z);
            return point;
        }


        /// <summary>
        /// Get angle between a vector and a sector.
        /// </summary>
        public static float AngleBetweenVectorAndSector(Vector3 vector, Vector3 sectorNormal, Vector3 sectorDirection, float sectorAngle)
        {
            return Vector3.Angle(
                Vector3.RotateTowards(
                    sectorDirection,
                    Vector3.ProjectOnPlane(vector, sectorNormal),
                    sectorAngle * 0.5f * Mathf.Deg2Rad,
                    0f),
                vector);
        }


        /// <summary>
        /// Get the closest points on two segments.
        /// </summary>
        public static void ClosestPointBetweenSegments(
            Vector3 startA, Vector3 endA,
            Vector3 startB, Vector3 endB,
            out Vector3 pointA, out Vector3 pointB)
        {
            Vector3 directionA = endA - startA;
            Vector3 directionB = endB - startB;

            float k0 = Vector3.Dot(directionA, directionB);
            float k1 = directionA.sqrMagnitude;
            float k2 = Vector3.Dot(startA - startB, directionA);
            float k3 = directionB.sqrMagnitude;
            float k4 = Vector3.Dot(startA - startB, directionB);

            float t = k3 * k1 - k0 * k0;
            float a = (k0 * k4 - k3 * k2) / t;
            float b = (k1 * k4 - k0 * k2) / t;

            if (float.IsNaN(a) || float.IsNaN(b))
            {
                pointB = ClosestPointOnSegment(startB, endB, startA);
                pointA = ClosestPointOnSegment(startB, endB, endA);

                if ((pointB - startA).sqrMagnitude < (pointA - endA).sqrMagnitude)
                {
                    pointA = startA;
                }
                else
                {
                    pointB = pointA;
                    pointA = endA;
                }
                return;
            }

            if (a < 0f)
            {
                if (b < 0f)
                {
                    pointA = ClosestPointOnSegment(startA, endA, startB);
                    pointB = ClosestPointOnSegment(startB, endB, startA);

                    if ((pointA - startB).sqrMagnitude < (pointB - startA).sqrMagnitude)
                    {
                        pointB = startB;
                    }
                    else pointA = startA;
                }
                else if (b > 1f)
                {
                    pointA = ClosestPointOnSegment(startA, endA, endB);
                    pointB = ClosestPointOnSegment(startB, endB, startA);

                    if ((pointA - endB).sqrMagnitude < (pointB - startA).sqrMagnitude)
                    {
                        pointB = endB;
                    }
                    else pointA = startA;
                }
                else
                {
                    pointA = startA;
                    pointB = ClosestPointOnSegment(startB, endB, startA);
                }
            }
            else if (a > 1f)
            {
                if (b < 0f)
                {
                    pointA = ClosestPointOnSegment(startA, endA, startB);
                    pointB = ClosestPointOnSegment(startB, endB, endA);

                    if ((pointA - startB).sqrMagnitude < (pointB - endA).sqrMagnitude)
                    {
                        pointB = startB;
                    }
                    else pointA = endA;
                }
                else if (b > 1f)
                {
                    pointA = ClosestPointOnSegment(startA, endA, endB);
                    pointB = ClosestPointOnSegment(startB, endB, endA);

                    if ((pointA - endB).sqrMagnitude < (pointB - endA).sqrMagnitude)
                    {
                        pointB = endB;
                    }
                    else pointA = endA;
                }
                else
                {
                    pointA = endA;
                    pointB = ClosestPointOnSegment(startB, endB, endA);
                }
            }
            else
            {
                if (b < 0f)
                {
                    pointB = startB;
                    pointA = ClosestPointOnSegment(startA, endA, startB);
                }
                else if (b > 1f)
                {
                    pointB = endB;
                    pointA = ClosestPointOnSegment(startA, endA, endB);
                }
                else
                {
                    pointA = startA + a * directionA;
                    pointB = startB + b * directionB;
                }
            }
        }


        /// <summary>
        /// Create ordered dithering matrix.
        /// </summary>
        /// <param name="size"> Must be power of 2 (at least 2). </param>
        public static int[,] CreateOrderedDitheringMatrix(int size)
        {
            if (size <= 1 || !Mathf.IsPowerOfTwo(size))
            {
                Debug.LogError("Size of ordered dithering matrix must be larger than 1 and be power of 2.");
                return null;
            }

            int inputSize = 1;
            int outputSize;
            int[,] input = new int[,] { { 0 } };
            int[,] output;

            while (true)
            {
                outputSize = inputSize * 2;
                output = new int[outputSize, outputSize];
                for (int i=0; i<inputSize; i++)
                {
                    for (int j=0; j<inputSize; j++)
                    {
                        int value = input[i, j] * 4;
                        output[i, j] = value;
                        output[i + inputSize, j + inputSize] = value + 1;
                        output[i + inputSize, j] = value + 2;
                        output[i, j + inputSize] = value + 3;
                    }
                }

                if (outputSize >= size) return output;
                else
                {
                    inputSize = outputSize;
                    input = output;
                }
            }
        }

    } // struct MathUtilities

} // namespace UnityExtensions