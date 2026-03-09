// ReSharper disable CompareOfFloatsByEqualityOperator
#if PRIME_TWEEN_INSPECTOR_DEBUGGING && UNITY_EDITOR
#define ENABLE_SERIALIZATION
#endif
using System;
using JetBrains.Annotations;
using UnityEngine;

namespace PrimeTween {
    /// <summary>The main API of the PrimeTween library.<br/><br/>
    /// Use static Tween methods to start animations (tweens).<br/>
    /// Use the returned Tween struct to control the running tween and access its properties.<br/><br/>
    /// Tweens are non-reusable. That is, when a tween completes (or is stopped manually), it becomes 'dead' (<see cref="isAlive"/> == false) and can no longer be used to control the tween or access its properties.<br/>
    /// To restart the animation from the beginning (or play in the opposite direction), simply start a new Tween. Starting tweens is very fast and doesn't allocate garbage,
    /// so you can start hundreds of tweens per second with no performance overhead.</summary>
    /// <example><code>
    /// var tween = Tween.LocalPositionX(transform, endValue: 1.5f, duration: 1f);
    /// // Let the tween run for some time...
    /// if (tween.isAlive) {
    ///     Debug.Log($"Animation is still running, elapsed time: {tween.elapsedTime}.");
    /// } else {
    ///     Debug.Log("Animation is already completed.");
    /// }
    /// </code></example>
    #if ENABLE_SERIALIZATION
    [Serializable]
    #endif
    public
        #if !ENABLE_SERIALIZATION
        readonly
        #endif
        partial struct Tween : IEquatable<Tween> {
        /// Uniquely identifies the tween.
        /// Can be observed from the Debug Inspector if PRIME_TWEEN_INSPECTOR_DEBUGGING is defined. Use only for debugging purposes.
        internal
            #if !ENABLE_SERIALIZATION
            readonly
            #endif
            long id;

        internal readonly ColdData tween;

        internal bool IsCreated => id != 0;

        internal Tween([NotNull] ColdData tween) {
            Assert.IsNotNull(tween);
            Assert.AreNotEqual(-1, tween.id);
            id = tween.id;
            this.tween = tween;
        }

        /// A tween is 'alive' when it has been created and has not stopped and has not completed yet. Paused tween is also considered 'alive'.
        public bool isAlive => id != 0 && tween.id == id && tween.hasData && tween.data.isAlive;

        /// Elapsed time of the current cycle.
        public float elapsedTime {
            get {
                if (!ValidateIsAlive()) {
                    return 0;
                }
                if (cyclesDone == cyclesTotal) {
                    return duration;
                }
                float result = elapsedTimeTotal - duration * cyclesDone;
                if (result < 0f) {
                    return 0f;
                }
                Assert.IsTrue(result >= 0f);
                return result;
            }
            set => SetElapsedTime(value);
        }

        void SetElapsedTime(float value) {
            if (!TryManipulate()) {
                return;
            }
            if (value < 0f || float.IsNaN(value)) {
                Debug.LogError($"Invalid elapsedTime value: {value}, tween: {ToString()}");
                return;
            }
            var cycleDuration = duration;
            if (value > cycleDuration) {
                value = cycleDuration;
            }
            var _cyclesDone = cyclesDone;
            if (_cyclesDone == cyclesTotal) {
                _cyclesDone -= 1;
            }
            SetElapsedTimeTotal(value + cycleDuration * _cyclesDone);
        }

        /// The total number of cycles. Returns -1 to indicate an infinite number of cycles.
        public int cyclesTotal => ValidateIsAlive() ? tween.data.cyclesTotal : 0;

        public int cyclesDone => ValidateIsAlive() ? tween.data.getCyclesDone() : 0;
        /// The duration of one cycle.
        public float duration {
            get {
                if (!ValidateIsAlive()) {
                    return 0;
                }
                var result = tween.data.cycleDuration;
                TweenSettings.validateFiniteDuration(ref result);
                return result;
            }
        }

        [NotNull]
        public override string ToString() {
            if (isAlive && tween.hasData) {
                return tween.managedData.GetDescription();
            } else {
                return $"DEAD / id {id}";
            }
        }

        /// Elapsed time of all cycles.
        public float elapsedTimeTotal {
            get {
                if (!ValidateIsAlive()) {
                    return 0;
                }
                // ReSharper disable once CompareOfFloatsByEqualityOperator
                if (tween.data.elapsedTimeTotal == float.MaxValue) {
                    return durationTotal;
                }
                return Mathf.Clamp(tween.data.elapsedTimeTotal - tween.data.waitDelay, 0f, durationTotal);
            }
            set => SetElapsedTimeTotal(value);
        }

