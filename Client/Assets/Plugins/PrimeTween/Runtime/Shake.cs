// ReSharper disable UnusedMember.Global
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedMethodReturnValue.Global
using System;
using Random = System.Random;
using SuppressMessage = System.Diagnostics.CodeAnalysis.SuppressMessageAttribute;
using AnimationCurve = UnityEngine.AnimationCurve;
using Transform = UnityEngine.Transform;
using Camera = UnityEngine.Camera;
using Vector3 = UnityEngine.Vector3;
using Quaternion = UnityEngine.Quaternion;
using NotNull = JetBrains.Annotations.NotNullAttribute;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;
using TweenType = PrimeTween.TweenAnimation.TweenType;

namespace PrimeTween {
    public partial struct Tween {
        /// <summary>Shakes the camera.<br/>
        /// If the camera is perspective, shakes all angles.<br/>
        /// If the camera is orthographic, shakes the z angle and x/y coordinates.<br/>
        /// Reference strengthFactor values - light: 0.2, medium: 0.5, heavy: 1.0.</summary>
        public static Sequence ShakeCamera([NotNull] Camera camera, float strengthFactor, float duration = 0.5f, float frequency = ShakeSettings.defaultFrequency, float startDelay = 0, float endDelay = 0, bool useUnscaledTime = PrimeTweenConfig.defaultUseUnscaledTimeForShakes) {
            var transform = camera.transform;
            if (camera.orthographic) {
                float orthoPosStrength = strengthFactor * camera.orthographicSize * 0.03f;
                return Sequence.Create()
                    .Group(ShakeLocalPosition(transform, new ShakeSettings(new Vector3(orthoPosStrength, orthoPosStrength), duration, frequency, startDelay: startDelay, endDelay: endDelay, useUnscaledTime: useUnscaledTime)))
                    .Group(ShakeLocalRotation(transform, new ShakeSettings(new Vector3(0, 0, strengthFactor * 0.6f), duration, frequency, startDelay: startDelay, endDelay: endDelay, useUnscaledTime: useUnscaledTime)));
            }
            return Sequence.Create()
                .Group(ShakeLocalRotation(transform, new ShakeSettings(strengthFactor * Vector3.one, duration, frequency, startDelay: startDelay, endDelay: endDelay, useUnscaledTime: useUnscaledTime)));
        }

        public static Tween ShakeLocalPosition([NotNull] Transform target, Vector3 strength, float duration, float frequency = ShakeSettings.defaultFrequency, bool enableFalloff = true, Ease easeBetweenShakes = Ease.Default, float asymmetryFactor = 0f, int cycles = 1,
            float startDelay = 0, float endDelay = 0, bool useUnscaledTime = PrimeTweenConfig.defaultUseUnscaledTimeForShakes)
            => ShakeLocalPosition(target, new ShakeSettings(strength, duration, frequency, enableFalloff, easeBetweenShakes, asymmetryFactor, cycles, startDelay, endDelay, useUnscaledTime));
        public static Tween ShakeLocalPosition([NotNull] Transform target, ShakeSettings settings)
            => ShakeTransform(TweenType.ShakeLocalPosition, target, settings);
        public static Tween PunchLocalPosition([NotNull] Transform target, Vector3 strength, float duration, float frequency = ShakeSettings.defaultFrequency, bool enableFalloff = true, Ease easeBetweenShakes = Ease.Default, float asymmetryFactor = 0f, int cycles = 1,
            float startDelay = 0, float endDelay = 0, bool useUnscaledTime = PrimeTweenConfig.defaultUseUnscaledTimeForShakes)
            => PunchLocalPosition(target, new ShakeSettings(strength, duration, frequency, enableFalloff, easeBetweenShakes, asymmetryFactor, cycles, startDelay, endDelay, useUnscaledTime));
        public static Tween PunchLocalPosition([NotNull] Transform target, ShakeSettings settings) => ShakeLocalPosition(target, settings.WithPunch());

