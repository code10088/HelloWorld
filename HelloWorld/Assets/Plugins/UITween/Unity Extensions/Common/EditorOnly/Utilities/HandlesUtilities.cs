#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;
using UnityEditor.IMGUI.Controls;

namespace UnityExtensions.Editor
{
    /// <summary>
    /// Handles Utilities.
    /// </summary>
    public struct HandlesUtilities
    {
        static Vector3[] _segmentVertices = new Vector3[2];
        static Vector3[] _boundsVertices = new Vector3[10];


        static BoxBoundsHandle _boxBoundsHandle;
        static BoxBoundsHandle boxBoundsHandle
        {
            get
            {
                if (_boxBoundsHandle == null) _boxBoundsHandle = new BoxBoundsHandle();
                return _boxBoundsHandle;
            }
        }


        static SphereBoundsHandle _sphereBoundsHandle;
        static SphereBoundsHandle sphereBoundsHandle
        {
            get
            {
                if (_sphereBoundsHandle == null) _sphereBoundsHandle = new SphereBoundsHandle();
                return _sphereBoundsHandle;
            }
        }


        static CapsuleBoundsHandle _capsuleBoundsHandle;
        static CapsuleBoundsHandle capsuleBoundsHandle
        {
            get
            {
                if (_capsuleBoundsHandle == null) _capsuleBoundsHandle = new CapsuleBoundsHandle();
                return _capsuleBoundsHandle;
            }
        }


        public static void DrawAALine(Vector3 point1, Vector3 point2)
        {
            _segmentVertices[0] = point1;
            _segmentVertices[1] = point2;
            Handles.DrawAAPolyLine(_segmentVertices);
        }


        public static void DrawAALine(Vector3 point1, Vector3 point2, float width)
        {
            _segmentVertices[0] = point1;
            _segmentVertices[1] = point2;
            Handles.DrawAAPolyLine(width, _segmentVertices);
        }


        public static void DrawWireBounds(Bounds bounds)
        {
            Vector3 min = bounds.min;
            Vector3 max = bounds.max;

            _boundsVertices[0] = min;
            _boundsVertices[1] = new Vector3(min.x, min.y, max.z);
            _boundsVertices[2] = new Vector3(max.x, min.y, max.z);
            _boundsVertices[3] = new Vector3(max.x, min.y, min.z);
            _boundsVertices[4] = min;

            _boundsVertices[5] = new Vector3(min.x, max.y, min.z);
            _boundsVertices[6] = new Vector3(min.x, max.y, max.z);
            _boundsVertices[7] = max;
            _boundsVertices[8] = new Vector3(max.x, max.y, min.z);
            _boundsVertices[9] = new Vector3(min.x, max.y, min.z);

            Handles.DrawAAPolyLine(_boundsVertices);

            DrawAALine(_boundsVertices[1], _boundsVertices[6]);
            DrawAALine(_boundsVertices[2], _boundsVertices[7]);
            DrawAALine(_boundsVertices[3], _boundsVertices[8]);
        }


        public static void DrawSphereOutline(Vector3 center, float radius)
        {
            var cameraTrans = Camera.current.transform;
            var cam_center = cameraTrans.position - center;
            float v2 = cam_center.sqrMagnitude;

            if (v2 > Camera.current.nearClipPlane * Camera.current.nearClipPlane)
            {
                float r2 = radius * radius;
                float r2_d_v2 = r2 / v2;

                Handles.CircleHandleCap(0, r2_d_v2 * cam_center + center, Quaternion.LookRotation(cam_center), radius * Mathf.Sqrt(1f - r2_d_v2), EventType.Repaint);
            }
        }


        public static void BoxHandle(ref Vector3 center, ref Vector3 size, Color color)
        {
            boxBoundsHandle.center = center;
            boxBoundsHandle.size = size;
            boxBoundsHandle.SetColor(color);
            boxBoundsHandle.DrawHandle();
            center = boxBoundsHandle.center;
            size = boxBoundsHandle.size;
        }


        public static Bounds BoxHandle(Bounds bounds, Color color)
        {
            boxBoundsHandle.center = bounds.center;
            boxBoundsHandle.size = bounds.size;
            boxBoundsHandle.SetColor(color);
            boxBoundsHandle.DrawHandle();
            bounds.center = boxBoundsHandle.center;
            bounds.size = boxBoundsHandle.size;
            return bounds;
        }


        public static Bounds BoxHandle(Transform transform, Bounds bounds, Color color)
        {
            using (HandlesMatrixScope.New(transform.localToWorldMatrix))
            {
                return BoxHandle(bounds, color);
            }
        }


        public static void BoxHandle(SerializedProperty boundsProperty, Color color)
        {
            boundsProperty.boundsValue = BoxHandle(boundsProperty.boundsValue, color);
        }


        public static void BoxHandle(Transform transform, SerializedProperty boundsProperty, Color color)
        {
            using (HandlesMatrixScope.New(transform.localToWorldMatrix))
            {
                boundsProperty.boundsValue = BoxHandle(boundsProperty.boundsValue, color);
            }
        }


        public static void SphereHandle(ref Vector3 center, ref float radius, Color color)
        {
            sphereBoundsHandle.center = center;
            sphereBoundsHandle.radius = radius;
            sphereBoundsHandle.SetColor(color);
            sphereBoundsHandle.DrawHandle();
            center = sphereBoundsHandle.center;
            radius = sphereBoundsHandle.radius;
        }


        public static void SphereHandle(Transform transform, ref Vector3 center, ref float radius, Color color)
        {
            using (HandlesMatrixScope.New(transform.localToWorldMatrix))
            {
                SphereHandle(ref center, ref radius, color);
            }
        }


        public static void SphereHandle(SerializedProperty center, SerializedProperty radius, Color color)
        {
            sphereBoundsHandle.center = center.vector3Value;
            sphereBoundsHandle.radius = radius.floatValue;
            sphereBoundsHandle.SetColor(color);
            sphereBoundsHandle.DrawHandle();
            center.vector3Value = sphereBoundsHandle.center;
            radius.floatValue = sphereBoundsHandle.radius;
        }


        public static void SphereHandle(Transform transform, SerializedProperty center, SerializedProperty radius, Color color)
        {
            using (HandlesMatrixScope.New(transform.localToWorldMatrix))
            {
                SphereHandle(center, radius, color);
            }
        }


        public static void CapsuleHandle(ref Vector3 center, ref float radius, ref float height, Color color, CapsuleBoundsHandle.HeightAxis axis)
        {
            capsuleBoundsHandle.center = center;
            capsuleBoundsHandle.radius = radius;
            capsuleBoundsHandle.height = height;
            capsuleBoundsHandle.heightAxis = axis;
            capsuleBoundsHandle.SetColor(color);
            capsuleBoundsHandle.DrawHandle();
            center = capsuleBoundsHandle.center;
            radius = capsuleBoundsHandle.radius;
            height = capsuleBoundsHandle.height;
        }


        public static void CapsuleHandle(Transform transform, ref Vector3 center, ref float radius, ref float height, Color color, CapsuleBoundsHandle.HeightAxis axis)
        {
            using (HandlesMatrixScope.New(transform.localToWorldMatrix))
            {
                CapsuleHandle(ref center, ref radius, ref height, color, axis);
            }
        }

    } // struct HandlesUtilities

} // namespace UnityExtensions.Editor

#endif // UNITY_EDITOR