        void SetElapsedTimeTotal(float value) {
            if (!TryManipulate()) {
                return;
            }
            if (value < 0f || float.IsNaN(value) || (cyclesTotal == -1 && value >= float.MaxValue)) { // >= tests for positive infinity, see SetInfiniteTweenElapsedTime() test
                Debug.LogError($"Invalid elapsedTimeTotal value: {value}, tween: {ToString()}");
                return;
            }
            ref var t = ref tween.managedData;
            ref var d = ref tween.data;

            t.SetElapsedTimeTotal(value, false, ref d);

            // SetElapsedTimeTotal may complete the tween, so isAlive check is needed
            if (d.isAlive) {
                float durationTotalCached = durationTotal;
                if (value > durationTotalCached) {
                    d.elapsedTimeTotal = durationTotalCached;
                }
            }
        }

        /// <summary>The duration of all cycles. If cycles == -1, returns <see cref="float.PositiveInfinity"/>.</summary>
        public float durationTotal {
            get {
                if (!ValidateIsAlive()) {
                    return 0;
                }
                int cycles = tween.data.cyclesTotal;
                if (cycles == -1) {
                    return float.PositiveInfinity;
                }
                Assert.AreNotEqual(0, cycles);
                return tween.data.cycleDuration * cycles;
            }
        }

        /// Normalized progress of the current cycle expressed in 0..1 range.
        public float progress {
            get {
                if (!ValidateIsAlive()) {
                    return 0;
                }
                if (duration == 0) {
                    return GetProgressFromState();
                }
                return Mathf.Min(elapsedTime / duration, 1f);
            }
            set {
                value = Mathf.Clamp01(value);
                if (value == 1f) {
                    bool isLastCycle = cyclesDone == cyclesTotal - 1;
                    if (isLastCycle) {
                        SetElapsedTimeTotal(float.MaxValue);
                        return;
                    }
                }
                SetElapsedTime(value * duration);
            }
        }

        /// Normalized progress of all cycles expressed in 0..1 range.
        public float progressTotal {
            get {
                if (!ValidateIsAlive()) {
                    return 0;
                }
                if (cyclesTotal == -1) {
                    return 0;
                }
                var _totalDuration = durationTotal;
                Assert.IsFalse(float.IsInfinity(_totalDuration));
                if (_totalDuration == 0) {
                    return GetProgressFromState();
                }
                return Mathf.Min(elapsedTimeTotal / _totalDuration, 1f);
            }
            set {
                if (!ValidateIsAlive()) {
                    return;
                }
                if (cyclesTotal == -1) {
                    Debug.LogError($"It's not allowed to set progressTotal on infinite tween (cyclesTotal == -1), tween: {ToString()}.");
                    return;
                }
                value = Mathf.Clamp01(value);
                if (value == 1f) {
                    SetElapsedTimeTotal(float.MaxValue);
                    return;
                }
                SetElapsedTimeTotal(value * durationTotal);
            }
        }

        float GetProgressFromState() => (tween.data.flags & Flags.StateAfter) != 0 ? 1f : 0f;

        /// <summary>The current percentage of change between 'startValue' and 'endValue' values in 0..1 range.</summary>
        public float interpolationFactor => ValidateIsAlive() ? Mathf.Max(0f, tween.data.easedInterpolationFactor) : 0f;

        public bool isPaused {
            get => TryManipulate() && tween.data.isPaused;
            set {
                if (TryManipulate()) {
                    ref var rt = ref tween.managedData;
                    ref var d = ref tween.data;
                    if (d.trySetPause(value)) {
                        if (value) {
                            return;
                        }
                        if ((timeScale > 0 && progressTotal >= 1f) ||
                            (timeScale < 0 && progressTotal == 0f)) {
                            if (d.IsMainSequenceRoot()) {
                                new Sequence(rt.cold.sequence).ReleaseTweens();
                            } else {
                                rt.Kill(ref d);
                            }
                        }
                    }
                }
            }
        }

        /// Interrupts the tween, ignoring onComplete callback.
        public void Stop() {
            if (isAlive && TryManipulate(false)) {
                tween.managedData.Kill(ref tween.data);
            }
        }