        public static Tween ShakeLocalRotation([NotNull] Transform target, Vector3 strength, float duration, float frequency = ShakeSettings.defaultFrequency, bool enableFalloff = true, Ease easeBetweenShakes = Ease.Default, float asymmetryFactor = 0f, int cycles = 1,
            float startDelay = 0, float endDelay = 0, bool useUnscaledTime = PrimeTweenConfig.defaultUseUnscaledTimeForShakes)
            => ShakeLocalRotation(target, new ShakeSettings(strength, duration, frequency, enableFalloff, easeBetweenShakes, asymmetryFactor, cycles, startDelay, endDelay, useUnscaledTime));
        public static Tween ShakeLocalRotation([NotNull] Transform target, ShakeSettings settings)
            => ShakeTransform(TweenType.ShakeLocalRotation,  target, settings);
        public static Tween PunchLocalRotation([NotNull] Transform target, Vector3 strength, float duration, float frequency = ShakeSettings.defaultFrequency, bool enableFalloff = true, Ease easeBetweenShakes = Ease.Default, float asymmetryFactor = 0f, int cycles = 1,
            float startDelay = 0, float endDelay = 0, bool useUnscaledTime = PrimeTweenConfig.defaultUseUnscaledTimeForShakes)
            => PunchLocalRotation(target, new ShakeSettings(strength, duration, frequency, enableFalloff, easeBetweenShakes, asymmetryFactor, cycles, startDelay, endDelay, useUnscaledTime));
        public static Tween PunchLocalRotation([NotNull] Transform target, ShakeSettings settings) => ShakeLocalRotation(target, settings.WithPunch());

        public static Tween ShakeScale([NotNull] Transform target, Vector3 strength, float duration, float frequency = ShakeSettings.defaultFrequency, bool enableFalloff = true, Ease easeBetweenShakes = Ease.Default, float asymmetryFactor = 0f, int cycles = 1,
            float startDelay = 0, float endDelay = 0, bool useUnscaledTime = PrimeTweenConfig.defaultUseUnscaledTimeForShakes)
            => ShakeScale(target, new ShakeSettings(strength, duration, frequency, enableFalloff, easeBetweenShakes, asymmetryFactor, cycles, startDelay, endDelay, useUnscaledTime));
        public static Tween ShakeScale([NotNull] Transform target, ShakeSettings settings)
            => ShakeTransform(TweenType.ShakeScale, target, settings);
        public static Tween PunchScale([NotNull] Transform target, Vector3 strength, float duration, float frequency = ShakeSettings.defaultFrequency, bool enableFalloff = true, Ease easeBetweenShakes = Ease.Default, float asymmetryFactor = 0f, int cycles = 1,
            float startDelay = 0, float endDelay = 0, bool useUnscaledTime = PrimeTweenConfig.defaultUseUnscaledTimeForShakes)
            => PunchScale(target, new ShakeSettings(strength, duration, frequency, enableFalloff, easeBetweenShakes, asymmetryFactor, cycles, startDelay, endDelay, useUnscaledTime));
        public static Tween PunchScale([NotNull] Transform target, ShakeSettings settings) => ShakeScale(target, settings.WithPunch());

        static Tween ShakeTransform(TweenType tweenType, [NotNull] Transform target, ShakeSettings settings) {
            if (PrimeTweenManager.Instance.isDestroyed) {
                return default;
            }
            var tween = PrimeTweenManager.FetchTween(settings._updateType);
            ref var rt = ref tween.managedData;
            ref var d = ref tween.data;

            prepareShakeData(settings, ref rt, ref d, target);
            var tweenSettings = settings.tweenSettings;
            tween.Setup(target, ref tweenSettings, true, tweenType, ref rt, ref d);
            return PrimeTweenManager.Animate(ref rt, ref d);
        }

