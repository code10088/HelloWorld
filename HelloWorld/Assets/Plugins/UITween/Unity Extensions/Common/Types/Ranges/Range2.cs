using System;
using UnityEngine;

namespace UnityExtensions
{
    [Serializable]
    public struct Range2
    {
        public Range x;
        public Range y;

        public Range2(Range x, Range y)
        {
            this.x = x;
            this.y = y;
        }
        
        public Range2(Vector2 min, Vector2 max)
        {
            x.min = min.x;
            x.max = max.x;
            y.min = min.y;
            y.max = max.y;
        }

        public Vector2 min
        {
            get { return new Vector2(x.min, y.min); }
            set
            {
                x.min = value.x;
                y.min = value.y;
            }
        }

        public Vector2 max
        {
            get { return new Vector2(x.max, y.max); }
            set
            {
                x.max = value.x;
                y.max = value.y;
            }
        }

        public Vector2 size
        {
            get { return new Vector2(x.size, y.size); }
            set
            {
                x.size = value.x;
                y.size = value.y;
            }
        }

        public Vector2 center
        {
            get { return new Vector2(x.center, y.center); }
            set
            {
                x.center = value.x;
                y.center = value.y;
            }
        }

        public void SortMinMax()
        {
            x.SortMinMax();
            y.SortMinMax();
        }

        public bool Contains(Vector2 point)
        {
            return x.Contains(point.x) && y.Contains(point.y);
        }

        public Vector2 Closest(Vector2 point)
        {
            point.x = x.Closest(point.x);
            point.y = y.Closest(point.y);
            return point;
        }

        public bool Intersects(Range2 other)
        {
            return x.Intersects(other.x) && y.Intersects(other.y);
        }

        public Range2 GetIntersection(Range2 other)
        {
            other.x = x.GetIntersection(other.x);
            other.y = y.GetIntersection(other.y);
            return other;
        }

        public Vector2 SignedDistance2(Vector2 point)
        {
            point.x = x.SignedDistance(point.x);
            point.y = y.SignedDistance(point.y);
            return point;
        }

        public Vector2 Distance2(Vector2 point)
        {
            point.x = x.Distance(point.x);
            point.y = y.Distance(point.y);
            return point;
        }

        public float SqrDistance(Vector2 point)
        {
            return Distance2(point).sqrMagnitude;
        }

        public float Distance(Vector2 point)
        {
            return Distance2(point).magnitude;
        }

        public void Encapsulate(Vector2 point)
        {
            x.Encapsulate(point.x);
            y.Encapsulate(point.y);
        }

        public void Encapsulate(Range2 other)
        {
            x.Encapsulate(other.x);
            y.Encapsulate(other.y);
        }

        public void Expand(Vector2 delta)
        {
            x.Expand(delta.x);
            y.Expand(delta.y);
        }

        public void Expand(float delta)
        {
            x.Expand(delta);
            y.Expand(delta);
        }

        public void Move(Vector2 delta)
        {
            x.Move(delta.x);
            y.Move(delta.y);
        }

    } // struct Range2

} // namespace UnityExtensions