        /// <summary>Immediately completes the tween.<br/>
        /// If the tween has infinite cycles (cycles == -1), completes only the current cycle. To choose between 'startValue' and 'endValue' in the case of infinite cycles, use <see cref="SetRemainingCycles(bool stopAtEndValue)"/> before calling Complete().</summary>
        public void Complete() {
            // don't warn that a tween is dead because a dead tween means that it's already 'completed'
            if (isAlive && TryManipulate(false)) {
                tween.managedData.ForceComplete(ref tween.data);
            }
        }

        internal bool TryManipulate(bool checkRecursive = true) {
            if (!ValidateIsAlive()) {
                return false;
            }
            ref var d = ref tween.data;
            if (!d.canManipulate()) {
                tween.managedData.LogErrorWithStackTrace(Constants.cantManipulateNested);
                return false;
            }

            if (d.isInSequence) {
                Assert.IsTrue(d.IsMainSequenceRoot());
                if (checkRecursive) {
                    foreach (var child in new Sequence(tween).GetAllTweens()) {
                        if (child.data.isUpdating) {
                            Debug.LogError(Constants.recursiveCallError);
                            return false;
                        }
                    }
                } else {
                    foreach (var child in new Sequence(tween).GetAllTweens()) {
                        child.data.isUpdating = false;
                    }
                }
            } else {
                if (checkRecursive) {
                    if (d.isUpdating) {
                        Debug.LogError(Constants.recursiveCallError);
                        return false;
                    }
                } else {
                    d.isUpdating = false;
                }
            }
            return true;
        }

        /// <summary>Stops the tween when it reaches 'startValue' or 'endValue' for the next time.<br/>
        /// For example, if you have an infinite tween (cycles == -1) with CycleMode.Yoyo/Rewind, and you wish to stop it when it reaches the 'endValue', then set <see cref="stopAtEndValue"/> to true.
        /// To stop the animation at the 'startValue', set <see cref="stopAtEndValue"/> to false.</summary>
        public void SetRemainingCycles(bool stopAtEndValue) {
            if (!TryManipulate()) {
                return;
            }
            ref var d = ref tween.data;
            if (d.cycleMode == CycleMode.Restart || d.cycleMode == CycleMode.Incremental) {
                Debug.LogWarning(nameof(SetRemainingCycles) + "(bool " + nameof(stopAtEndValue) + ") is meant to be used with CycleMode.Yoyo or Rewind. Please consider using the overload that accepts int instead.");
            }
            bool isOneCycleLeft = d.getCyclesDone() % 2 == 0 == stopAtEndValue;
            if (tween.data.isSequenceInverted) {
                isOneCycleLeft = !isOneCycleLeft;
            }
            SetRemainingCycles(isOneCycleLeft ? 1 : 2);
        }

        /// <summary>Sets the number of remaining cycles.<br/>
        /// This method modifies the <see cref="cyclesTotal"/> so that the tween will complete after the number of <see cref="cycles"/>.<br/>
        /// In case of negative <see cref="timeScale"/>, it modifies <see cref="cyclesDone"/> and <see cref="elapsedTimeTotal"/> so that the tween will rewind to the beginning after the number of <see cref="cycles"/>.<br/>
        /// To set the initial number of cycles, pass the 'cycles' parameter to 'Tween.' methods instead.<br/><br/>
        /// Setting cycles to -1 will repeat the tween indefinitely.<br/></summary>
        public void SetRemainingCycles(int cycles) {
            Assert.IsTrue(cycles >= -1);
            if (!TryManipulate()) {
                return;
            }
            ref var d = ref tween.data;
            if (d.tweenType == TweenAnimation.TweenType.Delay && tween.managedData.HasOnComplete) {
                Debug.LogError("Applying cycles to Delay will not repeat the OnComplete() callback, but instead will increase the Delay duration.\n" +
                               "OnComplete() is called only once when ALL tween cycles complete. To repeat the OnComplete() callback, please use the Sequence.Create(cycles: numCycles) and put the tween inside a Sequence.\n" +
                               "More info: https://discussions.unity.com/t/926420/101\n");
            }
            if (cycles == -1) {
                if (d.timeScale > 0f) {
                    d.cyclesTotal = -1;
                } else {
                    Debug.LogError($"'{nameof(SetRemainingCycles)}()' doesn't work with negative '{nameof(d.timeScale)}' and infinite(-1) '{nameof(cycles)}'.");
                }
            } else {
                TweenSettings.setCyclesTo1If0(ref cycles);
                if (d.timeScale > 0f) {
                    d.cyclesTotal = d.getCyclesDone() + cycles;
                } else {
                    int targetCyclesDone = cycles - 1;
                    d.elapsedTimeTotal = targetCyclesDone * d.cycleDuration + elapsedTime;
                    d.cyclesDone = targetCyclesDone;
                    if (d.cyclesTotal < targetCyclesDone) {
                        d.cyclesTotal = targetCyclesDone + 1;
                    }
                }
            }
        }

