// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedMethodReturnValue.Global
#if PRIME_TWEEN_DOTWEEN_ADAPTER
using System;
using System.Collections;
using JetBrains.Annotations;
using UnityEngine;

namespace PrimeTween {
    public static partial class DOTweenAdapter {
        static int remapFrequency(float frequency) {
            return (int) (frequency * 1.35f);
        }

        public static Tween DOShakePosition([NotNull] this Component target, float duration, float strength, int vibrato = 10, float randomness = 90, bool snapping = false, bool fadeOut = true)
            => DOShakePosition(target, duration, Vector3.one * strength, vibrato, randomness, snapping, fadeOut);
        public static Tween DOShakePosition([NotNull] this Component target, float duration, Vector3 strength, int vibrato = 10, float randomness = 90, bool snapping = false, bool fadeOut = true) {
            if (Math.Abs(randomness - 90f) > 0.001f) {
                Debug.LogWarning("PrimeTween doesn't support " + nameof(randomness));
            }
            if (snapping) {
                Debug.LogWarning("PrimeTween doesn't support " + nameof(snapping));
            }
            var settings = new ShakeSettings(strength, duration, vibrato);
            if (fadeOut) {
                settings.enableFalloff = true;
                settings.frequency = remapFrequency(settings.frequency);
            }
            return Tween.ShakeLocalPosition(target.transform, settings);
        }
        public static Tween DOPunchPosition([NotNull] this Component target, Vector3 punch, float duration, int vibrato = 10, float elasticity = 1, bool snapping = false) {
            if (snapping) {
                Debug.LogWarning("PrimeTween doesn't support " + nameof(snapping));
            }
            var shakeSettings = new ShakeSettings(punch, duration, remapFrequency(vibrato), asymmetryFactor: 1 - elasticity);
            return Tween.PunchLocalPosition(target.transform, shakeSettings);
        }

        public static Tween DOShakeRotation([NotNull] this Component target, float duration, float strength, int vibrato = 10, float randomness = 90, bool fadeOut = true)
            => DOShakeRotation(target, duration, Vector3.one * strength, vibrato, randomness, fadeOut);
        public static Tween DOShakeRotation([NotNull] this Component target, float duration, Vector3 strength, int vibrato = 10, float randomness = 90, bool fadeOut = true) {
            if (Math.Abs(randomness - 90f) > 0.001f) {
                Debug.LogWarning("PrimeTween doesn't support " + nameof(randomness));
            }
            var settings = new ShakeSettings(strength, duration, vibrato);
            if (fadeOut) {
                settings.enableFalloff = true;
                settings.frequency = remapFrequency(settings.frequency);
            }
            return Tween.ShakeLocalRotation(target.transform, settings);
        }
        public static Tween DOPunchRotation([NotNull] this Component target, Vector3 punch, float duration, int vibrato = 10, float elasticity = 1) {
            var shakeSettings = new ShakeSettings(punch, duration, remapFrequency(vibrato), asymmetryFactor: 1 - elasticity);
            return Tween.PunchLocalRotation(target.transform, shakeSettings);
        }

        public static Tween DOShakeScale([NotNull] this Component target, float duration, float strength, int vibrato = 10, float randomness = 90, bool fadeOut = true)
            => DOShakeScale(target, duration, Vector3.one * strength, vibrato, randomness, fadeOut);
        public static Tween DOShakeScale([NotNull] this Component target, float duration, Vector3 strength, int vibrato = 10, float randomness = 90, bool fadeOut = true) {
            if (Math.Abs(randomness - 90f) > 0.001f) {
                Debug.LogWarning("PrimeTween doesn't support " + nameof(randomness));
            }
            var settings = new ShakeSettings(strength, duration, vibrato);
            if (fadeOut) {
                settings.enableFalloff = true;
                settings.frequency = remapFrequency(settings.frequency);
            }
            return Tween.ShakeScale(target.transform, settings);
        }
        public static Tween DOPunchScale([NotNull] this Component target, Vector3 punch, float duration, int vibrato = 10, float elasticity = 1) {
            var shakeSettings = new ShakeSettings(punch, duration, remapFrequency(vibrato), asymmetryFactor: 1 - elasticity);
            return Tween.PunchScale(target.transform, shakeSettings);
        }

        public static Tween DORotate([NotNull] this Transform target, Vector3 endValue, float duration) {
            return Tween.Rotation(target, Quaternion.Euler(endValue), duration);
        }

        public static Tween DOLocalRotate([NotNull] this Transform target, Vector3 endValue, float duration) {
            return Tween.LocalRotation(target, Quaternion.Euler(endValue), duration);
        }

