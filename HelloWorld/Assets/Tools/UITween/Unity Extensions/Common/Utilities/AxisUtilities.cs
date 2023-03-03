using UnityEngine;

namespace UnityExtensions
{
    /// <summary>
    /// Axis Utilities 
    /// </summary>
    public struct AxisUtilities
    {
        /// <summary>
        /// Convert an Axis value to a Vector3 value.
        /// </summary>
        public static Vector3 ToVector(Axis axis)
        {
            switch (axis)
            {
                case Axis.PositiveX: case Axis.X: return new Vector3(1f, 0f, 0f);
                case Axis.NegativeX: return new Vector3(-1f, 0f, 0f);
                case Axis.PositiveY: case Axis.Y: return new Vector3(0f, 1f, 0f);
                case Axis.NegativeY: return new Vector3(0f, -1f, 0f);
                case Axis.PositiveZ: case Axis.Z: return new Vector3(0f, 0f, 1f);
                case Axis.NegativeZ: return new Vector3(0f, 0f, -1f);
                default: return new Vector3();
            }
        }


        /// <summary>
        /// Convert an local Axis value to a global Vector3 value.
        /// </summary>
        public static Vector3 ToVector(Axis localAxis, Transform transform)
        {
            switch (localAxis)
            {
                case Axis.PositiveX: case Axis.X: return transform.right;
                case Axis.NegativeX: return -transform.right;
                case Axis.PositiveY: case Axis.Y: return transform.up;
                case Axis.NegativeY: return -transform.up;
                case Axis.PositiveZ: case Axis.Z: return transform.forward;
                case Axis.NegativeZ: return -transform.forward;
                default: return new Vector3();
            }
        }


        /// <summary>
        /// Convert a Vector3 value to an Axis value.
        /// </summary>
        public static Axis FromVector(Vector3 vector)
        {
            if (vector == Vector3.zero) return Axis.None;

            Vector3 abs = new Vector3(Mathf.Abs(vector.x), Mathf.Abs(vector.y), Mathf.Abs(vector.z));

            if (abs.x > abs.y)
            {
                if (abs.x > abs.z)
                {
                    return vector.x > 0f ? Axis.PositiveX : Axis.NegativeX;
                }
                else
                {
                    return vector.z > 0f ? Axis.PositiveZ : Axis.NegativeZ;
                }
            }
            else
            {
                if (abs.y > abs.z)
                {
                    return vector.y > 0f ? Axis.PositiveY : Axis.NegativeY;
                }
                else
                {
                    return vector.z > 0f ? Axis.PositiveZ : Axis.NegativeZ;
                }
            }
        }


        /// <summary>
        /// Get the reversed axis.
        /// </summary>
        public static Axis Reverse(Axis axis)
        {
            switch (axis)
            {
                case Axis.PositiveX: return Axis.NegativeX;
                case Axis.NegativeX: return Axis.PositiveX;
                case Axis.PositiveY: return Axis.NegativeY;
                case Axis.NegativeY: return Axis.PositiveY;
                case Axis.PositiveZ: return Axis.NegativeZ;
                case Axis.NegativeZ: return Axis.PositiveZ;
                default: return axis;
            }
        }


        /// <summary>
        /// Get the relation between two axes.
        /// </summary>
        public static AxisRelation RelationBetween(Axis a, Axis b)
        {
            if (a == b)
            {
                return AxisRelation.Same;
            }

            if (((int)a << 3) == (int)b || ((int)b << 3) == (int)a)
            {
                return AxisRelation.Opposite;
            }

            return AxisRelation.Vertical;
        }


        /// <summary>
        /// Is the axis a positive direction?
        /// </summary>
        public static bool IsPositive(Axis axis)
        {
            return (int)axis <= 4;
        }

    } // struct AxisUtilities

} // UnityExtensions