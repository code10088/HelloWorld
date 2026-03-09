using System;
using System.Collections;
using JetBrains.Annotations;

namespace PrimeTween {
    public partial struct Tween : IEnumerator {
        /// <summary>Use this method to wait for a Tween in coroutines.</summary>
        /// <example><code>
        /// IEnumerator Coroutine() {
        ///     yield return Tween.Delay(1).ToYieldInstruction();
        /// }
        /// </code></example>
        [NotNull]
        public IEnumerator ToYieldInstruction() {
            if (!isAlive || !TryManipulate()) {
                return Array.Empty<object>().GetEnumerator();
            }
            if (tween.data.isInCoroutine) {
                Assert.LogErrorWithStackTrace($"{nameof(ToYieldInstruction)} can't be called on a tween that's already being waited for in a coroutine.", tween.id, tween.managedData.target);
                return Array.Empty<object>().GetEnumerator();
            }
            tween.data.isInCoroutine = true;
            return tween;
        }

        bool IEnumerator.MoveNext() {
            PrimeTweenManager.Instance.WarnStructBoxingInCoroutineOnce(id, tween);
            return isAlive;
        }

        object IEnumerator.Current {
            get {
                Assert.IsTrue(isAlive);
                return null;
            }
        }

        void IEnumerator.Reset() => throw new NotSupportedException();
    }

    public partial struct Sequence : IEnumerator {
        /// <summary>Use this method to wait for a Sequence in coroutines.</summary>
        /// <example><code>
        /// IEnumerator Coroutine() {
        ///     var sequence = Sequence.Create(Tween.Delay(1)).ChainCallback(() =&gt; Debug.Log("Done!"));
        ///     yield return sequence.ToYieldInstruction();
        /// }
        /// </code></example>
        [NotNull]
        public IEnumerator ToYieldInstruction() => root.ToYieldInstruction();

        bool IEnumerator.MoveNext() {
            PrimeTweenManager.Instance.WarnStructBoxingInCoroutineOnce(id, root.tween);
            return isAlive;
        }

        object IEnumerator.Current {
            get {
                Assert.IsTrue(isAlive);
                return null;
            }
        }

        void IEnumerator.Reset() => throw new NotSupportedException();
    }

    internal partial class ColdData : IEnumerator {
        bool IEnumerator.MoveNext() {
            var result = data.isAlive;
            if (!result) {
                ResetCoroutineEnumerator();
            }
            return result;
        }

        internal bool ResetCoroutineEnumerator() {
            if (data.isInCoroutine) {
                data.isInCoroutine = false;
                return true;
            }
            return false;
        }

        object IEnumerator.Current {
            get {
                Assert.IsTrue(data.isAlive); // p2 todo throws if debugger is attached
                Assert.IsTrue(data.isInCoroutine);
                return null;
            }
        }

        void IEnumerator.Reset() => throw new NotSupportedException();
    }
}