        public static Tween DOScale([NotNull] this Transform target, Single endValue, float duration) {
            return Tween.Scale(target, endValue, duration);
        }

        public static int DOKill([NotNull] this Component target, bool complete = false) => doKill_internal(target, complete);
        public static int DOKill([NotNull] this Material target, bool complete = false) => doKill_internal(target, complete);

        internal static int doKill_internal([CanBeNull] object target, bool complete = false) {
            bool prevLogCantManipulateError = PrimeTweenManager.logCantManipulateError;
            PrimeTweenManager.logCantManipulateError = false;
            var result = complete ? Tween.CompleteAll(target) : Tween.StopAll(target);
            PrimeTweenManager.logCantManipulateError = prevLogCantManipulateError;
            return result;
        }

        internal static Easing GetEasing(Ease ease, float? maybeStrength, float? maybePeriod) {
            var strength = maybeStrength ?? 1;
            switch (ease) {
                case Ease.OutBack:
                    if (maybePeriod.HasValue) {
                        Debug.LogWarning("Ease.OutBack doesn't support custom period.");
                    }
                    return Easing.Overshoot(strength / StandardEasing.backEaseConst);
                case Ease.OutBounce:
                    return Easing.Bounce(strength);
                case Ease.OutElastic:
                    return Easing.Elastic(strength, maybePeriod ?? StandardEasing.defaultElasticEasePeriod);
            }
            return Easing.Standard(ease);
        }

        // public static Tween DOTWEEN_METHOD_NAME([NotNull] this UnityEngine.Camera target, Single endValue, float duration) => Tween.METHOD_NAME(target, endValue, duration);
    }

    public static class DOTween {
        public static Ease defaultEaseType {
            get => PrimeTweenConfig.defaultEase;
            set => PrimeTweenConfig.defaultEase = value;
        }

        public static Sequence Sequence() => PrimeTween.Sequence.Create();

        public static void Kill([NotNull] object target, bool complete = false) => DOTweenAdapter.doKill_internal(target, complete);

        public static void KillAll(bool complete = false) => DOTweenAdapter.doKill_internal(null, complete);

        public static Tween To([NotNull] Func<float> getter, [NotNull] Action<float> setter, float endValue, float duration) => Tween.Custom(getter(), endValue, duration, val => setter(val));
        public static Tween To([NotNull] Func<Vector2> getter, [NotNull] Action<Vector2> setter, Vector2 endValue, float duration) => Tween.Custom(getter(), endValue, duration, val => setter(val));
        public static Tween To([NotNull] Func<Vector3> getter, [NotNull] Action<Vector3> setter, Vector3 endValue, float duration) => Tween.Custom(getter(), endValue, duration, val => setter(val));
        public static Tween To([NotNull] Func<Vector4> getter, [NotNull] Action<Vector4> setter, Vector4 endValue, float duration) => Tween.Custom(getter(), endValue, duration, val => setter(val));
        public static Tween To([NotNull] Func<Quaternion> getter, [NotNull] Action<Quaternion> setter, Quaternion endValue, float duration) => Tween.Custom(getter(), endValue, duration, val => setter(val));
        public static Tween To([NotNull] Func<Color> getter, [NotNull] Action<Color> setter, Color endValue, float duration) => Tween.Custom(getter(), endValue, duration, val => setter(val));
        public static Tween To([NotNull] Func<Rect> getter, [NotNull] Action<Rect> setter, Rect endValue, float duration) => Tween.Custom(getter(), endValue, duration, val => setter(val));
    }

    public static class DOVirtual {
        public static Tween DelayedCall(float delay, Action callback, bool ignoreTimeScale = true)
            => Tween.Delay(delay, callback, ignoreTimeScale);

        public static Tween Float(float startValue, float endValue, float duration, Action<float> onValueChange) => Tween.Custom(startValue, endValue, duration, onValueChange);
        public static Tween Vector3(Vector3 startValue, Vector3 endValue, float duration, Action<Vector3> onValueChange) => Tween.Custom(startValue, endValue, duration, onValueChange);
        public static Tween Color(Color startValue, Color endValue, float duration, Action<Color> onValueChange) => Tween.Custom(startValue, endValue, duration, onValueChange);

