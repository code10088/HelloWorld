#if UNITY_EDITOR

#if UNITY_2019_3_OR_NEWER && !(UNITY_2019_3_0 || UNITY_2019_3_1 || UNITY_2019_3_2 || UNITY_2019_3_3 || UNITY_2019_3_4 || UNITY_2019_3_5 || UNITY_2019_3_6)
#define SERIALIZE_REFERENCE_UNDO_FIXED
#endif

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityExtensions.Editor;

namespace UnityExtensions.Tween
{
    public abstract partial class TweenAnimation
    {
        static TweenAnimation _clipboard;

        static Dictionary<Type, TweenAnimationAttribute> _allTypes;

        internal static Dictionary<Type, TweenAnimationAttribute> allTypes
        {
            get
            {
                if (_allTypes == null)
                {
                    _allTypes = new Dictionary<Type, TweenAnimationAttribute>();
                    var types = TypeCache.GetTypesWithAttribute<TweenAnimationAttribute>();
                    foreach (var t in types)
                    {
                        if (t.IsSubclassOf(typeof(TweenAnimation)) && !t.IsAbstract)
                            _allTypes.Add(t, (TweenAnimationAttribute)(t.GetCustomAttributes(typeof(TweenAnimationAttribute), false)[0]));
                    }
                }
                return _allTypes;
            }
        }

        public virtual void Reset(TweenPlayer player)
        {
            enabled = true;
            _minNormalizedTime = 0f;
            _maxNormalizedTime = 1f;
            _holdBeforeStart = true;
            _holdAfterEnd = true;
            _interpolator = default;
            _foldout = true;
        }

        public virtual void OnValidate(TweenPlayer player) { }

        public abstract void RecordState();

        public abstract void RestoreState();

        protected abstract void OnPropertiesGUI(TweenPlayer player, SerializedProperty property);

        protected virtual void CreateOptionsMenu(GenericMenu menu, TweenPlayer player, int index)
        {
            menu.AddItem(new GUIContent("Reset"), () => ResetWithUndo(player), player.playing);
            menu.AddItem(new GUIContent("Remove"), () => RemoveWithUndo(player, index), player.playing);

            menu.AddSeparator(string.Empty);

            if (string.IsNullOrEmpty(_comment))
            {
                menu.AddItem(new GUIContent("Add Comment"), false, () => AddComment(player));
            }
            else
            {
                menu.AddItem(new GUIContent("Remove Comment"), false, () => RemoveComment(player));
            }

            menu.AddSeparator(string.Empty);

            menu.AddItem(new GUIContent("Move Up"), () => player.MoveUpAnimationWithUndo(index), index == 0);
            menu.AddItem(new GUIContent("Move Down"), () => player.MoveDownAnimationWithUndo(index), index == player.animationCount - 1);

            menu.AddSeparator(string.Empty);

            menu.AddItem(new GUIContent("Copy"), false, Copy);

            menu.AddItem(new GUIContent("Paste Values"), () => PasteValuesWithUndo(player), player.playing || _clipboard == null);
            menu.AddItem(new GUIContent("Paste As New"), () => PasteAsNewWithUndo(player), player.playing || _clipboard == null);
        }


        void ResetWithUndo(TweenPlayer player)
        {
            Undo.RecordObject(player, "Reset Animation");
            Reset(player);
        }


        void RemoveWithUndo(TweenPlayer player, int index)
        {
#if SERIALIZE_REFERENCE_UNDO_FIXED
            Undo.RecordObject(player, "Remove Animation");
#else
            Undo.RegisterCompleteObjectUndo(player, "Remove Animation");
#endif
            player.RemoveAnimation(index);
        }


        void AddComment(TweenPlayer player)
        {
            Undo.RecordObject(player, "Add Comment");
            _comment = "Comment";
        }


        void RemoveComment(TweenPlayer player)
        {
            Undo.RecordObject(player, "Remove Comment");
            _comment = null;
        }


