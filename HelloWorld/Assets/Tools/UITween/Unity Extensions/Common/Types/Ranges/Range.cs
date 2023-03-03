using System;

namespace UnityExtensions
{
    [Serializable]
    public struct Range
    {
        public float min;
        public float max;


        public Range(float min, float max)
        {
            this.min = min;
            this.max = max;
        }

        public float size
        {
            get { return max - min; }
            set
            {
                float center2 = min + max;
                min = (center2 - value) * 0.5f;
                max = (center2 + value) * 0.5f;
            }
        }

        public float center
        {
            get { return (min + max) * 0.5f; }
            set
            {
                float half = (max - min) * 0.5f;
                min = value - half;
                max = value + half;
            }
        }

        public void SortMinMax()
        {
            if (min > max)
            {
                float tmp = max;
                max = min;
                min = tmp;
            }
        }

        public bool Contains(float value)
        {
            return value >= min && value <= max;
        }

        public float Closest(float value)
        {
            if (value <= min) return min;
            if (value >= max) return max;
            return value;
        }

        public bool Intersects(Range other)
        {
            return min <= other.max && max >= other.min;
        }

        public Range GetIntersection(Range other)
        {
            if (min > other.min) other.min = min;
            if (max < other.max) other.max = max;
            return other;
        }

        public float SignedDistance(float value)
        {
            if (value < min) return value - min;
            if (value > max) return value - max;
            return 0f;
        }

        public float Distance(float value)
        {
            if (value < min) return min - value;
            if (value > max) return value - max;
            return 0f;
        }

        public void Encapsulate(float value)
        {
            if (value < min) min = value;
            else if (value > max) max = value;
        }

        public void Encapsulate(Range other)
        {
            if (other.min < min) min = other.min;
            if (other.max > max) max = other.max;
        }

        public void Expand(float delta)
        {
            min -= delta;
            max += delta;
        }

        public void Move(float delta)
        {
            min += delta;
            max += delta;
        }

    } // struct Range

} // namespace UnityExtensions