        public static Tween ShakeCustom<T>([NotNull] T target, Vector3 startValue, ShakeSettings settings, [NotNull] Action<T, Vector3> onValueChange) where T : class {
            Assert.IsNotNull(onValueChange);
            if (PrimeTweenManager.Instance.isDestroyed) {
                return default;
            }
            var tween = PrimeTweenManager.FetchTween(settings._updateType);
            ref var rt = ref tween.managedData;
            ref var d = ref tween.data;

            d.startValue.CopyFrom(ref startValue);
            prepareShakeData(settings, ref rt, ref d, target);
            tween.customOnValueChange = onValueChange;
            var tweenSettings = settings.tweenSettings;
            tween.Setup(target, ref tweenSettings, false, TweenType.ShakeCustom, ref rt, ref d);
            tween.onValueChange = (ref TweenData rt2, ref UnmanagedTweenData d2) => {
                var _onValueChange = rt2.cold.customOnValueChange as Action<T, Vector3>;
                Assert.IsNotNull(_onValueChange);
                var val = d2.startValue.vector3 + getShakeVal(ref rt2, ref d2);
                _onValueChange(rt2.target as T, val);
            };
            return PrimeTweenManager.Animate(ref rt, ref d);
        }
        public static Tween PunchCustom<T>([NotNull] T target, Vector3 startValue, ShakeSettings settings, [NotNull] Action<T, Vector3> onValueChange) where T : class => ShakeCustom(target, startValue, settings.WithPunch(), onValueChange);

        static void prepareShakeData(ShakeSettings settings, ref TweenData rt, ref UnmanagedTweenData d, object target) {
            rt.endValueOrDiff.Reset(); // not used
            rt.cold.shakeData.Setup(settings, ref rt, ref d, target);
        }

        internal static Vector3 getShakeVal(ref TweenData rt, ref UnmanagedTweenData d) {
            float fadeInOutFactor = calcFadeInOutFactor(ref rt, ref d);
            return rt.shakeData.getNextVal(ref rt, ref d) * fadeInOutFactor;
        }

        static float calcFadeInOutFactor(ref TweenData tween, ref UnmanagedTweenData d) {
            float animationDuration = d.animationDuration;
            var elapsedTimeInterpolating = d.easedInterpolationFactor * animationDuration;
            Assert.IsTrue(elapsedTimeInterpolating >= 0f);
            if (animationDuration == 0f) {
                return 0f;
            }
            Assert.IsTrue(animationDuration > 0f);
            float halfDuration = animationDuration * 0.5f;
            var oneShakeDuration = 1f / tween.cold.shakeData.frequency;
            if (oneShakeDuration > halfDuration) {
                oneShakeDuration = halfDuration;
            }
            float fadeInDuration = oneShakeDuration * 0.5f;
            if (elapsedTimeInterpolating < fadeInDuration) {
                return Mathf.InverseLerp(0f, fadeInDuration, elapsedTimeInterpolating);
            }
            var fadeoutStartTime = animationDuration - oneShakeDuration;
            Assert.IsTrue(fadeoutStartTime > 0f, tween.cold.id);
            if (elapsedTimeInterpolating > fadeoutStartTime) {
                return Mathf.InverseLerp(animationDuration, fadeoutStartTime, elapsedTimeInterpolating);
            }
            return 1f;
        }
    }

    #if PRIME_TWEEN_INSPECTOR_DEBUGGING && UNITY_EDITOR
    [Serializable]
    #endif
    internal struct ShakeData {
        float t;
        Vector3 from, to;
        float symmetryFactor;
        int falloffEaseInt;
        AnimationCurve customStrengthOverTime;
        Ease easeBetweenShakes;
        internal Vector3 strengthPerAxis { get; private set; }
        internal float frequency { get; private set; }
        float prevInterpolationFactor;
        int prevCyclesDone;

        const int disabledFalloff = -42;
        internal bool isAlive => frequency != 0f;