        void Copy()
        {
            _clipboard = (TweenAnimation)Activator.CreateInstance(GetType());
            EditorUtility.CopySerializedManagedFieldsOnly(this, _clipboard);
        }


        void PasteValuesWithUndo(TweenPlayer player)
        {
            Undo.RecordObject(player, "Paste Values Animation");
            EditorUtility.CopySerializedManagedFieldsOnly(_clipboard, this);
        }


        void PasteAsNewWithUndo(TweenPlayer player)
        {
#if SERIALIZE_REFERENCE_UNDO_FIXED
            Undo.RecordObject(player, "Paste As New Animation");
#else
            Undo.RegisterCompleteObjectUndo(player, "Paste As New Animation");
#endif
            var item = player.AddAnimation(_clipboard.GetType());
            EditorUtility.CopySerializedManagedFieldsOnly(_clipboard, item);
        }


        protected static void FromToFieldLayout(string label, ref float from, ref float to, out bool fromChanged, out bool toChanged)
        {
            var rect = EditorGUILayout.GetControlRect();
            float labelWidth = EditorGUIUtility.labelWidth;

            var fromRect = new Rect(rect.x + labelWidth, rect.y, (rect.width - labelWidth - 8) / 2, rect.height);
            var toRect = new Rect(rect.xMax - fromRect.width, fromRect.y, fromRect.width, fromRect.height);
            rect.width = labelWidth - 8;

            var content = EditorGUIUtilities.TempContent(label);
            EditorGUI.LabelField(rect, content);

            rect.width = EditorStyles.label.CalcSize(content).x;
            float delta = EditorGUIUtilities.DragValue(rect, 0, 0.01f);

            using (LabelWidthScope.New(12))
            {
                float newFrom = from, newTo = to;
                if (delta != 0)
                {
                    delta = (float)Math.Round(delta, 2);
                    newFrom = MathUtilities.RoundToSignificantDigitsFloat(newFrom + delta, 6);
                    newTo = MathUtilities.RoundToSignificantDigitsFloat(newTo + delta, 6);
                }

                newFrom = EditorGUI.FloatField(fromRect, "F", newFrom);
                newTo = EditorGUI.FloatField(toRect, "T", newTo);

                fromChanged = from != newFrom;
                from = newFrom;

                toChanged = to != newTo;
                to = newTo;
            }
        }


        protected static void FromToFieldLayout(string label, ref float from, ref float to)
        {
            FromToFieldLayout(label, ref from, ref to, out _, out _);
        }


        protected static void FromToFieldLayout(string label, SerializedProperty from, SerializedProperty to)
        {
            float fromValue = from.floatValue;
            float toValue = to.floatValue;

            FromToFieldLayout(label, ref fromValue, ref toValue, out bool fromChanged, out bool toChanged);

            if (fromChanged) from.floatValue = fromValue;
            if (toChanged) to.floatValue = toValue;
        }


        protected static void FromToFieldLayout(string label, ref float from, ref float to, ref bool toggle, out bool fromChanged, out bool toChanged, out bool toggleChanged)
        {
            var rect = EditorGUILayout.GetControlRect();
            float labelWidth = EditorGUIUtility.labelWidth;

            var fromRect = new Rect(rect.x + labelWidth, rect.y, (rect.width - labelWidth - 8) / 2, rect.height);
            var toRect = new Rect(rect.xMax - fromRect.width, fromRect.y, fromRect.width, fromRect.height);
            var toggleRect = new Rect(rect.x, rect.y, rect.height, rect.height);
            rect.width = labelWidth - 8 - toggleRect.width;
            rect.x = toggleRect.xMax;

            bool newToggle = EditorGUI.Toggle(toggleRect, toggle);
            using (DisabledScope.New(!newToggle))
            {
                var content = EditorGUIUtilities.TempContent(label);
                EditorGUI.LabelField(rect, content);

                rect.width = EditorStyles.label.CalcSize(content).x;
                float delta = EditorGUIUtilities.DragValue(rect, 0, 0.01f);

                using (LabelWidthScope.New(12))
                {
                    float newFrom = from, newTo = to;
                    if (delta != 0)
                    {
                        delta = (float)Math.Round(delta, 2);
                        newFrom = MathUtilities.RoundToSignificantDigitsFloat(newFrom + delta, 6);
                        newTo = MathUtilities.RoundToSignificantDigitsFloat(newTo + delta, 6);
                    }

                    newFrom = EditorGUI.FloatField(fromRect, "F", newFrom);
                    newTo = EditorGUI.FloatField(toRect, "T", newTo);

                    fromChanged = from != newFrom;
                    from = newFrom;

                    toChanged = to != newTo;
                    to = newTo;

                    toggleChanged = toggle != newToggle;
                    toggle = newToggle;
                }
            }
        }


