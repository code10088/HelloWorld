using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UnityExtensions.Tween
{
    [System.Serializable]
    public abstract class TweenVector3<TTarget> : TweenFromTo<Vector3, TTarget> where TTarget : Object
    {
        public bool3 toggle;

        public override void Interpolate(float factor)
        {
            if (toggle.anyTrue)
            {
                var t = toggle.allTrue ? default : current;

                if (toggle.x) t.x = (to.x - from.x) * factor + from.x;
                if (toggle.y) t.y = (to.y - from.y) * factor + from.y;
                if (toggle.z) t.z = (to.z - from.z) * factor + from.z;

                current = t;
            }
        }

#if UNITY_EDITOR

        public override void Reset(TweenPlayer player)
        {
            base.Reset(player);
            toggle = default;
        }


        protected override void OnPropertiesGUI(TweenPlayer player, SerializedProperty property)
        {
            base.OnPropertiesGUI(player, property);

            var (fromProp, toProp) = GetFromToProperties(property);
            var toggleProp = property.FindPropertyRelative(nameof(toggle));

            FromToFieldLayout("X",
                fromProp.FindPropertyRelative(nameof(Vector3.x)),
                toProp.FindPropertyRelative(nameof(Vector3.x)),
                toggleProp.FindPropertyRelative(nameof(bool3.x)));
            FromToFieldLayout("Y",
                fromProp.FindPropertyRelative(nameof(Vector3.y)),
                toProp.FindPropertyRelative(nameof(Vector3.y)),
                toggleProp.FindPropertyRelative(nameof(bool3.y)));
            FromToFieldLayout("Z",
                fromProp.FindPropertyRelative(nameof(Vector3.z)),
                toProp.FindPropertyRelative(nameof(Vector3.z)),
                toggleProp.FindPropertyRelative(nameof(bool3.z)));
        }

#endif // UNITY_EDITOR

    } // class TweenVector3<TTarget>

} // namespace UnityExtensions.Tween