        public static float EasedValue(float from, float to, float lifetimePercentage, Ease easeType, float? amplitude = null, float? period = null)
            => Mathf.LerpUnclamped(from, to, DOTweenAdapter.GetEasing(easeType, amplitude, period).Evaluate(lifetimePercentage));
        public static float EasedValue(float from, float to, float lifetimePercentage, [NotNull] AnimationCurve easeCurve)
            => Mathf.LerpUnclamped(from, to, Easing.Curve(easeCurve).Evaluate(lifetimePercentage));
        public static Vector3 EasedValue(Vector3 from, Vector3 to, float lifetimePercentage, Ease easeType, float? amplitude = null, float? period = null)
            => UnityEngine.Vector3.LerpUnclamped(from, to, DOTweenAdapter.GetEasing(easeType, amplitude, period).Evaluate(lifetimePercentage));
        public static Vector3 EasedValue(Vector3 from, Vector3 to, float lifetimePercentage, [NotNull] AnimationCurve easeCurve)
            => UnityEngine.Vector3.LerpUnclamped(from, to, Easing.Curve(easeCurve).Evaluate(lifetimePercentage));
    }

    public partial struct Sequence {
        public Sequence AppendCallback([NotNull] Action callback) {
            return ChainCallback(callback);
        }

        public Sequence SetLoops(int loops, LoopType? loopType = null) {
            root.SetLoops(loops, loopType);
            return this;
        }

        public Sequence Join(Sequence other) => Group(other);

        public Sequence Join(Tween other) {
            ref var otherData = ref other.tween.data;
            var startDelay = otherData.startDelay;
            if (startDelay > 0) {
                // For some weird reason, DG.Tweening.Sequence.DoInsert shifts the lastTweenInsertTime by a tween's delay.
                otherData.startDelay = 0;
                float endDelay = otherData.cycleDuration - startDelay - otherData.animationDuration;
                TweenData.CalculateCycleDuration(endDelay, ref otherData);
                Group(Tween.Delay(startDelay));
                ChainLast(other);
                return this;
            }
            return Group(other);
        }

        /// <summary>Schedules <see cref="other"/> after the last added tween.
        /// Internal because this API is hard to understand, but needed for adapter.</summary>
        internal Sequence ChainLast(Tween other) {
            if (TryManipulate()) {
                Insert(GetLastInSelfOrRoot().data.calcDurationWithWaitDependencies(), other);
            }
            return this;
        }

        public Sequence Append(Sequence other) => Chain(other);

        public Sequence Append(Tween other) {
            ref var otherData = ref other.tween.data;
            var startDelay = otherData.startDelay;
            if (startDelay > 0) {
                // For some weird reason, DG.Tweening.Sequence.DoInsert shifts the lastTweenInsertTime by a tween's delay.
                otherData.startDelay = 0;
                float endDelay = otherData.cycleDuration - startDelay - otherData.animationDuration;
                TweenData.CalculateCycleDuration(endDelay, ref otherData);
                Chain(Tween.Delay(startDelay));
            }
            return Chain(other);
        }

        public Sequence AppendInterval(float delay) {
            return Chain(Tween.Delay(delay));
        }

        public void Kill(bool complete = false) {
            if (complete) {
                Complete();
            } else {
                Stop();
            }
        }

        public void Complete(bool withCallbacks) {
            if (withCallbacks) {
                Debug.LogWarning("PrimeTween doesn't support " + nameof(Sequence) + "." + nameof(Complete) + "() " + nameof(withCallbacks) + " == true");
            }
            Complete();
        }

        public Sequence SetEase(Ease ease, float? amplitude = null, float? period = null) {
            root.SetEase(ease, amplitude, period);
            return this;
        }

        public Sequence SetEase([NotNull] AnimationCurve animCurve) {
            root.SetEase(animCurve);
            return this;
        }

        public Sequence SetDelay(float delay) {
            return PrependInterval(delay);
        }

        public Sequence OnStepComplete([NotNull] Action action) {
            Debug.LogWarning("Please use sequence.ChainCallback() as the last operation instead of sequence.OnStepComplete()");
            return ChainCallback(action);
        }

        public Sequence PrependInterval(float interval) {
            if (!ValidateCanManipulateSequence()) {
                return this;
            }
            foreach (var t in GetSelfChildren()) {
                t.data.waitDelay += interval;
            }
            duration += interval;
            return this;
        }

        public Sequence SetUpdate(bool isIndependentUpdate) {
            Assert.IsTrue(isAlive);
            Assert.IsTrue(root.tween.data.IsMainSequenceRoot());
            root.tween.data.useUnscaledTime = isIndependentUpdate;
            return this;
        }

        public Sequence AsyncWaitForCompletion() => this;

        public IEnumerator WaitForCompletion() => ToYieldInstruction();