        internal void Setup(ShakeSettings settings, ref TweenData rt, ref UnmanagedTweenData d, object target) {
            d.isPunch = settings.isPunch;
            symmetryFactor = Mathf.Clamp01(1 - settings.asymmetry);
            {
                var _strength = settings.strength;
                if (_strength == default) {
                    Debug.LogError("Shake's strength is (0, 0, 0).");
                }
                strengthPerAxis = _strength;
            }
            {
                var _frequency = settings.frequency;
                if (_frequency <= 0) {
                    Debug.LogError($"Shake's frequency should be > 0f, but was {_frequency}.", target as Object);
                    _frequency = ShakeSettings.defaultFrequency;
                }
                frequency = _frequency;
            }
            {
                if (settings.enableFalloff) {
                    var _falloffEase = settings.falloffEase;
                    var _customStrengthOverTime = settings.strengthOverTime;
                    if (_falloffEase == Ease.Default) {
                        _falloffEase = Ease.Linear;
                    }
                    if (_falloffEase == Ease.Custom) {
                        if (_customStrengthOverTime == null || !TweenSettings.ValidateCustomCurve(_customStrengthOverTime)) {
                            Debug.LogError($"Shake falloff is Ease.Custom, but {nameof(ShakeSettings.strengthOverTime)} is not configured correctly. Using Ease.Linear instead.", target as Object);
                            _falloffEase = Ease.Linear;
                        }
                    }
                    falloffEaseInt = (int)_falloffEase;
                    customStrengthOverTime = _customStrengthOverTime;
                } else {
                    falloffEaseInt = disabledFalloff;
                }
            }
            {
                var _easeBetweenShakes = settings.easeBetweenShakes;
                if (_easeBetweenShakes == Ease.Custom) {
                    Debug.LogError($"{nameof(ShakeSettings.easeBetweenShakes)} doesn't support Ease.Custom.", target as Object);
                    _easeBetweenShakes = Ease.OutQuad;
                }
                if (_easeBetweenShakes == Ease.Default) {
                    _easeBetweenShakes = PrimeTweenManager.defaultShakeEase;
                }
                easeBetweenShakes = _easeBetweenShakes;
            }
            onCycleComplete(ref rt, ref d);
        }

        internal void onCycleComplete(ref TweenData rt, ref UnmanagedTweenData d) {
            Assert.IsTrue(isAlive);
            resetAfterCycle();
            d.shakeSign = d.isPunch || PrimeTweenManager.random.NextDouble() < 0.5;
            to = generateShakePoint(ref d);
        }

        static int getMainAxisIndex(Vector3 strengthByAxis) {
            int mainAxisIndex = -1;
            float maxStrength = float.NegativeInfinity;
            for (int i = 0; i < 3; i++) {
                var strength = Mathf.Abs(strengthByAxis[i]);
                if (strength > maxStrength) {
                    maxStrength = strength;
                    mainAxisIndex = i;
                }
            }
            Assert.IsTrue(mainAxisIndex >= 0);
            return mainAxisIndex;
        }

        internal Vector3 getNextVal(ref TweenData rt, ref UnmanagedTweenData d) {
            var interpolationFactor = d.easedInterpolationFactor;
            Assert.IsTrue(interpolationFactor <= 1);

            int cyclesDiff = d.getCyclesDone() - prevCyclesDone;
            prevCyclesDone = d.getCyclesDone();
            if (interpolationFactor == 0f || (cyclesDiff > 0 && d.getCyclesDone() != d.cyclesTotal)) {
                onCycleComplete(ref rt, ref d);
                prevInterpolationFactor = interpolationFactor;
            }

            float animationDuration = d.animationDuration;
            var dt = (interpolationFactor - prevInterpolationFactor) * animationDuration;
            prevInterpolationFactor = interpolationFactor;

            var strengthOverTime = calcStrengthOverTime(interpolationFactor);
            var frequencyFactor = Mathf.Clamp01(strengthOverTime * 3f); // handpicked formula that describes the relationship between strength and frequency

            // The initial velocity should twice as big because the first shake starts from zero (twice as short as total range).
            var elapsedTimeInterpolating = d.easedInterpolationFactor * animationDuration;
            var halfShakeDuration = 0.5f / rt.shakeData.frequency;
            float iniVelFactor = elapsedTimeInterpolating < halfShakeDuration ? 2f : 1f;

            t += frequency * dt * frequencyFactor * iniVelFactor;
            if (t < 0f || t >= 1f) {
                d.shakeSign = !d.shakeSign;
                if (t < 0f) {
                    t = 1f;
                    to = from;
                    from = generateShakePoint(ref d);
                } else {
                    t = 0f;
                    from = to;
                    to = generateShakePoint(ref d);
                }
            }

            Vector3 result = default;
            for (int i = 0; i < 3; i++) {
                result[i] = Mathf.Lerp(from[i], to[i], StandardEasing.Evaluate(t, easeBetweenShakes)) * strengthOverTime;
            }
            return result;
        }