        /// <summary>Adds completion callback. Please consider using <see cref="OnComplete{T}"/> to prevent a possible capture of variable into a closure.</summary>
        /// <param name="warnIfTargetDestroyed">Set to 'false' to disable the error about target's destruction. Please note that the <see cref="onComplete"/> callback will be silently ignored in the case of target's destruction. More info: https://github.com/KyryloKuzyk/PrimeTween/discussions/4</param>
        public Tween OnComplete([CanBeNull] Action onComplete, bool? warnIfTargetDestroyed = null) {
            if (ValidateIsAlive()) {
                tween.managedData.OnComplete(onComplete, warnIfTargetDestroyed);
            }
            return this;
        }

        /// <summary>Adds completion callback.</summary>
        /// <param name="warnIfTargetDestroyed">Set to 'false' to disable the error about target's destruction. Please note that the <see cref="onComplete"/> callback will be silently ignored in the case of target's destruction. More info: https://github.com/KyryloKuzyk/PrimeTween/discussions/4</param>
        /// <example>The example shows how to destroy the object after the completion of a tween.
        /// Please note: we're using the '_transform' variable from the onComplete callback to prevent garbage allocation. Using the 'transform' variable directly will capture it into a closure and generate garbage.
        /// <code>
        /// Tween.PositionX(transform, endValue: 1.5f, duration: 1f)
        ///     .OnComplete(transform, _transform =&gt; Destroy(_transform.gameObject));
        /// </code></example>
        public Tween OnComplete<T>([NotNull] T target, [CanBeNull] Action<T> onComplete, bool? warnIfTargetDestroyed = null) where T : class {
            if (ValidateIsAlive()) {
                tween.managedData.OnComplete(target, onComplete, warnIfTargetDestroyed);
            }
            return this;
        }

        public Sequence Group(Tween _tween) => TryManipulate() ? Sequence.Create(this).Group(_tween) : default;
        public Sequence Chain(Tween _tween) => TryManipulate() ? Sequence.Create(this).Chain(_tween) : default;
        public Sequence Group(Sequence sequence) => TryManipulate() ? Sequence.Create(this).Group(sequence) : default;
        public Sequence Chain(Sequence sequence) => TryManipulate() ? Sequence.Create(this).Chain(sequence) : default;

        internal bool ValidateIsAlive() {
            if (!IsCreated) {
                if (!PrimeTweenManager.Instance.isDestroyed) {
                    Debug.LogError(Constants.defaultCtorError);
                }
            } else if (!isAlive) {
                Assert.LogErrorWithStackTrace(Constants.isDeadMessage, id, null);
            }
            return isAlive;
        }

        /// <summary>Custom timeScale. To smoothly animate timeScale over time, use <see cref="Tween.TweenTimeScale"/> method.</summary>
        public float timeScale {
            get => TryManipulate() ? tween.data.timeScale : 1;
            set {
                if (TryManipulate()) {
                    if (float.IsNaN(value) || float.IsInfinity(value)) {
                        throw new ArgumentException($"Invalid {nameof(timeScale)}: {value}.");
                    }
                    tween.data.timeScale = value;
                }
            }
        }

        public Tween OnUpdate<T>(T target, Action<T, Tween> onUpdate) where T : class {
            if (ValidateIsAlive()) {
                tween.managedData.SetOnUpdate(target, onUpdate);
            }
            return this;
        }

        internal float durationWithWaitDelay => tween.data.calcDurationWithWaitDependencies();

        public override int GetHashCode() => id.GetHashCode();
        /// https://www.jacksondunstan.com/articles/5148
        public bool Equals(Tween other) => isAlive && other.isAlive && id == other.id;

        [Obsolete(ObsoleteMessages.resetOnCompleteRenamed, true)]
        #if PRIME_TWEEN_EXPERIMENTAL
        public
        #endif
        Tween ResetOnComplete()  => ResetOnCompletion();

        /// <summary>Instantly resets the animation to the beginning upon completion.</summary>
        #if PRIME_TWEEN_EXPERIMENTAL
        public
        #else
        internal
        #endif
        Tween ResetOnCompletion() {
            if (ValidateIsAlive()) {
                tween.data.resetOnComplete = true;
            }
            return this;
        }
    }
}