        /// <summary>It's safe to destroy objects with running animations in PrimeTween, so this adapter method does nothing. More info: https://github.com/KyryloKuzyk/PrimeTween/discussions/4</summary>
        [PublicAPI]
        public Sequence SetLink(GameObject gameObject) => this;

        public Sequence Pause() {
            isPaused = true;
            return this;
        }

        public Sequence Play() {
            isPaused = false;
            return this;
        }
    }

    internal partial class ColdData {
        internal void SetEasing(Easing easing) {
            data.ease = easing.ease == Ease.Default ? PrimeTweenManager.Instance.defaultEase : easing.ease;
            customEase = easing.curve;
            parametricEase = easing.parametricEase;
            parametricEaseStrength = easing.parametricEaseStrength;
            parametricEasePeriod = easing.parametricEasePeriod;
        }
    }

    public partial struct Tween {
        public Tween SetEase(Ease ease, float? amplitude = null, float? period = null) {
            Assert.IsTrue(isAlive);
            var parametricEasing = DOTweenAdapter.GetEasing(ease, amplitude, period);
            tween.SetEasing(parametricEasing);
            return this;
        }

        public Tween SetDelay(float delay) {
            Assert.IsTrue(isAlive);
            ref var d = ref tween.data;
            Assert.IsFalse(d.isInSequence);
            float endDelay = d.cycleDuration - d.startDelay - d.animationDuration;
            d.startDelay = delay;
            TweenData.CalculateCycleDuration(endDelay, ref d);
            return this;
        }

        public Tween SetRelative(bool isRelative = true) {
            Assert.IsTrue(isAlive);
            if (!isRelative) {
                return this;
            }
            ref var rt = ref tween.managedData;
            ref var d = ref tween.data;

            TweenAnimation.ValueWrapper current;
            try {
                current = Utils.GetAnimatedValue(rt.target, d.tweenType, rt.cold.longParam);
            } catch {
                Debug.LogError($"{tween.data.tweenType} doesn't support 'SetRelative()'.");
                return this;
            }

            rt.endValueOrDiff = CalculateRelative(d, current, rt.endValueOrDiff);
            /*var getter = tween.getter;
                // p2 todo this doesn't account for double val
                if (tween.propType == PropType.Quaternion) {
                    if (getter != null) {
                        tween.endValue.QuaternionVal *= getter(tween).QuaternionVal;
                    } else {
                        tween.endValue.QuaternionVal *= tween.startValue.QuaternionVal;
                    }
                } else {
                    if (getter != null) {
                        tween.endValue.Vector4Val += getter(tween).Vector4Val;
                    } else {
                        tween.endValue.Vector4Val += tween.startValue.Vector4Val;
                    }
                }*/
            return this;
        }

        public Tween SetLoops(int loops, LoopType? loopType = null) {
            SetRemainingCycles(loops);
            if (isAlive && loopType.HasValue) {
                tween.data.cycleMode = toCycleMode(loopType.Value);
            }
            return this;
        }

        static CycleMode toCycleMode(LoopType t) {
            switch (t) {
                case LoopType.Restart:
                    return CycleMode.Restart;
                case LoopType.Yoyo:
                    // yoyo in dotween behaves like rewind. But yoyo in other tween libraries (like tween.js) preserves the normal ease
                    return CycleMode.Rewind;
                case LoopType.Incremental:
                    return CycleMode.Incremental;
                default:
                    throw new Exception();
            }
        }

        public void Kill(bool complete = false) {
            if (complete) {
                Complete();
            } else {
                Stop();
            }
        }

        public bool IsActive() => isAlive;
        public bool active => isAlive;
        public bool IsPlaying() => isAlive && !isPaused;

        public Tween Pause() {
            var copy = this;
            copy.isPaused = true;
            return this;
        }

        public Tween Play() {
            var copy = this;
            copy.isPaused = false;
            return this;
        }

        public float Elapsed(bool includeLoops = true) => includeLoops ? elapsedTimeTotal : elapsedTime;
        public float Duration(bool includeLoops = true) => includeLoops ? durationTotal : duration;
        public int Loops() => cyclesTotal;
        public int CompletedLoops() => cyclesDone;
        public float ElapsedDelay() => isAlive ? Mathf.Clamp(elapsedTime, 0f, tween.data.startDelay) : 0;
        public float ElapsedPercentage(bool includeLoops = true) => includeLoops ? progressTotal : progress;
        public void TogglePause() {
            var copy = this;
            copy.isPaused = !isPaused;
        }

        public Tween SetEase([NotNull] AnimationCurve animCurve) {
            Assert.IsTrue(isAlive);
            tween.SetEasing(Easing.Curve(animCurve));
            return this;
        }

