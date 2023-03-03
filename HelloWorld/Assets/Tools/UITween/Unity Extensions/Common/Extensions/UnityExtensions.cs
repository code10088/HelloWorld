using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace UnityExtensions
{
    /// <summary>
    /// Extensions for Unity.
    /// </summary>
    public static partial class Extensions
    {
#if !UNITY_2019_2_OR_NERER
        public static bool TryGetComponent<T>(this GameObject gameObject, out T component)
        {
            component = gameObject.GetComponent<T>();
            return component != null;
        }
#endif

        /// <summary>
        /// Get component if it exists or add a new one.
        /// </summary>
        public static T GetComponentSafely<T>(this GameObject target) where T : Component
        {
            if (!target.TryGetComponent(out T component))
            {
                component = target.AddComponent<T>();
            }
            return component;
        }


        /// <summary>
        /// Get component if it exists or add a new one.
        /// </summary>
        public static T GetComponentSafely<T>(this Component target) where T : Component
        {
            return target.gameObject.GetComponentSafely<T>();
        }


        /// <summary>
        /// Get the RectTransform component.
        /// </summary>
        public static RectTransform rectTransform(this Component target)
        {
            return target.transform as RectTransform;
        }


        /// <summary>
        /// Get the RectTransform component.
        /// </summary>
        public static RectTransform rectTransform(this GameObject target)
        {
            return target.transform as RectTransform;
        }


        /// <summary>
        /// Delay invoking (scaled time).
        /// </summary>
        public static void Delay(this MonoBehaviour behaviour, float delay, Action action)
        {
            behaviour.StartCoroutine(DelayedCoroutine());

            IEnumerator DelayedCoroutine()
            {
                yield return new WaitForSeconds(delay);
                action();
            }
        }


        /// <summary>
        /// Delay invoking (unscaled time).
        /// </summary>
        public static void DelayRealtime(this MonoBehaviour behaviour, float delay, Action action)
        {
            behaviour.StartCoroutine(DelayedCoroutine());

            IEnumerator DelayedCoroutine()
            {
                yield return new WaitForSecondsRealtime(delay);
                action();
            }
        }


        /// <summary>
        /// Reset localPosition, localRotation and localScale of transform.
        /// </summary>
        public static void ResetLocal(this Transform transform)
        {
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
        }


        /// <summary>
        /// Set localPosition.x
        /// </summary>
        public static void SetLocalPositionX(this Transform transform, float x)
        {
            var pos = transform.localPosition;
            pos.x = x;
            transform.localPosition = pos;
        }


        /// <summary>
        /// Set localPosition.y
        /// </summary>
        public static void SetLocalPositionY(this Transform transform, float y)
        {
            var pos = transform.localPosition;
            pos.y = y;
            transform.localPosition = pos;
        }


        /// <summary>
        /// Set localPosition.z
        /// </summary>
        public static void SetLocalPositionZ(this Transform transform, float z)
        {
            var pos = transform.localPosition;
            pos.z = z;
            transform.localPosition = pos;
        }


        /// <summary>
        /// Set anchoredPosition.x
        /// </summary>
        public static void SetAnchoredPositionX(this RectTransform rectTransform, float x)
        {
            var pos = rectTransform.anchoredPosition;
            pos.x = x;
            rectTransform.anchoredPosition = pos;
        }


        /// <summary>
        /// Set anchoredPosition.y
        /// </summary>
        public static void SetAnchoredPositionY(this RectTransform rectTransform, float y)
        {
            var pos = rectTransform.anchoredPosition;
            pos.y = y;
            rectTransform.anchoredPosition = pos;
        }


        /// <summary>
        /// Set sizeDelta.x
        /// </summary>
        public static void SetSizeDeltaX(this RectTransform rectTransform, float x)
        {
            var size = rectTransform.sizeDelta;
            size.x = x;
            rectTransform.sizeDelta = size;
        }


        /// <summary>
        /// Set sizeDelta.y
        /// </summary>
        public static void SetSizeDeltaY(this RectTransform rectTransform, float y)
        {
            var size = rectTransform.sizeDelta;
            size.y = y;
            rectTransform.sizeDelta = size;
        }


        /// <summary>
        /// Set anchorMin.x
        /// </summary>
        public static void SetAnchorMinX(this RectTransform rectTransform, float x)
        {
            var anchorMin = rectTransform.anchorMin;
            anchorMin.x = x;
            rectTransform.anchorMin = anchorMin;
        }


        /// <summary>
        /// Set anchorMin.y
        /// </summary>
        public static void SetAnchorMinY(this RectTransform rectTransform, float y)
        {
            var anchorMin = rectTransform.anchorMin;
            anchorMin.y = y;
            rectTransform.anchorMin = anchorMin;
        }


        /// <summary>
        /// Set anchorMax.x
        /// </summary>
        public static void SetAnchorMaxX(this RectTransform rectTransform, float x)
        {
            var anchorMax = rectTransform.anchorMax;
            anchorMax.x = x;
            rectTransform.anchorMax = anchorMax;
        }


        /// <summary>
        /// Set anchorMax.y
        /// </summary>
        public static void SetAnchorMaxY(this RectTransform rectTransform, float y)
        {
            var anchorMax = rectTransform.anchorMax;
            anchorMax.y = y;
            rectTransform.anchorMax = anchorMax;
        }


        /// <summary>
        /// Set pivot.x
        /// </summary>
        public static void SetPivotX(this RectTransform rectTransform, float x)
        {
            var pivot = rectTransform.pivot;
            pivot.x = x;
            rectTransform.pivot = pivot;
        }


        /// <summary>
        /// Set pivot.y
        /// </summary>
        public static void SetPivotY(this RectTransform rectTransform, float y)
        {
            var pivot = rectTransform.pivot;
            pivot.y = y;
            rectTransform.pivot = pivot;
        }


        /// <summary>
        /// Traverse transform tree (root node first).
        /// </summary>
        /// <param name="root"> The root node of transform tree. </param>
        /// <param name="operate"> A custom operation on every transform node. </param>
        /// <param name="depthLimit"> Negative value means no limit, zero means root only, positive value means maximum children depth </param>
        public static void TraverseHierarchy(this Transform root, Action<Transform> operate, int depthLimit = -1)
        {
            operate(root);

            if (depthLimit != 0)
            {
                int count = root.childCount;
                for (int i = 0; i < count; i++)
                {
                    TraverseHierarchy(root.GetChild(i), operate, depthLimit - 1);
                }
            }
        }


        /// <summary>
        /// Traverse transform tree (leaf node first).
        /// </summary>
        /// <param name="root"> The root node of transform tree. </param>
        /// <param name="operate"> A custom operation on every transform node. </param>
        /// <param name="depthLimit"> Negative value means no limit, zero means root only, positive value means maximum children depth </param>
        public static void InverseTraverseHierarchy(this Transform root, Action<Transform> operate, int depthLimit = -1)
        {
            if (depthLimit != 0)
            {
                int count = root.childCount;
                for (int i = 0; i < count; i++)
                {
                    InverseTraverseHierarchy(root.GetChild(i), operate, depthLimit - 1);
                }
            }

            operate(root);
        }


        /// <summary>
        /// Find a transform in the transform tree (root node first)
        /// </summary>
        /// <param name="root"> The root node of transform tree. </param>
        /// <param name="match"> match function. </param>
        /// <param name="depthLimit"> Negative value means no limit, zero means root only, positive value means maximum children depth </param>
        /// <returns> The matched node or null if no matched. </returns>
        public static Transform SearchHierarchy(this Transform root, Predicate<Transform> match, int depthLimit = -1)
        {
            if (match(root)) return root;
            if (depthLimit == 0) return null;

            int count = root.childCount;
            Transform result = null;

            for (int i = 0; i < count; i++)
            {
                result = SearchHierarchy(root.GetChild(i), match, depthLimit - 1);
                if (result) break;
            }

            return result;
        }


        public static Rect GetWorldRect(this RectTransform rectTransform)
        {
            var rect = rectTransform.rect;
            rect.min = rectTransform.TransformPoint(rect.min);
            rect.max = rectTransform.TransformPoint(rect.max);
            return rect;
        }


        public static void Encapsulate(this ref Rect rect, Vector2 point)
        {
            if (rect.xMin > point.x) rect.xMin = point.x;
            if (rect.xMax < point.x) rect.xMax = point.x;
            if (rect.yMin > point.y) rect.yMin = point.y;
            if (rect.yMax < point.y) rect.yMax = point.y;
        }


        /// <summary>
        /// Get the overlapped rect.
        /// </summary>
        public static Rect GetIntersection(this Rect rect, Rect other)
        {
            if (rect.xMin > other.xMin) other.xMin = rect.xMin;
            if (rect.xMax < other.xMax) other.xMax = rect.xMax;
            if (rect.yMin > other.yMin) other.yMin = rect.yMin;
            if (rect.yMax < other.yMax) other.yMax = rect.yMax;
            return other;
        }


        public static Vector2 xy(this Vector3 v)
        {
            return new Vector2(v.x, v.y);
        }


        public static Vector2 yz(this Vector3 v)
        {
            return new Vector2(v.y, v.z);
        }


        public static Vector2 xz(this Vector3 v)
        {
            return new Vector2(v.x, v.z);
        }


        /// <summary>
        /// Clone the AnimationCurve instance.
        /// </summary>
        public static AnimationCurve Clone(this AnimationCurve target)
        {
            var newCurve = new AnimationCurve(target.keys);
            newCurve.postWrapMode = target.postWrapMode;
            newCurve.preWrapMode = target.preWrapMode;

            return newCurve;
        }


        /// <summary>
        /// Clone the Gradient instance.
        /// </summary>
        public static Gradient Clone(this Gradient target)
        {
            var newGradient = new Gradient();
            newGradient.alphaKeys = target.alphaKeys;
            newGradient.colorKeys = target.colorKeys;
            newGradient.mode = target.mode;

            return newGradient;
        }


        public static PlatformMask ToFlag(this RuntimePlatform platform)
        {
            switch (platform)
            {
                case RuntimePlatform.WindowsEditor: return PlatformMask.WindowsEditor;
                case RuntimePlatform.OSXEditor: return PlatformMask.OSXEditor;
                case RuntimePlatform.LinuxEditor: return PlatformMask.LinuxEditor;

                case RuntimePlatform.WindowsPlayer: return PlatformMask.WindowsPlayer;
                case RuntimePlatform.OSXPlayer: return PlatformMask.OSXPlayer;
                case RuntimePlatform.LinuxPlayer: return PlatformMask.LinuxPlayer;

                case RuntimePlatform.PS4: return PlatformMask.PS4;
                case RuntimePlatform.XboxOne: return PlatformMask.XboxOne;
                case RuntimePlatform.Switch: return PlatformMask.Switch;

                case RuntimePlatform.IPhonePlayer: return PlatformMask.iPhone;
                case RuntimePlatform.Android: return PlatformMask.Android;

                case RuntimePlatform.WSAPlayerX86: return PlatformMask.WSAPlayerX86;
                case RuntimePlatform.WSAPlayerX64: return PlatformMask.WSAPlayerX64;
                case RuntimePlatform.WSAPlayerARM: return PlatformMask.WSAPlayerARM;

                case RuntimePlatform.WebGLPlayer: return PlatformMask.WebGLPlayer;

                case RuntimePlatform.tvOS: return PlatformMask.tvOS;
                //case RuntimePlatform.Stadia: return PlatformMask.Stadia;

                default: return PlatformMask.None;
            }
        }


        public static bool Contains(this PlatformMask mask, RuntimePlatform platform)
        {
            return (mask & platform.ToFlag()) != 0;
        }


        public static float ScreenToWorldSize(this Camera camera, float pixelSize, float clipPlane)
        {
            if (camera.orthographic)
            {
                return pixelSize * camera.orthographicSize * 2f / camera.pixelHeight;
            }
            else
            {
                return pixelSize * clipPlane * Mathf.Tan(camera.fieldOfView * 0.5f * Mathf.Deg2Rad) * 2f / camera.pixelHeight;
            }
        }


        public static float WorldToScreenSize(this Camera camera, float worldSize, float clipPlane)
        {
            if (camera.orthographic)
            {
                return worldSize * camera.pixelHeight * 0.5f / camera.orthographicSize;
            }
            else
            {
                return worldSize * camera.pixelHeight * 0.5f / (clipPlane * Mathf.Tan(camera.fieldOfView * 0.5f * Mathf.Deg2Rad));
            }
        }


        public static Vector4 GetClipPlane(this Camera camera, Vector3 point, Vector3 normal)
        {
            Matrix4x4 wtoc = camera.worldToCameraMatrix;
            point = wtoc.MultiplyPoint(point);
            normal = wtoc.MultiplyVector(normal).normalized;

            return new Vector4(normal.x, normal.y, normal.z, -Vector3.Dot(point, normal));
        }


        /// <summary>
        /// Calculate ZBufferParams, can used in compute shader 
        /// </summary>
        public static Vector4 GetZBufferParams(this Camera camera)
        {
            double f = camera.farClipPlane;
            double n = camera.nearClipPlane;

            double rn = 1f / n;
            double rf = 1f / f;
            double fpn = f / n;

            return SystemInfo.usesReversedZBuffer
                ? new Vector4((float)(fpn - 1.0), 1f, (float)(rn - rf), (float)rf)
                : new Vector4((float)(1.0 - fpn), (float)fpn, (float)(rf - rn), (float)rn);
        }


        public static void AddListener(this EventTrigger eventTrigger, EventTriggerType type, UnityAction<BaseEventData> callback)
        {
            var triggers = eventTrigger.triggers;
            var index = triggers.FindIndex(entry => entry.eventID == type);
            if (index < 0)
            {
                var entry = new EventTrigger.Entry();
                entry.eventID = type;
                entry.callback.AddListener(callback);
                triggers.Add(entry);
            }
            else
            {
                triggers[index].callback.AddListener(callback);
            }
        }


        public static void RemoveListener(this EventTrigger eventTrigger, EventTriggerType type, UnityAction<BaseEventData> callback)
        {
            var triggers = eventTrigger.triggers;
            var index = triggers.FindIndex(entry => entry.eventID == type);
            if (index >= 0)
            {
                triggers[index].callback.RemoveListener(callback);
            }
        }

    } // class Extensions

} // namespace UnityExtensions