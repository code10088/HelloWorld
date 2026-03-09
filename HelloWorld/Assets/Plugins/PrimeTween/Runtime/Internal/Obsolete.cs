// ReSharper disable UnusedMember.Global
// ReSharper disable PossibleNullReferenceException
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedParameter.Global
using System;
using System.ComponentModel;
using JetBrains.Annotations;
using UnityEngine;
using TweenType = PrimeTween.TweenAnimation.TweenType;

namespace PrimeTween {
    public partial class PrimeTweenConfig {
        // ReSharper disable once ValueParameterNotUsed
        [EditorBrowsable(EditorBrowsableState.Never)] [Obsolete("this setting is replaced with the 'warnIfTargetDestroyed' parameter that you can pass to the tween.OnComplete(), Tween.Delay() and Sequence.ChainDelay() methods.")]
        public static bool warnDestroyedTweenHasOnComplete { set {} }
    }

    internal static class ObsoleteMessages {
        /// Obsolete on 2023-08-29
        internal const string obsoleteIsAliveMessage = "please use 'isAlive' (lower case 'i') instead.";
        /// Obsolete on 2023-08-29
        internal const string obsoleteIsPausedMessage = "please use 'isPaused' (lower case 'i') instead.";
        internal const string obsoleteTweenSetCycles = "SetCycles() was renamed to SetRemainingCycles().";

        /// Obsolete on 2025-11-15
        internal const string tweenSettingsOverloadEndValue = "Use the overload with TweenSettings<T> instead.\n\n" +
                                                              "For example, instead of:\n" +
                                                              "Tween.Position(transform, endValue, tweenSettings);\n\n" +
                                                              "Use this:\n" +
                                                              "Tween.Position(transform, new TweenSettings<Vector3>(endValue, tweenSettings));\n" +
                                                              "Tween.Position(transform, new (endValue, tweenSettings)); // shorter syntax";
        internal const string tweenSettingsOverloadStartEndValue = "Use the overload with TweenSettings<T> instead.\n\n" +
                                                                   "For example, instead of:\n" +
                                                                   "Tween.Position(transform, startValue, endValue, tweenSettings);\n\n" +
                                                                   "Use this:\n" +
                                                                   "Tween.Position(transform, new TweenSettings<Vector3>(startValue, endValue, tweenSettings));\n" +
                                                                   "Tween.Position(transform, new (startValue, endValue, tweenSettings)); // shorter syntax";
        internal const string resetOnCompleteRenamed = "This method was renamed to '" + nameof(Tween.ResetOnCompletion) + "'.";
    }

    public partial struct Tween {
        [EditorBrowsable(EditorBrowsableState.Never)] [Obsolete(ObsoleteMessages.obsoleteTweenSetCycles)]
        public void SetCycles(bool stopAtEndValue) => SetRemainingCycles(stopAtEndValue);
        [EditorBrowsable(EditorBrowsableState.Never)] [Obsolete(ObsoleteMessages.obsoleteTweenSetCycles)]
        public void SetCycles(int cycles) => SetRemainingCycles(cycles);

        /// Obsolete on 2023-11-24
        const string minMaxExpected = "numMinExpected/numMaxExpected parameters are no longer supported.";
        [EditorBrowsable(EditorBrowsableState.Never)] [Obsolete(minMaxExpected)]
        public static int StopAll([CanBeNull] object onTarget, int? numMinExpected, int? numMaxExpected = null) => StopAll(onTarget);
        [EditorBrowsable(EditorBrowsableState.Never)] [Obsolete(minMaxExpected)]
        public static int CompleteAll([CanBeNull] object onTarget, int? numMinExpected, int? numMaxExpected = null) => CompleteAll(onTarget);
        [EditorBrowsable(EditorBrowsableState.Never)] [Obsolete(minMaxExpected)]
        public static int SetPausedAll(bool isPaused, [CanBeNull] object onTarget, int? numMinExpected, int? numMaxExpected = null) => SetPausedAll(isPaused, onTarget);