        public Tween SetTarget([NotNull] object target) {
            Assert.IsNotNull(target);
            Assert.IsTrue(isAlive);
            tween.managedData.target = target;
            return this;
        }

        public IEnumerator WaitForCompletion() => ToYieldInstruction();

        public Tween AsyncWaitForCompletion() => this;

        public Tween SetUpdate(bool isIndependentUpdate) {
            Assert.IsTrue(isAlive);
            tween.data.useUnscaledTime = isIndependentUpdate;
            return this;
        }

        public Tween From() => setFrom(true, false);
        public Tween From(bool isRelative) => setFrom(true, isRelative);
        public Tween From(bool setImmediately, bool isRelative) => setFrom(setImmediately, isRelative);

        public Tween From(float fromValue, bool setImmediately = true, bool isRelative = false) => setFrom(setImmediately, isRelative, fromValue.ToContainer(), PropType.Float);
        public Tween From(Color fromValue, bool setImmediately = true, bool isRelative = false) => setFrom(setImmediately, isRelative, fromValue.ToContainer(), PropType.Color);
        public Tween From(Vector2 fromValue, bool setImmediately = true, bool isRelative = false) => setFrom(setImmediately, isRelative, fromValue.ToContainer(), PropType.Vector2);
        public Tween From(Vector3 fromValue, bool setImmediately = true, bool isRelative = false) => setFrom(setImmediately, isRelative, fromValue.ToContainer(), PropType.Vector3);
        public Tween From(Vector4 fromValue, bool setImmediately = true, bool isRelative = false) => setFrom(setImmediately, isRelative, fromValue.ToContainer(), PropType.Vector4);
        public Tween From(Quaternion fromValue, bool setImmediately = true, bool isRelative = false) => setFrom(setImmediately, isRelative, fromValue.ToContainer(), PropType.Quaternion);
        public Tween From(Rect fromValue, bool setImmediately = true, bool isRelative = false) => setFrom(setImmediately, isRelative, fromValue.ToContainer(), PropType.Rect);

        static TweenAnimation.ValueWrapper CalculateRelative(UnmanagedTweenData data, TweenAnimation.ValueWrapper current, TweenAnimation.ValueWrapper diff) {
            switch (Utils.TweenTypeToTweenData(data.tweenType).Item1) {
                case PropType.Quaternion:
                    return (current.quaternion * diff.quaternion).ToContainer();
                case PropType.Double:
                    return (current.DoubleVal + diff.DoubleVal).ToContainer();
                default:
                    return (current.vector4 + diff.vector4).ToContainer();
            }
        }

        Tween setFrom(bool setImmediately, bool isRelative, TweenAnimation.ValueWrapper? fromValue = null, PropType propType = PropType.None) {
            if (!TryManipulate()) {
                return this;
            }
            if (elapsedTimeTotal != 0f) {
                Debug.LogError(Constants.animationAlreadyStarted);
                return this;
            }
            ref var rt = ref tween.managedData;
            ref var d = ref tween.data;
            if (rt.IsUnityTargetDestroyed()) {
                Debug.LogError("Tween's target has been destroyed.");
                return this;
            }

            TweenAnimation.ValueWrapper current;
            try {
                current = Utils.GetAnimatedValue(rt.target, d.tweenType, rt.cold.longParam);
            } catch {
                Debug.LogError($"{tween.data.tweenType} doesn't support 'From()'.");
                return this;
            }

            if (isRelative) {
                rt.endValueOrDiff = CalculateRelative(d, current, rt.endValueOrDiff);
            }
            if (fromValue.HasValue) {
                if (d.propType != propType) {
                    Debug.LogError($"Animated value is {d.propType}, but '{nameof(From)}()' was called with {propType}. Please provide a correct type.");
                    return this;
                }
                d.startFromCurrent = false;
                d.startValue = isRelative ? CalculateRelative(d, current, fromValue.Value) : fromValue.Value;
            } else {
                d.startFromCurrent = false;
                d.startValue = rt.endValueOrDiff;
                rt.endValueOrDiff = current;
            }
            TweenData.CacheDiff(ref d, ref rt);
            if (setImmediately) {
                d.easedInterpolationFactor = d.calcEasedT(0f);
                rt.ReportOnValueChange(ref d);
            }
            return this;
        }

        [PublicAPI]
        public Tween SetLink(GameObject gameObject) {
            return this;
        }
    }

    public enum LoopType {
        Restart,
        Yoyo,
        Incremental
    }
}
#endif