        protected static void FromToFieldLayout(string label, ref float from, ref float to, ref bool toggle)
        {
            FromToFieldLayout(label, ref from, ref to, ref toggle, out _, out _, out _);
        }


        protected static void FromToFieldLayout(string label, SerializedProperty from, SerializedProperty to, SerializedProperty toggle)
        {
            float fromValue = from.floatValue;
            float toValue = to.floatValue;
            bool toggleValue = toggle.boolValue;

            FromToFieldLayout(label, ref fromValue, ref toValue, ref toggleValue, out bool fromChanged, out bool toChanged, out bool toggleChanged);

            if (fromChanged) from.floatValue = fromValue;
            if (toChanged) to.floatValue = toValue;
            if (toggleChanged) toggle.boolValue = toggleValue;
        }


        protected static void FromToFieldLayout(string label, SerializedProperty from, SerializedProperty to, SerializedProperty toggle, float min, float max)
        {
            float fromValue = from.floatValue;
            float toValue = to.floatValue;
            bool toggleValue = toggle.boolValue;

            FromToFieldLayout(label, ref fromValue, ref toValue, ref toggleValue, out bool fromChanged, out bool toChanged, out bool toggleChanged);

            if (fromChanged) from.floatValue = Mathf.Clamp(fromValue, min, max);
            if (toChanged) to.floatValue = Mathf.Clamp(toValue, min, max);
            if (toggleChanged) toggle.boolValue = toggleValue;
        }


        protected static void HoldStartEndLayout(string label, ref bool start, ref bool end, bool startEnabled, bool endEnabled)
        {
            var rect = EditorGUILayout.GetControlRect();
            float labelWidth = EditorGUIUtility.labelWidth;

            var startRect = new Rect(rect.x + labelWidth, rect.y, (rect.width - labelWidth - 8) / 2, rect.height);
            var endRect = new Rect(rect.xMax - startRect.width, startRect.y, startRect.width, startRect.height);
            rect.width = labelWidth - 8;

            var content = EditorGUIUtilities.TempContent(label);
            EditorGUI.LabelField(rect, content);

            startRect.xMin += 12;
            endRect.xMin += 12;

            using (DisabledScope.New(!startEnabled))
                start = GUI.Toggle(startRect, start, "Start", EditorStyles.miniButton);

            using (DisabledScope.New(!endEnabled))
                end = GUI.Toggle(endRect, end, "End", EditorStyles.miniButton);
        }


