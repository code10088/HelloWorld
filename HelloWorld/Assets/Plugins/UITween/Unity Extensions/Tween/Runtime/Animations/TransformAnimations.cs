using UnityEngine;
using System;

#if UNITY_EDITOR
using UnityEditor;
using UnityExtensions.Editor;
#endif

namespace UnityExtensions.Tween
{
    [Serializable, TweenAnimation("Transform/Position", "Transform Position")]
    public class TweenTransformPosition : TweenVector3<Transform>
    {
        public Space space = Space.Self;

        public override Vector3 current
        {
            get => target ? (space == Space.World ? target.position : target.localPosition) : default;
            set
            {
                if (target)
                {
                    if (space == Space.World) target.position = value;
                    else target.localPosition = value;
                }
            }
        }

#if UNITY_EDITOR
        public override void Reset(TweenPlayer player)
        {
            space = Space.Self;
            base.Reset(player);
        }

        protected override void OnPropertiesGUI(TweenPlayer player, SerializedProperty property)
        {
            EditorGUILayout.PropertyField(property.FindPropertyRelative(nameof(space)));
            base.OnPropertiesGUI(player, property);
        }
#endif
    } // class TweenTransformPosition


    [Serializable, TweenAnimation("Transform/Euler Angles", "Transform Euler Angles")]
    public class TweenTransformEulerAngles : TweenVector3<Transform>
    {
        public Space space = Space.Self;

        public override Vector3 current
        {
            get => target ? (space == Space.World ? target.eulerAngles : target.localEulerAngles) : default;
            set
            {
                if (target)
                {
                    if (space == Space.World) target.eulerAngles = value;
                    else target.localEulerAngles = value;
                }
            }
        }

#if UNITY_EDITOR
        public override void Reset(TweenPlayer player)
        {
            space = Space.Self;
            base.Reset(player);
        }

        protected override void OnPropertiesGUI(TweenPlayer player, SerializedProperty property)
        {
            EditorGUILayout.PropertyField(property.FindPropertyRelative(nameof(space)));
            base.OnPropertiesGUI(player, property);
        }
#endif
    } // class TweenTransformEulerAngles


    [Serializable, TweenAnimation("Transform/Rotation", "Transform Rotation")]
    public class TweenTransformRotation : TweenQuaternion<Transform>
    {
        public Space space = Space.Self;

        public override Quaternion current
        {
            get => target ? (space == Space.World ? target.rotation : target.localRotation) : Quaternion.identity;
            set
            {
                if (target)
                {
                    if (space == Space.World) target.rotation = value;
                    else target.localRotation = value;
                }
            }
        }

#if UNITY_EDITOR
        public override void Reset(TweenPlayer player)
        {
            space = Space.Self;
            base.Reset(player);
        }

        protected override void OnPropertiesGUI(TweenPlayer player, SerializedProperty property)
        {
            EditorGUILayout.PropertyField(property.FindPropertyRelative(nameof(space)));
            base.OnPropertiesGUI(player, property);
        }
#endif
    } // class TweenTransformRotation


    [Serializable, TweenAnimation("Transform/Scale", "Transform Scale")]
    public class TweenTransformScale : TweenVector3<Transform>
    {
        public override Vector3 current
        {
            get => target ? target.localScale : Vector3.one;
            set { if (target) target.localScale = value; }
        }

    } // class TweenTransformScale


    [Serializable, TweenAnimation("Transform/Transform", "Transform")]
    public class TweenTransform : TweenFromTo<Transform>
    {
        public bool togglePosition = default;
        public bool toggleRotation = default;
        public bool toggleLocalScale = default;
        public Transform target = default;

        public Vector3 currentPosition
        {
            get => target ? target.position : default;
            set { if (target) target.position = value; }
        }

        public Quaternion currentRotation
        {
            get => target ? target.rotation : Quaternion.identity;
            set { if (target) target.rotation = value; }
        }

        public Vector3 currentLocalScale
        {
            get => target ? target.localScale : Vector3.one;
            set { if (target) target.localScale = value; }
        }

        public override void Interpolate(float factor)
        {
            if (from && to && target)
            {
                if (togglePosition)
                {
                    currentPosition = Vector3.LerpUnclamped(from.position, to.position, factor);
                }
                if (toggleRotation)
                {
                    currentRotation = Quaternion.SlerpUnclamped(from.rotation, to.rotation, factor);
                }
                if (toggleLocalScale)
                {
                    currentLocalScale = Vector3.LerpUnclamped(from.localScale, to.localScale, factor);
                }
            }
        }

#if UNITY_EDITOR

