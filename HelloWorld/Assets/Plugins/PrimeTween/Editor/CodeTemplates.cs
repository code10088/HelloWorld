/*
// ReSharper disable PossibleNullReferenceException
// ReSharper disable UnusedMember.Global
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedMember.Local
using System;
using System.ComponentModel;
using JetBrains.Annotations;
using TweenType = PrimeTween.TweenAnimation.TweenType;

namespace PrimeTween {
    internal static class CodeTemplates {
        public static Tween PositionAtSpeed([NotNull] UnityEngine.Transform target,                                 UnityEngine.Vector3 endValue, float averageSpeed, Easing ease = default, int cycles = 1, CycleMode cycleMode = CycleMode.Restart, float startDelay = 0, float endDelay = 0, bool useUnscaledTime = false) => PositionAtSpeed(target, new TweenSettings<UnityEngine.Vector3>(            endValue, new TweenSettings(averageSpeed, ease, cycles, cycleMode, startDelay, endDelay, useUnscaledTime)));
        public static Tween PositionAtSpeed([NotNull] UnityEngine.Transform target, UnityEngine.Vector3 startValue, UnityEngine.Vector3 endValue, float averageSpeed, Easing ease = default, int cycles = 1, CycleMode cycleMode = CycleMode.Restart, float startDelay = 0, float endDelay = 0, bool useUnscaledTime = false) => PositionAtSpeed(target, new TweenSettings<UnityEngine.Vector3>(startValue, endValue, new TweenSettings(averageSpeed, ease, cycles, cycleMode, startDelay, endDelay, useUnscaledTime)));
               static Tween PositionAtSpeed([NotNull] UnityEngine.Transform target, TweenSettings<UnityEngine.Vector3> settings) {
            var speed = settings.settings.duration;
            if (speed <= 0) {
                UnityEngine.Debug.LogError($"Invalid speed provided to the Tween.{nameof(PositionAtSpeed)}() method: {speed}.");
                return default;
            }
            if (settings.startFromCurrent) {
                settings.startFromCurrent = false;
                settings.startValue = target.position;
            }
            settings.settings.duration = Extensions.CalcDistance(settings.startValue, settings.endValue) / speed;
            return Tween.Position(target, settings);
        }


        [EditorBrowsable(EditorBrowsableState.Never), Obsolete(ObsoleteMessages.tweenSettingsOverloadEndValue)]      public static Tween METHOD_NAME([NotNull] UnityEngine.Camera target,                    Single endValue, TweenSettings settings) => METHOD_NAME(target, new TweenSettings<float>(            endValue, settings));
        [EditorBrowsable(EditorBrowsableState.Never), Obsolete(ObsoleteMessages.tweenSettingsOverloadStartEndValue)] public static Tween METHOD_NAME([NotNull] UnityEngine.Camera target, Single startValue, Single endValue, TweenSettings settings) => METHOD_NAME(target, new TweenSettings<float>(startValue, endValue, settings));
        public static Tween METHOD_NAME([NotNull] UnityEngine.Camera target,                    Single endValue, float duration, Easing ease = default, int cycles = 1, CycleMode cycleMode = CycleMode.Restart, float startDelay = 0, float endDelay = 0, bool useUnscaledTime = false)    => METHOD_NAME(target, new TweenSettings<float>(            endValue, duration, ease, cycles, cycleMode, startDelay, endDelay, useUnscaledTime));
        public static Tween METHOD_NAME([NotNull] UnityEngine.Camera target, Single startValue, Single endValue, float duration, Easing ease = default, int cycles = 1, CycleMode cycleMode = CycleMode.Restart, float startDelay = 0, float endDelay = 0, bool useUnscaledTime = false)    => METHOD_NAME(target, new TweenSettings<float>(startValue, endValue, duration, ease, cycles, cycleMode, startDelay, endDelay, useUnscaledTime));
        public static Tween METHOD_NAME([NotNull] UnityEngine.Camera target, TweenSettings<float> settings) => animate(target, ref settings, TweenType.CameraOrthographicSize);


        [EditorBrowsable(EditorBrowsableState.Never), Obsolete(ObsoleteMessages.tweenSettingsOverloadStartEndValue)]
        public static Tween Custom_TEMPLATE(                       Single startValue, Single endValue, TweenSettings settings, [NotNull] Action<   Single> onValueChange)                 => Custom_TEMPLATE(        new TweenSettings<float>(startValue, endValue,   settings), onValueChange);
        [EditorBrowsable(EditorBrowsableState.Never), Obsolete(ObsoleteMessages.tweenSettingsOverloadStartEndValue)]
        public static Tween Custom_TEMPLATE<T>([NotNull] T target, Single startValue, Single endValue, TweenSettings settings, [NotNull] Action<T, Single> onValueChange) where T : class => Custom_internal(target, new TweenSettings<float>(startValue, endValue,   settings), onValueChange);
        #if PRIME_TWEEN_EXPERIMENTAL
        public static Tween CustomAdditive<T> ([NotNull] T target, Single deltaValue,                  TweenSettings settings, [NotNull] Action<T, Single> onDeltaChange) where T : class => Custom_internal(target, new TweenSettings<float>(default,    deltaValue, settings), onDeltaChange, true);
        #endif

        public static Tween Custom_TEMPLATE                       (Single startValue, Single endValue, float duration,         [NotNull] Action<   Single> onValueChange, Easing ease = default, int cycles = 1, CycleMode cycleMode = CycleMode.Restart, float startDelay = 0, float endDelay = 0, bool useUnscaledTime = false)                 => Custom_TEMPLATE(        new TweenSettings<float>(startValue, endValue, duration, ease, cycles, cycleMode, startDelay, endDelay, useUnscaledTime), onValueChange);
        public static Tween Custom_TEMPLATE<T>([NotNull] T target, Single startValue, Single endValue, float duration,         [NotNull] Action<T, Single> onValueChange, Easing ease = default, int cycles = 1, CycleMode cycleMode = CycleMode.Restart, float startDelay = 0, float endDelay = 0, bool useUnscaledTime = false) where T : class => Custom_internal(target, new TweenSettings<float>(startValue, endValue, duration, ease, cycles, cycleMode, startDelay, endDelay, useUnscaledTime), onValueChange);
        public static Tween Custom_TEMPLATE<T>([NotNull] T target, TweenSettings<float> settings,                              [NotNull] Action<T, Single> onValueChange)                                                                                                                                                         where T : class => Custom_internal(target, settings, onValueChange);
        public static Tween Custom_TEMPLATE(                       TweenSettings<float> settings,                              [NotNull] Action<   Single> onValueChange) {
            Assert.IsNotNull(onValueChange);
            if (settings.startFromCurrent) {
                UnityEngine.Debug.LogWarning(Constants.customTweensDontSupportStartFromCurrentWarning);
            }
            if (PrimeTweenManager.Instance.isDestroyed) {
                return default;
            }
            var tween = PrimeTweenManager.FetchTween(settings.settings._updateType);
            ref var rt = ref tween.managedData;
            ref var d = ref tween.data;

            d.startValue.CopyFrom(ref settings.startValue);
            rt.endValueOrDiff.CopyFrom(ref settings.endValue);

            tween.customOnValueChange = onValueChange;
            tween.Setup(PrimeTweenManager.dummyTarget, ref settings.settings, false, TweenType.CustomFloat, ref rt, ref d);
            tween.onValueChange = (ref TweenData rt2, ref UnmanagedTweenData d2) => {
                if (d2.isUpdating) {
                    UnityEngine.Debug.LogError(Constants.recursiveCallError);
                    return;
                }
                d2.isUpdating = true;

                var startValue = d2.startValue;
                var t = d2.easedInterpolationFactor;
                var diff = rt2.endValueOrDiff;
                var val = TweenData.FloatVal(startValue, t, diff);
                var _onValueChange = rt2.cold.customOnValueChange as Action<Single>;
                try {
                    _onValueChange(val);
                } finally {
                    d2.isUpdating = false;
                }
            };
            return PrimeTweenManager.Animate(ref rt, ref d);
        }
        static Tween Custom_internal<T>([NotNull] T target, TweenSettings<float> settings, [NotNull] Action<T, Single> onValueChange, bool isAdditive = false) where T : class {
            Assert.IsNotNull(onValueChange);
            if (settings.startFromCurrent) {
                UnityEngine.Debug.LogWarning(Constants.customTweensDontSupportStartFromCurrentWarning);
            }
            if (PrimeTweenManager.Instance.isDestroyed) {
                return default;
            }
            var tween = PrimeTweenManager.FetchTween(settings.settings._updateType);
            ref var rt = ref tween.managedData;
            ref var d = ref tween.data;

            d.startValue.CopyFrom(ref settings.startValue);
            d.isAdditive = isAdditive;
            rt.endValueOrDiff.CopyFrom(ref settings.endValue);

            tween.customOnValueChange = onValueChange;
            tween.Setup(target, ref settings.settings, false, TweenType.CustomFloat, ref rt, ref d);
            tween.onValueChange = (ref TweenData rt2, ref UnmanagedTweenData d2) => {
                if (d2.isUpdating) {
                    UnityEngine.Debug.LogError(Constants.recursiveCallError);
                    return;
                }
                d2.isUpdating = true;

                var startValue = d2.startValue;
                var t = d2.easedInterpolationFactor;
                var dataIsAdditive = d2.isAdditive;

                var _target = rt2.target as T;
                var diff = rt2.endValueOrDiff;

                Single val;
                if (dataIsAdditive) {
                    var newVal = TweenData.FloatVal(startValue, t, diff);
                    val = newVal.calcDelta(rt2.cold.prevVal);
                    rt2.cold.prevVal.single = newVal;
                } else {
                    val = TweenData.FloatVal(startValue, t, diff);
                }
                var _onValueChange = rt2.cold.customOnValueChange as Action<T, Single>;
                try {
                    _onValueChange(_target, val);
                } finally {
                    d2.isUpdating = false;
                }
            };
            return PrimeTweenManager.Animate(ref rt, ref d);
        }
        static Tween animate(object target, ref TweenSettings<float> settings, TweenType _tweenType) {
            if (PrimeTweenManager.Instance.isDestroyed) {
                return default;
            }
            var tween = PrimeTweenManager.FetchTween(settings.settings._updateType);
            ref var d = ref tween.data;
            ref var rt = ref tween.managedData;

            d.startValue.CopyFrom(ref settings.startValue);
            rt.endValueOrDiff.CopyFrom(ref settings.endValue);

            tween.Setup(target, ref settings.settings, settings.startFromCurrent, _tweenType, ref rt, ref d);
            return PrimeTweenManager.Animate(ref rt, ref d);
        }
        static Tween AnimateMaterial([CanBeNull] object target, long longParam, ref TweenSettings<float> settings, TweenType _tweenType) {
            if (PrimeTweenManager.Instance.isDestroyed) {
                return default;
            }
            #if DEVELOPMENT_BUILD || UNITY_EDITOR
            int propId = (int)longParam;
            if (target is UnityEngine.Material material && !Utils.IsValidMaterialProperty(_tweenType, target, propId)) {
                UnityEngine.Debug.LogWarning($"Material doesn't have a property with id '{propId}'.", material);
            }
            if (target is UnityEngine.Renderer renderer && !Utils.IsValidMaterialProperty(_tweenType, target, propId)) {
                UnityEngine.Debug.LogWarning($"Renderer's material doesn't have a property with id '{propId}'.", renderer);
            }
            #endif
            var tween = PrimeTweenManager.FetchTween(settings.settings._updateType);
            ref var d = ref tween.data;
            ref var rt = ref tween.managedData;

            d.startValue.CopyFrom(ref settings.startValue);
            rt.endValueOrDiff.CopyFrom(ref settings.endValue);

            tween.longParam = longParam;
            tween.Setup(target, ref settings.settings, settings.startFromCurrent, _tweenType, ref rt, ref d);
            return PrimeTweenManager.Animate(ref rt, ref d);
        }


#if PRIME_TWEEN_EXPERIMENTAL
        public static Tween PositionAdditive([NotNull] UnityEngine.Transform target, Single deltaValue, float duration, Easing ease = default,    int cycles = 1, CycleMode cycleMode = CycleMode.Restart, float startDelay = 0, float endDelay = 0, bool useUnscaledTime = false) => PositionAdditive(target, deltaValue, new TweenSettings(duration, ease, cycles, cycleMode, startDelay, endDelay, useUnscaledTime));
        public static Tween PositionAdditive([NotNull] UnityEngine.Transform target, Single deltaValue, TweenSettings settings)                                                                                                                                                    => CustomAdditive  (target, deltaValue, settings, (_target, delta) => additiveTweenSetter());
#endif

        static void additiveTweenSetter() {}
    }
}
*/
