using System;
using UnityEngine;

namespace UnityExtensions
{
    [Serializable]
    public struct Range3
    {
        public Range x;
        public Range y;
        public Range z;

        public Range3(Range x, Range y, Range z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }

        public Range3(Vector3 min, Vector3 max)
        {
            x.min = min.x;
            x.max = max.x;
            y.min = min.y;
            y.max = max.y;
            z.min = min.z;
            z.max = max.z;
        }

        public Vector3 min
        {
            get { return new Vector3(x.min, y.min, z.min); }
            set
            {
                x.min = value.x;
                y.min = value.y;
                z.min = value.z;
            }
        }

        public Vector3 max
        {
            get { return new Vector3(x.max, y.max, z.max); }
            set
            {
                x.max = value.x;
                y.max = value.y;
                z.max = value.z;
            }
        }

        public Vector3 size
        {
            get { return new Vector3(x.size, y.size, z.size); }
            set
            {
                x.size = value.x;
                y.size = value.y;
                z.size = value.z;
            }
        }

        public Vector3 center
        {
            get { return new Vector3(x.center, y.center, z.center); }
            set
            {
                x.center = value.x;
                y.center = value.y;
                z.center = value.z;
            }
        }

        public void SortMinMax()
        {
            x.SortMinMax();
            y.SortMinMax();
            z.SortMinMax();
        }

        public bool Contains(Vector3 point)
        {
            return x.Contains(point.x) && y.Contains(point.y) && z.Contains(point.z);
        }

        public Vector3 Closest(Vector3 point)
        {
            point.x = x.Closest(point.x);
            point.y = y.Closest(point.y);
            point.z = z.Closest(point.z);
            return point;
        }

        public bool Intersects(Range3 other)
        {
            return x.Intersects(other.x) && y.Intersects(other.y) && z.Intersects(other.z);
        }

        public Range3 GetIntersection(Range3 other)
        {
            other.x = x.GetIntersection(other.x);
            other.y = y.GetIntersection(other.y);
            other.z = z.GetIntersection(other.z);
            return other;
        }

        public Vector3 SignedDistance3(Vector3 point)
        {
            point.x = x.SignedDistance(point.x);
            point.y = y.SignedDistance(point.y);
            point.z = z.SignedDistance(point.z);
            return point;
        }

        public Vector3 Distance3(Vector3 point)
        {
            point.x = x.Distance(point.x);
            point.y = y.Distance(point.y);
            point.z = z.Distance(point.z);
            return point;
        }

        public float SqrDistance(Vector3 point)
        {
            return Distance3(point).sqrMagnitude;
        }

        public float Distance(Vector3 point)
        {
            return Distance3(point).magnitude;
        }

        public void Encapsulate(Vector3 point)
        {
            x.Encapsulate(point.x);
            y.Encapsulate(point.y);
            z.Encapsulate(point.z);
        }

        public void Encapsulate(Range3 other)
        {
            x.Encapsulate(other.x);
            y.Encapsulate(other.y);
            z.Encapsulate(other.z);
        }

        public void Expand(Vector3 delta)
        {
            x.Expand(delta.x);
            y.Expand(delta.y);
            z.Expand(delta.z);
        }

        public void Expand(float delta)
        {
            x.Expand(delta);
            y.Expand(delta);
            z.Expand(delta);
        }

        public void Move(Vector3 delta)
        {
            x.Move(delta.x);
            y.Move(delta.y);
            z.Move(delta.z);
        }

    } // struct Range3

} // namespace UnityExtensions