        Transform _originalTarget;
        Vector3 _tempPosition;
        Quaternion _tempRotation;
        Vector3 _tempLocalScale;

        public override void Reset(TweenPlayer player)
        {
            base.Reset(player);
            togglePosition = default;
            toggleRotation = default;
            toggleLocalScale = default;
            target = player.transform;
        }

        public override void RecordState()
        {
            _originalTarget = target;
            _tempPosition = currentPosition;
            _tempRotation = currentRotation;
            _tempLocalScale = currentLocalScale;
        }

        public override void RestoreState()
        {
            var currentTarget = target;
            target = _originalTarget;
            currentPosition = _tempPosition;
            currentRotation = _tempRotation;
            currentLocalScale = _tempLocalScale;
            target = currentTarget;
        }

        protected override void OnPropertiesGUI(TweenPlayer player, SerializedProperty property)
        {
            using (DisabledScope.New(player.playing))
            {
                EditorGUILayout.PropertyField(property.FindPropertyRelative(nameof(target)));
            }

            var rect = EditorGUILayout.GetControlRect();
            float labelWidth = EditorGUIUtility.labelWidth;

            var fromRect = new Rect(rect.x + labelWidth, rect.y, (rect.width - labelWidth - 8) / 2, rect.height);
            var toRect = new Rect(rect.xMax - fromRect.width, fromRect.y, fromRect.width, fromRect.height);

            var togglePositionProp = property.FindPropertyRelative(nameof(togglePosition));
            var toggleRotationProp = property.FindPropertyRelative(nameof(toggleRotation));
            var toggleLocalScaleProp = property.FindPropertyRelative(nameof(toggleLocalScale));
            
            rect.width = rect.height + EditorStyles.label.CalcSize(EditorGUIUtilities.TempContent("P")).x;
            togglePositionProp.boolValue = EditorGUI.ToggleLeft(rect, "P", togglePositionProp.boolValue);

            rect.x = rect.xMax + rect.height * 0.5f;
            rect.width = rect.height + EditorStyles.label.CalcSize(EditorGUIUtilities.TempContent("R")).x;
            toggleRotationProp.boolValue = EditorGUI.ToggleLeft(rect, "R", toggleRotationProp.boolValue);

            rect.x = rect.xMax + rect.height * 0.5f;
            rect.width = rect.height + EditorStyles.label.CalcSize(EditorGUIUtilities.TempContent("S")).x;
            toggleLocalScaleProp.boolValue = EditorGUI.ToggleLeft(rect, "S", toggleLocalScaleProp.boolValue);

            using (DisabledScope.New(!togglePosition && !toggleRotation && !toggleLocalScale))
            {
                using (LabelWidthScope.New(12))
                {
                    var (fromProp, toProp) = GetFromToProperties(property);
                    EditorGUI.ObjectField(fromRect, fromProp, EditorGUIUtilities.TempContent("F"));
                    EditorGUI.ObjectField(toRect, toProp, EditorGUIUtilities.TempContent("T"));
                }
            }
        }

        protected override void CreateOptionsMenu(GenericMenu menu, TweenPlayer player, int index)
        {
            base.CreateOptionsMenu(menu, player, index);

            if (!from || !target) menu.AddDisabledItem(new GUIContent("From = Current"));
            else menu.AddItem(new GUIContent("From = Current"), false, () =>
            {
                Undo.RecordObject(from, "From = Current");
                from.position = currentPosition;
                from.rotation = currentRotation;
                from.localScale = currentLocalScale;
            });

            if (!to || !target) menu.AddDisabledItem(new GUIContent("To = Current"));
            else menu.AddItem(new GUIContent("To = Current"), false, () =>
            {
                Undo.RecordObject(to, "To = Current");
                to.position = currentPosition;
                to.rotation = currentRotation;
                to.localScale = currentLocalScale;
            });

            if (!from || !target) menu.AddDisabledItem(new GUIContent("Current = From"));
            else menu.AddItem(new GUIContent("Current = From"), false, () =>
            {
                Undo.RecordObject(target, "Current = From");
                currentPosition = from.position;
                currentRotation = from.rotation;
                currentLocalScale = from.localScale;
            });

            if (!to || !target) menu.AddDisabledItem(new GUIContent("Current = To"));
            else menu.AddItem(new GUIContent("Current = To"), false, () =>
            {
                Undo.RecordObject(target, "Current = To");
                currentPosition = to.position;
                currentRotation = to.rotation;
                currentLocalScale = to.localScale;
            });
        }

#endif // UNITY_EDITOR

    } // class TweenTransform

} // namespace UnityExtensions.Tween