        [EditorBrowsable(EditorBrowsableState.Never)] [Obsolete(ObsoleteMessages.obsoleteIsAliveMessage)]
        public bool IsAlive => isAlive;
        [EditorBrowsable(EditorBrowsableState.Never)] [Obsolete(ObsoleteMessages.obsoleteIsPausedMessage)]
        public bool IsPaused => isPaused;

        /// Obsolete on 2023-08-30
        const string localScaleRenamed = "please use 'Scale' instead of 'LocalScale'.";

        [EditorBrowsable(EditorBrowsableState.Never)] [Obsolete(localScaleRenamed)]
        public static Tween LocalScale([NotNull] Transform target, Vector3 endValue, float duration, Easing ease = default, int cycles = 1, CycleMode cycleMode = CycleMode.Restart, float startDelay = 0, float endDelay = 0, bool useUnscaledTime = false)
            => LocalScale(target, new TweenSettings<Vector3>(endValue, new TweenSettings(duration, ease, cycles, cycleMode, startDelay, endDelay, useUnscaledTime)));
        [EditorBrowsable(EditorBrowsableState.Never)] [Obsolete(localScaleRenamed)]
        public static Tween LocalScale([NotNull] Transform target, Vector3 startValue, Vector3 endValue, float duration, Easing ease = default, int cycles = 1, CycleMode cycleMode = CycleMode.Restart, float startDelay = 0, float endDelay = 0, bool useUnscaledTime = false)
            => LocalScale(target, new TweenSettings<Vector3>(startValue, endValue, new TweenSettings(duration, ease, cycles, cycleMode, startDelay, endDelay, useUnscaledTime)));
        [EditorBrowsable(EditorBrowsableState.Never)] [Obsolete(localScaleRenamed)]
        public static Tween LocalScale([NotNull] Transform target, Vector3 endValue, TweenSettings settings) => LocalScale(target, new TweenSettings<Vector3>(endValue, settings));
        [EditorBrowsable(EditorBrowsableState.Never)] [Obsolete(localScaleRenamed)]
        public static Tween LocalScale([NotNull] Transform target, Vector3 startValue, Vector3 endValue, TweenSettings settings) => LocalScale(target, new TweenSettings<Vector3>(startValue, endValue, settings));
        [EditorBrowsable(EditorBrowsableState.Never)] [Obsolete(localScaleRenamed)]
        public static Tween LocalScale([NotNull] Transform target, TweenSettings<Vector3> settings) {
            return animate(target, ref settings, TweenType.Scale);
        }

        [EditorBrowsable(EditorBrowsableState.Never)] [Obsolete(localScaleRenamed)]
        public static Tween LocalScaleX([NotNull] Transform target, Single endValue, float duration, Easing ease = default, int cycles = 1, CycleMode cycleMode = CycleMode.Restart, float startDelay = 0, float endDelay = 0, bool useUnscaledTime = false)
            => LocalScaleX(target, new TweenSettings<float>(endValue, new TweenSettings(duration, ease, cycles, cycleMode, startDelay, endDelay, useUnscaledTime)));
        [EditorBrowsable(EditorBrowsableState.Never)] [Obsolete(localScaleRenamed)]
        public static Tween LocalScaleX([NotNull] Transform target, Single startValue, Single endValue, float duration, Easing ease = default, int cycles = 1, CycleMode cycleMode = CycleMode.Restart, float startDelay = 0, float endDelay = 0, bool useUnscaledTime = false)
            => LocalScaleX(target, new TweenSettings<float>(startValue, endValue, new TweenSettings(duration, ease, cycles, cycleMode, startDelay, endDelay, useUnscaledTime)));
        [EditorBrowsable(EditorBrowsableState.Never)] [Obsolete(localScaleRenamed)]
        public static Tween LocalScaleX([NotNull] Transform target, Single endValue, TweenSettings settings) => LocalScaleX(target, new TweenSettings<float>(endValue, settings));
        [EditorBrowsable(EditorBrowsableState.Never)] [Obsolete(localScaleRenamed)]
        public static Tween LocalScaleX([NotNull] Transform target, Single startValue, Single endValue, TweenSettings settings) => LocalScaleX(target, new TweenSettings<float>(startValue, endValue, settings));
        [EditorBrowsable(EditorBrowsableState.Never)] [Obsolete(localScaleRenamed)]
        public static Tween LocalScaleX([NotNull] Transform target, TweenSettings<float> settings)
            => animate(target, ref settings, TweenType.ScaleX);

