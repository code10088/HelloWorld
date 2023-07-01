using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UnityExtensions.Tween
{
    [System.Serializable]
    public abstract class TweenVector2<TTarget> : TweenFromTo<Vector2, TTarget> where TTarget : Object
    {
        public bool2 toggle;

        public override void Interpolate(float factor)
        {
            if (toggle.anyTrue)
            {
                var t = toggle.allTrue ? default : current;

                if (toggle.x) t.x = (to.x - from.x) * factor + from.x;
                if (toggle.y) t.y = (to.y - from.y) * factor + from.y;

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
                fromProp.FindPropertyRelative(nameof(Vector2.x)),
                toProp.FindPropertyRelative(nameof(Vector2.x)),
                toggleProp.FindPropertyRelative(nameof(bool2.x)));
            FromToFieldLayout("Y",
                fromProp.FindPropertyRelative(nameof(Vector2.y)),
                toProp.FindPropertyRelative(nameof(Vector2.y)),
                toggleProp.FindPropertyRelative(nameof(bool2.y)));
        }

#endif // UNITY_EDITOR

    } // class TweenVector2<TTarget>

} // namespace UnityExtensions.Tween