        internal void OnInspectorGUI(int index, TweenPlayer player, SerializedProperty property)
        {
            var rect = EditorGUILayout.GetControlRect();
            var rect2 = rect;

            // foldout
            using (var scope = ChangeCheckScope.New(player))
            {
                rect2.width = rect.height;
                var result = GUI.Toggle(rect2, _foldout, GUIContent.none, EditorStyles.foldout);
                if (scope.changed) _foldout = result;
            }

            // enabled
            using (var scope = ChangeCheckScope.New(player))
            {
                rect2.x = rect2.xMax;
                var result = EditorGUI.ToggleLeft(rect2, GUIContent.none, enabled);
                if (scope.changed) enabled = result;
            }

            var optionsIcon = EditorGUIUtilities.paneOptionsIcon;

            // name
            rect2.x = rect2.xMax;
            rect2.xMax = rect.xMax - optionsIcon.width;

            using (var scope = ChangeCheckScope.New(player))
            {
                var result = GUI.Toggle(rect2, _foldout, allTypes[GetType()].name, EditorStyles.boldLabel);
                if (scope.changed) _foldout = result;
            }

            //EditorGUI.LabelField(rect2, allTypes[GetType()].name, EditorStyles.boldLabel);

            // options
            rect.Set(rect2.xMax, rect.y + 4, optionsIcon.width, optionsIcon.height);
            if (GUI.Button(rect, EditorGUIUtilities.TempContent(image: optionsIcon), GUIStyle.none))
            {
                var menu = new GenericMenu();
                CreateOptionsMenu(menu, player, index);
                menu.DropDown(rect);
            }

            rect = EditorGUILayout.GetControlRect(false, 3);
            rect.xMin += EditorGUIUtility.singleLineHeight * 2;
            rect.xMax -= EditorGUIUtility.singleLineHeight * 2;

            // progress
            EditorGUI.DrawRect(rect, TweenPlayer.Editor.progressBackgroundInvalid);

            rect2.Set(rect.x + minNormalizedTime * rect.width, rect.y,
                Mathf.Max(1, rect.width * (maxNormalizedTime - minNormalizedTime)), rect.height);

            if (enabled)
            {
                rect.width = Mathf.Round(rect.width * player.normalizedTime);
                EditorGUI.DrawRect(rect, TweenPlayer.Editor.progressForegroundInvalid);
            }

            EditorGUI.DrawRect(rect2, TweenPlayer.Editor.progressBackgroundValid);

            if (enabled)
            {
                rect2 = rect.GetIntersection(rect2);
                if (rect2.width > 0) EditorGUI.DrawRect(rect2, TweenPlayer.Editor.progressForegroundValid);
            }

            if (!string.IsNullOrEmpty(_comment))
            {
                using (var scope = ChangeCheckScope.New(player))
                {
                    rect = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight);

                    var result = EditorGUI.DelayedTextField(rect, _comment, EditorStyles.centeredGreyMiniLabel);
                    if (scope.changed) _comment = result;
                }
            }

            GUILayout.Space(4);

            if (_foldout)
            {
                using (var scope = ChangeCheckScope.New(player))
                {
                    float min = minNormalizedTime * player.duration;
                    float max = maxNormalizedTime * player.duration;

                    FromToFieldLayout("Time Range", ref min, ref max, out bool fromChanged, out bool toChanged);

                    if (scope.changed)
                    {
                        min /= player.duration;
                        max /= player.duration;
                        if (fromChanged) minNormalizedTime = Mathf.Min(min, max);
                        if (toChanged) maxNormalizedTime = Mathf.Max(max, min);
                    }
                }

                if (minNormalizedTime > 0f || maxNormalizedTime < 1f)
                {
                    using (var scope = ChangeCheckScope.New(player))
                    {
                        bool newHoldBeforeStart = _holdBeforeStart;
                        bool newHoldAfterEnd = _holdAfterEnd;
                        HoldStartEndLayout("Hold", ref newHoldBeforeStart, ref newHoldAfterEnd, minNormalizedTime > 0f, maxNormalizedTime < 1f);
                        if (scope.changed)
                        {
                            _holdBeforeStart = newHoldBeforeStart;
                            _holdAfterEnd = newHoldAfterEnd;
                        }
                    }
                }

                EditorGUILayout.PropertyField(property.FindPropertyRelative(nameof(_interpolator)));
                OnPropertiesGUI(player, property);
                GUILayout.Space(4);
            }

            EditorGUI.DrawRect(EditorGUILayout.GetControlRect(false, 1), TweenPlayer.Editor.separatorLineColor);
        }

    } // TweenAnimation

} // namespace UnityExtensions.TweenController

#endif