        [EditorBrowsable(EditorBrowsableState.Never)] [Obsolete(localScaleRenamed)]
        public static Tween LocalScaleY([NotNull] Transform target, Single endValue, float duration, Easing ease = default, int cycles = 1, CycleMode cycleMode = CycleMode.Restart, float startDelay = 0, float endDelay = 0, bool useUnscaledTime = false)
            => LocalScaleY(target, new TweenSettings<float>(endValue, new TweenSettings(duration, ease, cycles, cycleMode, startDelay, endDelay, useUnscaledTime)));
        [EditorBrowsable(EditorBrowsableState.Never)] [Obsolete(localScaleRenamed)]
        public static Tween LocalScaleY([NotNull] Transform target, Single startValue, Single endValue, float duration, Easing ease = default, int cycles = 1, CycleMode cycleMode = CycleMode.Restart, float startDelay = 0, float endDelay = 0, bool useUnscaledTime = false)
            => LocalScaleY(target, new TweenSettings<float>(startValue, endValue, new TweenSettings(duration, ease, cycles, cycleMode, startDelay, endDelay, useUnscaledTime)));
        [EditorBrowsable(EditorBrowsableState.Never)] [Obsolete(localScaleRenamed)]
        public static Tween LocalScaleY([NotNull] Transform target, Single endValue, TweenSettings settings) => LocalScaleY(target, new TweenSettings<float>(endValue, settings));
        [EditorBrowsable(EditorBrowsableState.Never)] [Obsolete(localScaleRenamed)]
        public static Tween LocalScaleY([NotNull] Transform target, Single startValue, Single endValue, TweenSettings settings) => LocalScaleY(target, new TweenSettings<float>(startValue, endValue, settings));
        [EditorBrowsable(EditorBrowsableState.Never)] [Obsolete(localScaleRenamed)]
        public static Tween LocalScaleY([NotNull] Transform target, TweenSettings<float> settings)
            => animate(target, ref settings, TweenType.ScaleY);

        [EditorBrowsable(EditorBrowsableState.Never)] [Obsolete(localScaleRenamed)]
        public static Tween LocalScaleZ([NotNull] Transform target, Single endValue, float duration, Easing ease = default, int cycles = 1, CycleMode cycleMode = CycleMode.Restart, float startDelay = 0, float endDelay = 0, bool useUnscaledTime = false)
            => LocalScaleZ(target, new TweenSettings<float>(endValue, new TweenSettings(duration, ease, cycles, cycleMode, startDelay, endDelay, useUnscaledTime)));
        [EditorBrowsable(EditorBrowsableState.Never)] [Obsolete(localScaleRenamed)]
        public static Tween LocalScaleZ([NotNull] Transform target, Single startValue, Single endValue, float duration, Easing ease = default, int cycles = 1, CycleMode cycleMode = CycleMode.Restart, float startDelay = 0, float endDelay = 0, bool useUnscaledTime = false)
            => LocalScaleZ(target, new TweenSettings<float>(startValue, endValue, new TweenSettings(duration, ease, cycles, cycleMode, startDelay, endDelay, useUnscaledTime)));
        [EditorBrowsable(EditorBrowsableState.Never)] [Obsolete(localScaleRenamed)]
        public static Tween LocalScaleZ([NotNull] Transform target, Single endValue, TweenSettings settings) => LocalScaleZ(target, new TweenSettings<float>(endValue, settings));
        [EditorBrowsable(EditorBrowsableState.Never)] [Obsolete(localScaleRenamed)]
        public static Tween LocalScaleZ([NotNull] Transform target, Single startValue, Single endValue, TweenSettings settings) => LocalScaleZ(target, new TweenSettings<float>(startValue, endValue, settings));
        [EditorBrowsable(EditorBrowsableState.Never)] [Obsolete(localScaleRenamed)]
        public static Tween LocalScaleZ([NotNull] Transform target, TweenSettings<float> settings)
            => animate(target, ref settings, TweenType.ScaleZ);

