using UnityEngine;
using System;

#if UNITY_EDITOR
using UnityEditor;
using UnityExtensions.Editor;
#endif

namespace UnityExtensions.Tween
{
    interface ITweenFromTo
    {
        void SwapFromWithTo();
    }

    interface ITweenUnmanaged
    {
        void LetFromEqualCurrent();
        void LetToEqualCurrent();
    }

    interface ITweenFromToWithTarget
    {
        UnityEngine.Object target { get; }
        void LetCurrentEqualFrom();
        void LetCurrentEqualTo();
    }

    [Serializable]
    public abstract class TweenFromTo<T> : TweenAnimation, ITweenFromTo
    {
        public T from;
        public T to;

        public void SwapFromWithTo()
        {
            RuntimeUtilities.Swap(ref from, ref to);
        }

#if UNITY_EDITOR

        public override void Reset(TweenPlayer player)
        {
            base.Reset(player);
            from = default;
            to = default;
        }

        protected override void CreateOptionsMenu(GenericMenu menu, TweenPlayer player, int index)
        {
            base.CreateOptionsMenu(menu, player, index);

            menu.AddSeparator(string.Empty);

            menu.AddItem(new GUIContent("Swap 'From' with 'To'"), false, () =>
            {
                Undo.RecordObject(player, "Swap 'From' with 'To'");
                SwapFromWithTo();
            });
        }

        protected (SerializedProperty, SerializedProperty) GetFromToProperties(SerializedProperty property)
        {
            return
            (
                property.FindPropertyRelative(nameof(from)),
                property.FindPropertyRelative(nameof(to))
            );
        }

#endif // UNITY_EDITOR

    }// class TweenFromTo<T>


    public abstract class TweenUnmanaged<T> : TweenFromTo<T>, ITweenUnmanaged where T : unmanaged
    {
        /// <summary>
        /// 当前状态
        /// </summary>
        public abstract T current { get; set; }

        public void LetFromEqualCurrent()
        {
            from = current;
        }

        public void LetToEqualCurrent()
        {
            to = current;
        }

#if UNITY_EDITOR

        T _temp;

        public override void Reset(TweenPlayer player)
        {
            base.Reset(player);
            from = to = current;
        }

        public override void RecordState()
        {
            _temp = current;
        }

        public override void RestoreState()
        {
            current = _temp;
        }

        protected override void CreateOptionsMenu(GenericMenu menu, TweenPlayer player, int index)
        {
            base.CreateOptionsMenu(menu, player, index);

            menu.AddItem(new GUIContent("Let 'From' Equal 'Current'"), false, () =>
            {
                Undo.RecordObject(player, "Let 'From' Equal 'Current'");
                LetFromEqualCurrent();
            });

            menu.AddItem(new GUIContent("Let 'To' Equal 'Current'"), false, () =>
            {
                Undo.RecordObject(player, "Let 'To' Equal 'Current'");
                LetToEqualCurrent();
            });
        }

#endif // UNITY_EDITOR

    } // class TweenUnmanaged<T>


    [Serializable]
    public abstract class TweenFromTo<TValue, TTarget> : TweenUnmanaged<TValue>, ITweenFromToWithTarget
        where TValue : unmanaged
        where TTarget : UnityEngine.Object
    {
        public TTarget target;

        UnityEngine.Object ITweenFromToWithTarget.target => target;

        public void LetCurrentEqualFrom()
        {
            Interpolate(0f);    // supports toggles
        }

        public void LetCurrentEqualTo()
        {
            Interpolate(1f);    // supports toggles
        }

#if UNITY_EDITOR

        TTarget _originalTarget;

        public override void Reset(TweenPlayer player)
        {
            player.TryGetComponent(out target);
            base.Reset(player);
        }

        public override void RecordState()
        {
            _originalTarget = target;
            base.RecordState();
        }

        public override void RestoreState()
        {
            var currentTarget = target;
            target = _originalTarget;
            base.RestoreState();
            target = currentTarget;
        }

        protected override void CreateOptionsMenu(GenericMenu menu, TweenPlayer player, int index)
        {
            base.CreateOptionsMenu(menu, player, index);

            menu.AddItem(new GUIContent("Let 'Current' Equal 'From'"), () =>
            {
                Undo.RecordObject(target, "Let 'Current' Equal 'From'");
                LetCurrentEqualFrom();
            }, !target);

            menu.AddItem(new GUIContent("Let 'Current' Equal 'To'"), () =>
            {
                Undo.RecordObject(target, "Let 'Current' Equal 'To'");
                LetCurrentEqualTo();
            }, !target);
        }

        protected override void OnPropertiesGUI(TweenPlayer player, SerializedProperty property)
        {
            using (DisabledScope.New(player.playing))
            {
                EditorGUILayout.PropertyField(property.FindPropertyRelative(nameof(target)));
            }
        }

#endif // UNITY_EDITOR

    } // class TweenFromTo<TValue, TTarget>

} // namespace UnityExtensions.Tween