        Vector3 generateShakePoint(ref UnmanagedTweenData d) {
            var mainAxisIndex = getMainAxisIndex(strengthPerAxis);
            Vector3 result = default;
            float signFloat = d.shakeSign ? 1f : -1f;
            for (int i = 0; i < 3; i++) {
                var strength = strengthPerAxis[i];
                if (d.isPunch) {
                    result[i] = clampBySymmetryFactor(strength * signFloat, strength, symmetryFactor);
                } else {
                    result[i] = i == mainAxisIndex ? calcMainAxisEndVal(signFloat, strength, symmetryFactor) : calcNonMainAxisEndVal(strength, symmetryFactor);
                }
            }
            return result;
        }

        float calcStrengthOverTime(float interpolationFactor) {
            if (falloffEaseInt == disabledFalloff) {
                return 1;
            }
            var falloffEase = (Ease)falloffEaseInt;
            if (falloffEase != Ease.Custom) {
                return 1 - StandardEasing.Evaluate(interpolationFactor, falloffEase);
            }
            Assert.IsNotNull(customStrengthOverTime);
            return customStrengthOverTime.Evaluate(interpolationFactor);
        }

        static float calcMainAxisEndVal(float velocity, float strength, float symmetryFactor) {
            float result = Mathf.Sign(velocity) * strength * RandomRange(0.6f, 1f); // doesn't matter if we're using strength or its abs because velocity alternates
            return clampBySymmetryFactor(result, strength, symmetryFactor);
        }

        static float clampBySymmetryFactor(float val, float strength, float symmetryFactor) {
            if (strength > 0) {
                return Mathf.Clamp(val, -strength * symmetryFactor, strength);
            }
            return Mathf.Clamp(val, strength, -strength * symmetryFactor);
        }

        static float calcNonMainAxisEndVal(float strength, float symmetryFactor) {
            if (strength > 0) {
                return RandomRange(-strength * symmetryFactor, strength);
            }
            return RandomRange(strength, -strength * symmetryFactor);
        }

        static float RandomRange(float minInclusive, float max) {
            double val = PrimeTweenManager.random.NextDouble();
            return (float)(minInclusive + val * (max - minInclusive));
        }

        internal static bool TryTakeStartValueFromOtherShake(ref TweenData newTween, ref UnmanagedTweenData newTweenData) {
            if (!newTween.shakeData.isAlive) {
                return false;
            }
            var shakeTransform = newTween.target as Transform;
            if (shakeTransform == null) {
                return false;
            }
            var shakes = PrimeTweenManager.Instance.shakes;
            var key = (shakeTransform, newTweenData.tweenType);
            if (!shakes.TryGetValue(key, out var data)) {
                var startValue = Utils.GetAnimatedValue(newTween.target, newTweenData.tweenType, newTween.cold.longParam);
                shakes.Add(key, (startValue, 1));
                return false;
            }
            Assert.IsTrue(data.count >= 1);
            newTweenData.startValue = data.startValue;
            // Debug.Log($"tryTakeStartValueFromOtherShake {data.startValue.vector3}");
            data.count++;
            shakes[key] = data;
            return true;
        }

        internal void Reset(object target, TweenType tweenType) {
            Assert.IsTrue(isAlive);
            var shakeTransform = target as Transform;
            if (shakeTransform != null) {
                var key = (shakeTransform, tweenType);
                var shakes = PrimeTweenManager.Instance.shakes;
                if (shakes.TryGetValue(key, out var data)) {
                    // no key present if it's a ShakeCustom() with Transform target because custom shakes have startFromCurrent == false and aren't added to shakes dict
                    Assert.IsTrue(data.count >= 1);
                    data.count--;
                    if (data.count == 0) {
                        bool isRemoved = shakes.Remove(key);
                        Assert.IsTrue(isRemoved);
                    } else {
                        shakes[key] = data;
                    }
                }
            }

            resetAfterCycle();
            customStrengthOverTime = null;
            frequency = 0f;
            prevInterpolationFactor = 0f;
            prevCyclesDone = 0;
            Assert.IsFalse(isAlive);
        }

        void resetAfterCycle() {
            t = 0f;
            from = default;
        }
    }
}