        [EditorBrowsable(EditorBrowsableState.Never)] [Obsolete(localScaleRenamed)]
        public static Tween LocalScale([NotNull] Transform target, Single endValue, float duration, Easing ease = default, int cycles = 1, CycleMode cycleMode = CycleMode.Restart, float startDelay = 0, float endDelay = 0, bool useUnscaledTime = false)
            => LocalScale(target, new TweenSettings<float>(endValue, new TweenSettings(duration, ease, cycles, cycleMode, startDelay, endDelay, useUnscaledTime)));
        [EditorBrowsable(EditorBrowsableState.Never)] [Obsolete(localScaleRenamed)]
        public static Tween LocalScale([NotNull] Transform target, Single startValue, Single endValue, float duration, Easing ease = default, int cycles = 1, CycleMode cycleMode = CycleMode.Restart, float startDelay = 0, float endDelay = 0, bool useUnscaledTime = false)
            => LocalScale(target, new TweenSettings<float>(startValue, endValue, new TweenSettings(duration, ease, cycles, cycleMode, startDelay, endDelay, useUnscaledTime)));
        [EditorBrowsable(EditorBrowsableState.Never)] [Obsolete(localScaleRenamed)]
        public static Tween LocalScale([NotNull] Transform target, Single endValue, TweenSettings settings) => LocalScale(target, new TweenSettings<float>(endValue, settings));
        [EditorBrowsable(EditorBrowsableState.Never)] [Obsolete(localScaleRenamed)]
        public static Tween LocalScale([NotNull] Transform target, Single startValue, Single endValue, TweenSettings settings) => LocalScale(target, new TweenSettings<float>(startValue, endValue, settings));
        [EditorBrowsable(EditorBrowsableState.Never)] [Obsolete(localScaleRenamed)]
        public static Tween LocalScale([NotNull] Transform target, TweenSettings<float> uniformScaleSettings) => Scale(target, uniformScaleSettings);

        [EditorBrowsable(EditorBrowsableState.Never)] [Obsolete(localScaleRenamed)]
        public static Tween ShakeLocalScale([NotNull] Transform target, Vector3 strength, float duration, float frequency = ShakeSettings.defaultFrequency, bool enableFalloff = true, Ease easeBetweenShakes = Ease.Default, float asymmetryFactor = 0f, int cycles = 1,
            float startDelay = 0, float endDelay = 0, bool useUnscaledTime = PrimeTweenConfig.defaultUseUnscaledTimeForShakes)
            => ShakeScale(target, new ShakeSettings(strength, duration, frequency, enableFalloff, easeBetweenShakes, asymmetryFactor, cycles, startDelay, endDelay, useUnscaledTime));
        [EditorBrowsable(EditorBrowsableState.Never)] [Obsolete(localScaleRenamed)]
        public static Tween ShakeLocalScale([NotNull] Transform target, ShakeSettings settings) => ShakeScale(target, settings);
        [EditorBrowsable(EditorBrowsableState.Never)] [Obsolete(localScaleRenamed)]
        public static Tween PunchLocalScale([NotNull] Transform target, Vector3 strength, float duration, float frequency = ShakeSettings.defaultFrequency, bool enableFalloff = true, Ease easeBetweenShakes = Ease.Default, float asymmetryFactor = 0f, int cycles = 1,
            float startDelay = 0, float endDelay = 0, bool useUnscaledTime = PrimeTweenConfig.defaultUseUnscaledTimeForShakes)
            => PunchScale(target, new ShakeSettings(strength, duration, frequency, enableFalloff, easeBetweenShakes, asymmetryFactor, cycles, startDelay, endDelay, useUnscaledTime));
        [EditorBrowsable(EditorBrowsableState.Never)] [Obsolete(localScaleRenamed)]
        public static Tween PunchLocalScale([NotNull] Transform target, ShakeSettings settings) => ShakeScale(target, settings.WithPunch());
    }

    public partial struct Sequence {
        /// Obsolete on 2023-11-28
        [EditorBrowsable(EditorBrowsableState.Never)] [Obsolete("please use Sequence.Create(cycles: numCycles) instead. Or use SetRemainingCycles() to modify the number of remaining cycles when the Sequence is already running.")]
        public Sequence SetCycles(int cycles) {
            SetRemainingCycles(cycles);
            return this;
        }

        [EditorBrowsable(EditorBrowsableState.Never)] [Obsolete(ObsoleteMessages.obsoleteIsAliveMessage)]
        public bool IsAlive => isAlive;
        [EditorBrowsable(EditorBrowsableState.Never)] [Obsolete(ObsoleteMessages.obsoleteIsPausedMessage)]
        public bool IsPaused => isPaused;

        const string chainAndInsertCallbackMessage = "The behavior of ChainCallback() and InsertCallback() methods was fixed in version 1.2.0. " +
                                                     "Use their obsolete counterparts only if you need to preserve the old incorrect behaviour in the existing project.\n" +
                                                     "More info: https://github.com/KyryloKuzyk/PrimeTween/discussions/112\n\n";
        [EditorBrowsable(EditorBrowsableState.Never)] [Obsolete(chainAndInsertCallbackMessage)]
        public Sequence ChainCallbackObsolete([NotNull] Action callback, bool? warnIfTargetDestroyed = null) {
            if (TryManipulate()) {
                InsertCallbackObsolete(duration, callback, warnIfTargetDestroyed);
            }
            return this;
        }
        [EditorBrowsable(EditorBrowsableState.Never)] [Obsolete(chainAndInsertCallbackMessage)]
        public Sequence InsertCallbackObsolete(float atTime, Action callback, bool? warnIfTargetDestroyed = null) {
            if (!TryManipulate()) {
                return this;
            }
            var delay = PrimeTweenManager.DelayWithoutDurationCheck(PrimeTweenManager.dummyTarget, atTime, false);
            Assert.IsTrue(delay.HasValue);
            delay.Value.tween.managedData.OnComplete(callback, warnIfTargetDestroyed);
            return Insert(0f, delay.Value);
        }
        [EditorBrowsable(EditorBrowsableState.Never)] [Obsolete(chainAndInsertCallbackMessage)]
        public Sequence ChainCallbackObsolete<T>([NotNull] T target, [NotNull] Action<T> callback, bool? warnIfTargetDestroyed = null) where T: class {
            if (TryManipulate()) {
                InsertCallbackObsolete(duration, target, callback, warnIfTargetDestroyed);
            }
            return this;
        }
        [EditorBrowsable(EditorBrowsableState.Never)] [Obsolete(chainAndInsertCallbackMessage)]
        public Sequence InsertCallbackObsolete<T>(float atTime, [NotNull] T target, Action<T> callback, bool? warnIfTargetDestroyed = null) where T: class {
            if (!TryManipulate()) {
                return this;
            }
            var delay = PrimeTweenManager.DelayWithoutDurationCheck(target, atTime, false);
            if (!delay.HasValue) {
                return this;
            }
            delay.Value.tween.managedData.OnComplete(target, callback, warnIfTargetDestroyed);
            return Insert(0f, delay.Value);
        }

        /// Obsolete on 2025-09-16
        [EditorBrowsable(EditorBrowsableState.Never)] [Obsolete("use '" + nameof(Sequence) + "." + nameof(SequenceCycleMode) + "' instead.")]
        public static Sequence Create(int cycles, CycleMode cycleMode, Ease sequenceEase = Ease.Linear, bool useUnscaledTime = false, UpdateType updateType = default) {
            return Create(cycles, (SequenceCycleMode)cycleMode, sequenceEase, useUnscaledTime, updateType);
        }
    }
}
