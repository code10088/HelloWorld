#if TEST_FRAMEWORK_INSTALLED
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using JetBrains.Annotations;
using NUnit.Framework;
using PrimeTween;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Assert = NUnit.Framework.Assert;
using AssertionException = UnityEngine.Assertions.AssertionException;
using Debug = UnityEngine.Debug;
using Mathf = UnityEngine.Mathf;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;
using SuppressMessage = System.Diagnostics.CodeAnalysis.SuppressMessageAttribute;

public partial class Tests {
    [Test]
    public void TweenTimeScaleWhileInSequence() {
        var t = Tween.Delay(0.01f);
        Sequence.Create(t);
        expectCantManipulateTweenInsideSequence();
        t.timeScale = 2f;
        LogAssert.NoUnexpectedReceived();
        Tween.StopAll();
    }

    [Test]
    public async Task TweenTimeScaleOutlive() {
        {
            var tween = Tween.Delay(getDt() / 2);
            var timeScaleTween = Tween.TweenTimeScale(tween, Random.value + 0.01f, float.MaxValue);
            await tween;
            Assert.IsFalse(timeScaleTween.isAlive);
        }
        {
            var seq = Sequence.Create(Tween.Delay(getDt() / 2));
            var timeScaleTween = Tween.TweenTimeScale(seq, Random.value + 0.01f, float.MaxValue);
            await seq;
            Assert.IsFalse(timeScaleTween.isAlive);
        }
    }

    [Test]
    public async Task TweenTimeScale() {
        var t = Tween.PositionZ(transform, 10, 1);
        const float iniTimeScale = 0.9f;
        t.timeScale = iniTimeScale;
        Assert.AreEqual(iniTimeScale, t.timeScale);
        const float targetTimeScale = 0.5f;
        await Tween.TweenTimeScale(t, targetTimeScale, 0.001f);
        Assert.AreEqual(targetTimeScale, t.timeScale);
        t.Complete();
    }

    [UnityTest]
    public IEnumerator QuaternionDefaultValue() {
        {
            var q = Quaternion.Euler(0, 0, 45);
            var def = new Quaternion();
            Assert.AreEqual(Quaternion.identity.normalized, def.normalized);
            Assert.AreNotEqual(Quaternion.Angle(Quaternion.identity, q), Quaternion.Angle(def, q));
            Assert.AreEqual(Quaternion.Angle(Quaternion.identity, q), Quaternion.Angle(def.normalized, q));
        }
        {
            Tween t = default;
            var def = new Quaternion();
            int numCallback = 0;
            t = Tween.Custom(def, def, 0.01f, delegate {
                numCallback++;
                var startVal = t.tween.data.startValue.quaternion;
                var endVal = t.tween.managedData.endValueOrDiff.quaternion;
                // Debug.Log($"{startVal}, {endVal}");
                Assert.AreNotEqual(def, startVal);
                Assert.AreNotEqual(def, endVal);
                t.Stop();
            });
            yield return t.ToYieldInstruction();
            Assert.AreEqual(1, numCallback);
        }
    }

    /// This test can fail if Game window is set to 'Play Unfocused'
    [UnityTest]
    public IEnumerator StartValueIsAppliedOnFirstFrame() {
        const int iniVal = -1;
        float val = iniVal;
        const int startValue = 0;
        Tween.Custom(startValue, 1, 0.01f, newVal => val = newVal);
        Assert.AreEqual(iniVal, val);
        yield return new WaitForEndOfFrame();
        Assert.AreEqual(startValue, val);
        yield return new WaitForEndOfFrame();
        Assert.AreNotEqual(startValue, val);
    }

    [Test]
    public void SafetyChecksEnabled() {
        #if !PRIME_TWEEN_SAFETY_CHECKS
        Assert.Inconclusive();
        #endif
    }

    [UnityTest]
    public IEnumerator TweenIsDeadInOnComplete() {
        Tween t = default;
        t = Tween.Delay(0.01f, () => {
            Assert.IsFalse(t.isAlive);
            for (int i = 0; i < 6; i++) {
                expectIsDeadError();
            }
            Assert.AreEqual(0, t.elapsedTime);
            Assert.AreEqual(0, t.elapsedTimeTotal);
            Assert.AreEqual(0, t.progress);
            Assert.AreEqual(0, t.progressTotal);
            Assert.AreEqual(0, t.duration);
            Assert.AreEqual(0, t.durationTotal);
        });
        yield return t.ToYieldInstruction();
    }

    static void expectIsDeadError(bool isCreated = true) => LogAssert.Expect(LogType.Error, new Regex(isCreated ? Constants.isDeadMessage : "Animation is not created."));

    [Test]
    public void ShakeDuplication1() {
        var s1 = Tween.ShakeLocalPosition(transform, Vector3.one, 0.1f, startDelay: 0.1f);
        var s2 = Tween.ShakeLocalPosition(transform, Vector3.one, 0.1f);
        Assert.IsTrue(s1.isAlive);
        Assert.IsTrue(s2.isAlive);
    }

    [UnityTest]
    public IEnumerator ShakeDuplication2() {
        var s1 = Tween.ShakeLocalPosition(transform, Vector3.one, 0.1f);
        Assert.IsTrue(s1.isAlive);
        var s2 = Tween.ShakeLocalPosition(transform, Vector3.one, 0.1f);
        Assert.IsTrue(s1.isAlive);
        yield return null;
        // because two shakes are started the same frame, the first one completes the second one
        Assert.IsTrue(s1.isAlive);
        Assert.IsTrue(s2.isAlive);
    }

    [UnityTest]
    public IEnumerator ShakeDuplication3() {
        var s1 = Tween.ShakeLocalPosition(transform, Vector3.one, 0.1f);
        yield return null;
        var s2 = Tween.ShakeLocalPosition(transform, Vector3.one, 0.1f);
        yield return null;
        Assert.IsTrue(s1.isAlive);
        Assert.IsTrue(s2.isAlive);
    }

    [UnityTest]
    public IEnumerator ShakeDuplication4() {
        const float startDelay = 0.05f;
        var s1 = Tween.ShakeLocalPosition(transform, Vector3.one, 0.1f, startDelay: startDelay);
        var s2 = Tween.ShakeLocalPosition(transform, Vector3.one, 0.1f, startDelay: 0.1f);
        yield return Tween.Delay(startDelay + Time.deltaTime).ToYieldInstruction();
        Assert.IsTrue(s1.isAlive);
        Assert.IsTrue(s2.isAlive);
    }

    [UnityTest]
    public IEnumerator ShakeDuplication5() {
        var target = new GameObject(nameof(ShakeDuplication5)).transform;
        target.localPosition = new Vector3(Random.value, Random.value, Random.value);
        var iniPos = target.localPosition;
        var s1 = Tween.ShakeLocalPosition(target, Vector3.one, 0.1f);
        var seq = Sequence.Create(s1);
        Assert.IsTrue(s1.isAlive);
        var s2 = Tween.ShakeLocalPosition(target, Vector3.one, 0.1f);
        Assert.IsTrue(s1.isAlive);
        Assert.IsTrue(seq.isAlive);
        Assert.IsTrue(s2.isAlive);

        Assert.IsTrue(s1.tween.data.startFromCurrent);
        Assert.IsTrue(s2.tween.data.startFromCurrent);
        yield return null;
        Assert.IsTrue(s1.isAlive);
        Assert.IsTrue(seq.isAlive);
        Assert.IsTrue(s2.isAlive);

        Assert.IsFalse(s1.tween.data.startFromCurrent);
        Assert.IsFalse(s2.tween.data.startFromCurrent);
        Assert.AreEqual(iniPos, s1.tween.data.startValue.vector3);
        Assert.AreEqual(iniPos, s2.tween.data.startValue.vector3);
    }

    [Test]
    public void ShakeDuplicationDestroyedTarget() {
        var target = new GameObject(nameof(ShakeDuplicationDestroyedTarget)).transform;
        Tween.ShakeLocalPosition(target, Vector3.one, 0.1f);
        Object.DestroyImmediate(target.gameObject);
        Tween.ShakeLocalPosition(target, Vector3.one, 0.1f);
        expectTargetIsNull();
    }

    static void expectTargetIsNull() => LogAssert.Expect(LogType.Error, new Regex("Tween's target is null"));

    [UnityTest]
    public IEnumerator FramePacing() {
        Tween.StopAll();
        const int fps = 120;
        Application.targetFrameRate = fps;
        QualitySettings.vSyncCount = 0;
        Assert.AreEqual(fps, Application.targetFrameRate);
        yield return null;
        {
            var go = new GameObject();
            go.AddComponent<FramePacingTest>();
            while (go != null) {
                yield return null;
            }
        }
        Application.targetFrameRate = targetFrameRate;
        yield return null;
    }

    #if UNITY_EDITOR
    [Test]
    public void GenerateCode() {
        const string path = "Packages/com.kyrylokuzyk.primetween/Editor/CodeGenerator.asset";
        var codeGenerator = UnityEditor.AssetDatabase.LoadAssetAtPath<CodeGenerator>(path);
        codeGenerator.Generate();
    }
    #endif

    [Test]
    public void TweenCompleteInvokeOnCompleteParameter() {
        {
            int numCompleted = 0;
            var t = Tween.Scale(transform, 1.5f, 0.01f).OnComplete(() => numCompleted++);
            t.Complete();
            Assert.AreEqual(1, numCompleted);
            t.Complete();
            Assert.AreEqual(1, numCompleted);
        }
        /*{
            int numCompleted = 0;
            var t = Tween.Scale(transform, 1.5f, 0.01f).OnComplete(() => numCompleted++);
            t.Complete(false);
            Assert.AreEqual(0, numCompleted);
            t.Complete(false);
            Assert.AreEqual(0, numCompleted);
            t.Complete();
            Assert.AreEqual(0, numCompleted);
        }*/
    }

    [Test]
    public void IgnoreFromInScale() {
        var t = Tween.Scale(transform, 1.5f, 0.01f);
        Assert.IsTrue(t.tween.data.startFromCurrent);
    }

    [UnityTest]
    public IEnumerator FromToValues() {
        {
            var duration = getDt() * Random.Range(0.5f, 1.5f);
            var t = Tween.Custom(0, 0, duration, ease: Ease.Linear, onValueChange: delegate { });
            while (t.isAlive) {
                Assert.AreEqual(t.elapsedTime, t.progress * duration, 0.001f);
                Assert.AreEqual(t.interpolationFactor, t.progress);
                Assert.AreEqual(t.interpolationFactor, t.progressTotal);
                yield return null;
            }
        }
        var from = Random.value;
        var to = Random.value;
        var data = new TweenSettings<float>(from, to, 0.01f);
        {
            var t = Tween.LocalPositionX(transform, data);
            Assert.AreEqual(from, t.tween.data.startValue.single);
            Assert.AreEqual(to - from, t.tween.managedData.endValueOrDiff.single);
        }
        {
            var t = Tween.Custom(this, data, delegate { });
            Assert.AreEqual(from, t.tween.data.startValue.single);
            Assert.AreEqual(to - from, t.tween.managedData.endValueOrDiff.single);
        }
    }

    [UnityTest]
    public IEnumerator TweenCompleteWhenInterpolationCompleted() {
        float curVal = 0f;
        var t = Tween.Custom(this, 0f, 1f, 0.05f, (_, val) => curVal = val, cycles: 2, endDelay: 1f, cycleMode: CycleMode.Yoyo);
        while (t.interpolationFactor < 1f) {
            yield return null;
        }
        Assert.AreEqual(0, t.cyclesDone);
        Assert.AreEqual(1f, curVal);
        t.Complete();
        Assert.AreEqual(0f, curVal);
    }

    [Test]
    public async Task CycleModeIncremental() {
        {
            float curVal = 0f;
            await Tween.Custom(this, 0f, 1f, 0.01f, (_, val) => curVal = val, cycles: 2, cycleMode: CycleMode.Incremental);
            Assert.AreEqual(2f, curVal);
        }
        {
            float curVal = 0f;
            await Tween.Custom(this, 0f, 1f, 0.01f, (_, val) => curVal = val, cycles: 4, cycleMode: CycleMode.Incremental);
            Assert.AreEqual(4f, curVal);
        }
        {
            float curVal = 0f;
            Tween.Custom(this, 0f, 1f, 0.01f, (_, val) => curVal = val, cycles: 2, cycleMode: CycleMode.Incremental)
                .Complete();
            Assert.AreEqual(2f, curVal);
        }
        {
            float curVal = 0f;
            Tween.Custom(this, 0f, 1f, 0.01f, (_, val) => curVal = val, cycles: 4, cycleMode: CycleMode.Incremental)
                .Complete();
            Assert.AreEqual(4f, curVal);
        }
    }

    [Test]
    public void TweenCompleteWithEvenCycles() {
        {
            float curVal = 0f;
            Tween.Custom(this, 0f, 1f, 0.05f, (_, val) => curVal = val, cycles: 2, cycleMode: CycleMode.Restart)
                .Complete();
            Assert.AreEqual(1f, curVal);
        }
        {
            float curVal = 0f;
            Tween.Custom(this, 0f, 1f, 0.05f, (_, val) => curVal = val, cycles: 4, cycleMode: CycleMode.Restart)
                .Complete();
            Assert.AreEqual(1f, curVal);
        }

        {
            float curVal = 0f;
            Tween.Custom(this, 0f, 1f, 0.05f, (_, val) => curVal = val, cycles: 2, cycleMode: CycleMode.Yoyo)
                .Complete();
            Assert.AreEqual(0f, curVal);
        }
        {
            float curVal = 0f;
            Tween.Custom(this, 0f, 1f, 0.05f, (_, val) => curVal = val, cycles: 4, cycleMode: CycleMode.Yoyo)
                .Complete();
            Assert.AreEqual(0f, curVal);
        }
        {
            float curVal = 0f;
            Tween.Custom(this, 0f, 1f, 0.05f, (_, val) => curVal = val, cycles: 2, cycleMode: CycleMode.Rewind)
                .Complete();
            Assert.AreEqual(0f, curVal);
        }
        {
            float curVal = 0f;
            Tween.Custom(this, 0f, 1f, 0.05f, (_, val) => curVal = val, cycles: 4, cycleMode: CycleMode.Rewind)
                .Complete();
            Assert.AreEqual(0f, curVal);
        }
    }

    [Test]
    public void TweenCompleteWithOddCycles() {
        {
            float curVal = 0f;
            Tween.Custom(this, 0f, 1f, 0.05f, (_, val) => curVal = val, cycles: 1, cycleMode: CycleMode.Yoyo)
                .Complete();
            Assert.AreEqual(1f, curVal);
        }
        {
            float curVal = 0f;
            Tween.Custom(this, 0f, 1f, 0.05f, (_, val) => curVal = val, cycles: 3, cycleMode: CycleMode.Yoyo)
                .Complete();
            Assert.AreEqual(1f, curVal);
        }
    }

    [UnityTest]
    public IEnumerator TweenOnCompleteIsCalledOnceForTweenInSequence() {
        for (int i = 0; i < 1; i++) {
            loopBegin:
            float curVal = 0f;
            int numCompleted = 0;
            float duration = Mathf.Max(minDuration, getDt()) * 10f;
            var t = Tween.Custom(this, 0f, 1f, duration, (_, val) => curVal = val, cycles: 1, cycleMode: CycleMode.Yoyo)
                .OnComplete(() => numCompleted++);
            var s = t.Chain(Tween.Delay(duration));
            while (true) {
                if (!t.isAlive) {
                    goto loopBegin;
                }
                if (t.cyclesDone == 1) {
                    break;
                }
                yield return null;
            }
            Assert.IsTrue(t.isAlive);
            Assert.AreEqual(1, t.tween.data.getCyclesDone());
            Assert.IsNotNull(t.tween.sequence);
            Assert.AreEqual(1, numCompleted);

            Assert.IsTrue(s.isAlive);
            Assert.AreEqual(1f, curVal);
            s.Complete();
            Assert.AreEqual(1f, curVal);
            Assert.AreEqual(1, numCompleted);
        }
    }

    [Test]
    public void TweenCompleteInSequence() {
        float curVal = 0f;
        var t = Tween.Custom(this, 0f, 1f, 0.05f, (_, val) => curVal = val, cycles: 1, cycleMode: CycleMode.Yoyo);
        var s = t.Chain(Tween.Delay(0.05f));
        Assert.IsTrue(t.isAlive);
        Assert.AreEqual(0, t.tween.data.getCyclesDone());
        Assert.IsNotNull(t.tween.sequence);
        Assert.IsTrue(s.isAlive);
        Assert.AreNotEqual(1f, curVal);
        s.Complete();
        Assert.AreEqual(1f, curVal);
    }

    [Test]
    public async Task AwaitExceptions() {
        expectTweenWasStoppedBecauseException();
        await Tween.Custom(this, 0f, 1f, 1f, delegate {
            throw new Exception();
        });
    }

    [UnityTest]
    public IEnumerator CoroutineEnumeratorNotEnumeratedToTheEnd() {
        Tween.StopAll();
        Assert.AreEqual(0, tweensCount);
        var t = Tween.Delay(TweenSettings.minDuration * 100);
        var e = t.ToYieldInstruction();
        Assert.IsTrue(e.MoveNext());
        yield return e.Current;
        Assert.IsTrue(t.isAlive);
        while (t.isAlive) {
            yield return null;
        }
        yield return null;
        Assert.AreEqual(0, tweensCount);
        testCompletedCorEnumerator(e);
    }

    [UnityTest]
    public IEnumerator CoroutineEnumeratorInfiniteTween() {
        {
            var t = Tween.Position(transform, Vector3.one, getDt(), cycles: -1);
            Tween.Delay(getDt() * 5f).OnComplete(() => t.Stop());
            yield return t.ToYieldInstruction();
        }
        {
            var t = Tween.Position(transform, Vector3.one, getDt(), cycles: -1);
            Tween.Delay(getDt() * 5f).OnComplete(() => t.Complete());
            yield return t.ToYieldInstruction();
        }
    }

    [UnityTest]
    public IEnumerator CoroutineEnumeratorMultipleToYieldInstruction() {
        var t = Tween.Delay(0.01f);
        var e = t.ToYieldInstruction();
        LogAssert.Expect(LogType.Error, new Regex("already being waited for in a coroutine"));
        t.ToYieldInstruction();
        t.Complete();
        yield return e;
        Assert.IsFalse(t.isAlive);
        testCompletedCorEnumerator(e);
    }

    [UnityTest]
    public IEnumerator CoroutineEnumeratorUsingDead() {
        var t = Tween.Delay(0.01f);
        var e = t.ToYieldInstruction();
        yield return e;
        Assert.IsFalse(t.isAlive);
        testCompletedCorEnumerator(e);
    }

    static void testCompletedCorEnumerator(IEnumerator e) {
        Assert.IsFalse(e.MoveNext());
        Assert.Throws<AssertionException>(() => {
            _ = e.Current;
        });
        Assert.Throws<NotSupportedException>(() => e.Reset());
    }

    [UnityTest]
    public IEnumerator YieldInstructionsClash2() {
        var test = new GameObject().AddComponent<YieldInstructionsClash>();
        // ReSharper disable once LoopVariableIsNeverChangedInsideLoop
        while (test != null) {
            yield return null;
        }
    }

    [UnityTest]
    public IEnumerator YieldInstructionsClash() {
        Application.targetFrameRate = 100;
        yield return null;
        Assert.AreEqual(0, tweensCount);
        for (int i = 0; i < 1; i++) {
            {
                var t1 = Tween.Delay(minDuration);
                int frameStart = Time.frameCount;
                yield return t1.ToYieldInstruction();
                Assert.AreEqual(1, Time.frameCount - frameStart);
                Assert.IsFalse(t1.isAlive);
                var t2 = Tween.Delay(minDuration);
                t2.ToYieldInstruction();
                t2.Complete();
            }
            {
                var t1 = Tween.Delay(minDuration);
                t1.ToYieldInstruction();
                yield return null;
                yield return null;
                Assert.IsFalse(t1.isAlive);
                var t2 = Tween.Delay(minDuration);
                t2.ToYieldInstruction();
                t2.Complete();
            }
        }
        Application.targetFrameRate = targetFrameRate;
    }

    [Test]
    public void TweenDuplicateInSequence() {
        var t1 = Tween.Delay(0.1f);
        var t2 = Tween.Delay(0.1f);
        var s = t1.Chain(t2);
        expectNestTweenTwiceError();
        s.Chain(t1);
    }

    /*[UnityTest]
    public IEnumerator WaitStopFromValueChange2() {
        Tween t = default;
        t = Tween.WaitWhile(this, _ => {
            Assert.IsTrue(t.isAlive);
            t.Stop();
            return true;
        });
        yield return t.ToYieldInstruction();
    }

    [UnityTest]
    public IEnumerator WaitCompleteFromValueChange2() {
        Tween t = default;
        int numValueChanged = 0;
        t = Tween.WaitWhile(this, _ => {
            numValueChanged++;
            switch (numValueChanged) {
                case 1:
                    Assert.IsTrue(t.isAlive);
                    t.Complete();
                    break;
                case 2:
                    Assert.IsFalse(t.isAlive);
                    break;
                default: throw new Exception();
            }
            return true;
        });
        yield return t.ToYieldInstruction();
    }

    [UnityTest]
    public IEnumerator WaitCompleteFromValueChange() {
        var target = new GameObject();
        int numValueChanged = 0;
        yield return Tween.WaitWhile(target, _ => {
            numValueChanged++;
            switch (numValueChanged) {
                case 1:
                    Assert.AreEqual(1, Tween.CompleteAll(target, 1, 1));
                    break;
                case 2:
                    Assert.AreEqual(0, Tween.CompleteAll(target, 0, 0));
                    break;
                default: throw new Exception();
            }
            return true;
        }).ToYieldInstruction();
    }

    [UnityTest]
    public IEnumerator WaitStopFromValueChange() {
        var target = new GameObject();
        yield return Tween.WaitWhile(target, _ => {
            Assert.AreEqual(1, Tween.StopAll(target, 1, 1));
            return true;
        }).ToYieldInstruction();
    }

    [UnityTest]
    public IEnumerator TweenWait() {
        var timeStart = Time.time;
        int numOnCompleteDone = 0;
        const float duration = 0.3f;
        var t = Tween.WaitWhile(this, _ => Time.time - timeStart < duration)
            .OnComplete(() => numOnCompleteDone++);
        yield return null;
        Assert.IsTrue(t.isAlive);
        yield return new WaitForSeconds(duration + 0.01f);
        Assert.IsFalse(t.isAlive);
        Assert.AreEqual(1, numOnCompleteDone);
        LogAssert.NoUnexpectedReceived();
    }

    [Test]
    public void TweenWaitInstantComplete() {
        int numOnCompleteDone = 0;
        var t = Tween.WaitWhile(this, _ => true).OnComplete(() => numOnCompleteDone++);
        Assert.IsTrue(t.isAlive);
        t.Complete();
        Assert.IsFalse(t.isAlive);
        Assert.AreEqual(1, numOnCompleteDone);
    }

    [Test]
    public void TweenWaitInstantStop() {
        int numOnCompleteDone = 0;
        var t = Tween.WaitWhile(this, _ => true).OnComplete(() => numOnCompleteDone++);
        Assert.IsTrue(t.isAlive);
        t.Stop();
        Assert.IsFalse(t.isAlive);
        Assert.AreEqual(0, numOnCompleteDone);
    }

    [Test]
    public void TweenWaitException() {
        var t = Tween.WaitWhile(this, _ => throw new Exception()).OnComplete(Assert.Fail);
        Assert.IsTrue(t.isAlive);
        LogAssert.Expect(LogType.Error, new Regex("Tween was stopped because of exception"));
        LogAssert.Expect(LogType.Error, new Regex(Constants.onCompleteCallbackIgnored));
        t.Complete();
        Assert.IsFalse(t.isAlive);
    }

    [UnityTest]
    public IEnumerator TweenWaitDuration() {
        // Application.targetFrameRate = 60;
        var t = Tween.WaitWhile(this, _ => true);
        validate();
        Assert.AreEqual(0, t.elapsedTime);
        Assert.AreEqual(0, t.elapsedTimeTotal);
        yield return null;
        Assert.AreNotEqual(0, t.elapsedTime);
        Assert.AreNotEqual(0, t.elapsedTimeTotal);
        // for (int i = 0; i < 60; i++) {
        //     yield return null;
        //     Debug.Log($"{t.elapsedTime}, {t.elapsedTimeTotal}");
        // }
        validate();
        t.Complete();
        void validate() {
            Assert.AreEqual(-1, t.cyclesTotal);
            Assert.AreEqual(0, t.cyclesDone);
            Assert.IsTrue(float.IsPositiveInfinity(t.duration));
            Assert.IsTrue(float.IsPositiveInfinity(t.durationTotal));
            Assert.AreEqual(0, t.progress);
            Assert.AreEqual(0, t.progressTotal);
            Assert.AreEqual(0, t.interpolationFactor);
        }
    }*/

    [Test]
    public void ZeroDurationWarning() {
        var oldSetting = PrimeTweenConfig.warnZeroDuration;
        try {
            PrimeTweenConfig.warnZeroDuration = true;
            LogAssert.Expect(LogType.Warning, new Regex(nameof(PrimeTweenManager.warnZeroDuration)));
            Tween.Custom(this, 0, 1, 0, delegate{});
            PrimeTweenConfig.warnZeroDuration = false;
            Tween.Custom(this, 0, 1, 0, delegate{});
            LogAssert.NoUnexpectedReceived();
        } finally {
            PrimeTweenConfig.warnZeroDuration = oldSetting;
        }
    }

    [Test]
    public void CompleteTweenTwice() {
        int numCompleted = 0;
        var t = createCustomTween(1)
            .OnComplete(() => numCompleted++);
        t.Complete();
        Assert.AreEqual(1, numCompleted);
        t.Complete();
        Assert.AreEqual(1, numCompleted);
    }

    [UnityTest]
    public IEnumerator FromValueShouldNotChangeBetweenCycles() {
        var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        var origPos = new Vector3(10, 10, 10);
        cube.transform.position = origPos;
        var tween = Tween.Position(cube.transform, new Vector3(20, 10, 10), 0.01f, cycles: 5);
        Assert.IsTrue(tween.isAlive);
        Assert.IsTrue(tween.tween.data.startFromCurrent);
        yield return null;
        Assert.IsFalse(tween.tween.data.startFromCurrent);
        while (tween.isAlive) {
            Assert.AreEqual(origPos, tween.tween.data.startValue.vector3, "'From' should not change after a cycle. This can happen if tween resets startFromCurrent after a cycle.");
            yield return null;
        }
        Object.Destroy(cube);
    }

    [Test]
    public void SettingIsPausedOnTweenInSequenceDisplayError() {
        var target = new object();
        var t = Tween.Delay(target, 0.01f);
        Sequence.Create(t);
        expectError();
        t.isPaused = true;

        expectError();
        Tween.SetPausedAll(true, target);

        expectError();
        Tween.SetPausedAll(false, target);

        void expectError() {
            expectCantManipulateTweenInsideSequence();
        }
    }

    [Test]
    public void SettingCyclesOnDeadTweenDisplaysError() {
        var t = createTween();
        Assert.IsTrue(t.isAlive);
        t.Complete();
        Assert.IsFalse(t.isAlive);
        expectIsDeadError();
        t.SetRemainingCycles(5);
    }

    [Test]
    public void TestDeadTween() {
        var t = createDeadTween();

        expectError();
        t.isPaused = true;

        t.Stop();
        t.Complete();
        // t.Revert();

        expectError();
        t.SetRemainingCycles(10);

        expectError();
        t.OnComplete(delegate{});

        expectError();
        t.OnComplete(this, delegate { });

        expectError();
        t.timeScale = 0;

        void expectError() {
            expectIsDeadError();
        }
    }

    static Tween createDeadTween() {
        var t = createCustomTween(0.1f);
        t.Complete();
        Assert.IsFalse(t.isAlive);
        return t;
    }

    [UnityTest]
    public IEnumerator TweenIsPaused() {
        var val = 0f;
        var t = Tween.Custom(this, 0, 1, 1, (_, newVal) => {
            val = newVal;
        });
        t.isPaused = true;
        yield return null;
        Assert.AreEqual(0, val);
        yield return null;
        Assert.AreEqual(0, val);
        yield return null;
        Assert.AreEqual(0, val);
        t.isPaused = false;
        yield return null;
        Assert.AreNotEqual(0, val);
    }

    [UnityTest]
    public IEnumerator SequenceIsPaused() {
        var val = 0f;
        var t = Tween.Custom(this, 0, 1, 1, (_, newVal) => {
            val = newVal;
        });
        var s = Sequence.Create(t);
        s.isPaused = true;
        yield return null;
        Assert.AreEqual(0, val);
        yield return null;
        Assert.AreEqual(0, val);
        yield return null;
        Assert.AreEqual(0, val);
        s.isPaused = false;
        yield return null;
        Assert.AreNotEqual(0, val);
    }

    const int capacityForTest = 800;

    [UnityTest]
    public IEnumerator TweensCapacity() {
        Tween.StopAll();
        Assert.AreEqual(0, tweensCount);
        PrimeTweenConfig.SetTweensCapacity(capacityForTest);
        Assert.AreEqual(capacityForTest, tweensCapacity);
        PrimeTweenConfig.SetTweensCapacity(0);
        Assert.AreEqual(0, tweensCapacity);
        LogAssert.Expect(LogType.Warning, new Regex("Please increase the capacity"));
        Tween.Delay(0.0001f);
        Tween.Delay(0.0001f); // created before set capacity
        PrimeTweenConfig.SetTweensCapacity(capacityForTest);
        Assert.AreEqual(capacityForTest, tweensCapacity);
        var delay = Tween.Delay(0.0001f);
        yield return delay.ToYieldInstruction(); // should not display warning
        Assert.IsFalse(delay.isAlive);
        yield return null; // the yielded tween is not yet released when the coroutine completes. The release will happen only in a frame
        Assert.AreEqual(0, tweensCount);
        LogAssert.NoUnexpectedReceived();
    }

    static int tweensCapacity => PrimeTweenManager.Instance.currentPoolCapacity;

    [Test]
    public void ListResize() {
        Tween.StopAll();
        Assert.AreEqual(0, tweensCount);
        var list = new List<ColdData>();
        test(2, 2);
        Assert.AreNotEqual(list[0], list[1]);
        test(0, 2);
        test(10, 10);
        test(2, 5);
        test(10, 20);
        test(5,  30);
        test(6, 29);
        test(4, 31);
        test(5, 32);
        test(5, 31);
        test(4, 31);
        test(3, 31);
        test(0, 31);
        test(31, 31);
        Assert.Throws<AssertionException>(() => test(32, 31));
        test(0, 0);
        test(10, 10);
        void test(int newCount, int newCapacity) {
            PrimeTweenManager.ResizeAndSetCapacity(list, newCount, newCapacity);
            Assert.AreEqual(newCount, list.Count);
            Assert.AreEqual(newCapacity, list.Capacity);

            PrimeTweenConfig.SetTweensCapacity(newCapacity);
            Assert.AreEqual(newCapacity, tweensCapacity);
        }
        PrimeTweenConfig.SetTweensCapacity(capacityForTest);
    }

    static ShakeSettings shakeSettings {
        get {
            if (Random.value < 0.5f) {
                return new ShakeSettings(Vector3.one, 1f, 10f, false);
            }
            return new ShakeSettings(Vector3.one, 1f, 10f, false, Ease.Linear);
        }
    }

    [UnityTest]
    public IEnumerator ShakeCompleteWhenStartDelayIsNotElapsed() {
        var target = new GameObject(nameof(ShakeCompleteWhenStartDelayIsNotElapsed)).transform;
        var iniPos = Random.value * Vector3.one;
        target.localPosition = iniPos;
        var t = Tween.ShakeLocalPosition(target, Vector3.one, 0.1f, startDelay: 0.1f);
        yield return null;
        Assert.AreEqual(0f, t.interpolationFactor);
        Assert.AreEqual(iniPos, target.localPosition);
        t.Complete();
        Assert.AreEqual(iniPos, target.localPosition);
    }

    [UnityTest]
    public IEnumerator ShakeScale() {
        var shakeTransform = new GameObject("shake target").transform;
        shakeTransform.position = Vector3.one;
        Assert.AreEqual(shakeTransform.localScale, Vector3.one);
        var t = Tween.ShakeScale(shakeTransform, shakeSettings);
        yield return null;
        Assert.AreNotEqual(shakeTransform.localScale, Vector3.one);
        t.Complete();
        Assert.IsTrue(shakeTransform.localScale == Vector3.one);
        Object.DestroyImmediate(shakeTransform.gameObject);
    }

    [UnityTest]
    public IEnumerator ShakeLocalRotation() {
        var shakeTransform = new GameObject("shake target").transform;
        shakeTransform.position = Vector3.one;
        Assert.AreEqual(shakeTransform.localRotation, Quaternion.identity);
        var t = Tween.ShakeLocalRotation(shakeTransform, shakeSettings);
        yield return null;
        Assert.AreNotEqual(shakeTransform.localRotation, Quaternion.identity);
        t.Complete();
        Assert.IsTrue(shakeTransform.localRotation == Quaternion.identity);
        Object.DestroyImmediate(shakeTransform.gameObject);
    }

    [UnityTest]
    public IEnumerator ShakeLocalPosition() {
        var shakeTransform = new GameObject("shake target").transform;
        shakeTransform.position = Vector3.one;
        Assert.AreEqual(shakeTransform.position, Vector3.one);
        var t = Tween.ShakeLocalPosition(shakeTransform, shakeSettings);
        yield return null;
        Assert.AreNotEqual(shakeTransform.position, Vector3.one);
        t.Complete();
        Assert.IsTrue(shakeTransform.position == Vector3.one, shakeTransform.position.ToString());
        Object.DestroyImmediate(shakeTransform.gameObject);
    }

    [UnityTest]
    public IEnumerator ShakeCustom() {
        var shakeTransform = new GameObject("shake target").transform;
        var iniPos = Vector3.one;
        shakeTransform.position = iniPos;
        Assert.AreEqual(iniPos, shakeTransform.position);
        var t = Tween.ShakeCustom(shakeTransform, iniPos, shakeSettings, (target, val) => target.localPosition = val);
        yield return null;
        Assert.AreNotEqual(iniPos, shakeTransform.position);
        t.Complete();
        Assert.IsTrue(iniPos == shakeTransform.position, iniPos.ToString());
    }

    [UnityTest]
    public IEnumerator CreateShakeWhenTweenListHasNull() {
        Tween.StopAll();
        Assert.AreEqual(0, tweensCount);
        Tween.Delay(0.0001f);
        LogAssert.Expect(LogType.Error, "Shake's strength is (0, 0, 0).");
        LogAssert.Expect(LogType.Error, new Regex("Shake's frequency should be > 0f"));
        Tween.Delay(0.0001f)
            .OnComplete(() => {
                Assert.AreEqual(1, getNullTweensCount());
                Tween.ShakeLocalPosition(transform, default).Complete();
            });
        yield return null;
        yield return null;
        yield return null;
        Assert.AreEqual(0, tweensCount);
    }

    static int getNullTweensCount() => PrimeTweenManager.Instance.tweensCount - Tween.GetTweensCount();

    [UnityTest]
    public IEnumerator DelayNoTarget() {
        int numCallbackCalled = 0;
        var t = Tween.Delay(getDt() * 2f, () => numCallbackCalled++);
        Assert.AreEqual(0, numCallbackCalled);
        while (t.isAlive) {
            yield return null;
        }
        Assert.AreEqual(1, numCallbackCalled);
    }

    [UnityTest]
    public IEnumerator DelayFirstOverload() {
        int numCallbackCalled = 0;
        var t = Tween.Delay(this, getDt() * 3, () => numCallbackCalled++);
        Assert.AreEqual(0, numCallbackCalled);
        while (t.isAlive) {
            yield return null;
        }
        Assert.AreEqual(1, numCallbackCalled);
    }

    [UnityTest]
    public IEnumerator DelaySecondOverload() {
        int numCallbackCalled = 0;
        var t = Tween.Delay(this, getDt() * 3, _ => numCallbackCalled++);
        Assert.AreEqual(0, numCallbackCalled);
        while (t.isAlive) {
            yield return null;
        }
        Assert.AreEqual(1, numCallbackCalled);
    }

    [UnityTest]
    [SuppressMessage("ReSharper", "PossibleNullReferenceException")]
    public IEnumerator NewTweenCreatedFromManualOnComplete() {
        Tween.StopAll();
        Assert.AreEqual(0, tweensCount);
        var t1 = createTween().OnComplete(() => createTween());
        createTween();
        t1.Complete();
        Assert.AreEqual(3, tweensCount);
        checkTweensAreOrdered();
        yield return null;
        Assert.AreEqual(2, tweensCount);
        checkTweensAreOrdered();
        Tween.StopAll();
    }

    static void checkTweensAreOrdered() {
        foreach (var tweens in PrimeTweenManager.Instance.allTweenArrays) {
            TweenData[] arr = new TweenData[tweens.Count];
            foreach (var el in tweens) {
                arr[el.index] = el.tween;
            }
            Assert.IsTrue(arr.OrderBy(x => x.id).SequenceEqual(arr));
        }
    }

    [UnityTest]
    [SuppressMessage("ReSharper", "PossibleNullReferenceException")]
    public IEnumerator NewTweenCreatedFromNormalOnComplete() {
        Tween.StopAll();
        Assert.AreEqual(0, tweensCount);
        int numOnCompleteCalled = 0;
        var t1 = createCustomTween(0.01f).OnComplete(() => {
            numOnCompleteCalled++;
            createCustomTween(0.01f);
            createCustomTween(0.01f);
            createCustomTween(0.01f);
        });
        Assert.AreEqual(1, tweensCount);
        checkTweensAreOrdered();
        while (t1.isAlive) {
            yield return null;
        }
        Assert.AreEqual(1, numOnCompleteCalled);
        Assert.AreEqual(3, tweensCount);
        checkTweensAreOrdered();
    }

    [UnityTest]
    public IEnumerator SetAllPaused() {
        if (tweensCount != 0) {
            var aliveCount = Tween.GetTweensCount();
            Assert.AreEqual(Tween.StopAll(null), aliveCount);
        }
        Assert.AreEqual(0, tweensCount);
        const int count = 10;
        var tweens = new List<Tween>();
        for (int i = 0; i < count; i++) {
            tweens.Add(createCustomTween(1));
        }
        Assert.IsTrue(tweens.All(_ => !_.isPaused));
        Assert.AreEqual(Tween.SetPausedAll(true, null), count);
        Assert.IsTrue(tweens.All(_ => _.isPaused));
        Assert.AreEqual(Tween.SetPausedAll(false, null), count);
        Assert.IsTrue(tweens.All(_ => !_.isPaused));
        Assert.IsTrue(tweens.All(_ => _.isAlive));
        yield return null;
        Assert.IsTrue(tweens.All(_ => _.isAlive));
        foreach (var _ in tweens) {
            _.Complete();
        }
        Assert.IsFalse(tweens.All(_ => _.isAlive));
    }

    [UnityTest]
    public IEnumerator StopAllCalledFromOnValueChange() {
        Tween.StopAll();
        Assert.AreEqual(0, tweensCount);
        int numOnValueChangeCalled = 0;
        var t = Tween.Custom(this, 0, 1, 1f, delegate {
            Assert.AreEqual(0, numOnValueChangeCalled);
            numOnValueChangeCalled++;
            var numStopped = Tween.StopAll(this);
            Assert.AreEqual(1, numStopped);
        });
        Assert.IsTrue(t.isAlive);
        yield return null;
        yield return null;
        Assert.IsFalse(t.isAlive);
        LogAssert.NoUnexpectedReceived();
    }

    [UnityTest]
    public IEnumerator RecursiveCompleteCallFromOnValueChange() {
        int numOnValueChangeCalled = 0;
        int numOnCompleteCalled = 0;
        Tween t = default;
        t = Tween.Custom(this, 0, 1, 1f, delegate {
            // Debug.Log(val);
            numOnValueChangeCalled++;
            Assert.IsTrue(numOnValueChangeCalled <= 2);
            t.Complete();
        }).OnComplete(() => numOnCompleteCalled++);
        Assert.IsTrue(t.isAlive);
        while (t.isAlive) {
            yield return null;
        }
        Assert.IsFalse(t.isAlive);
        Assert.AreEqual(1, numOnCompleteCalled);
        LogAssert.NoUnexpectedReceived();
    }

    [UnityTest]
    public IEnumerator RecursiveCompleteAllCallFromOnValueChange() {
        Tween.StopAll();
        Assert.AreEqual(0, tweensCount);
        int numOnValueChangeCalled = 0;
        int numOnCompleteCalled = 0;
        var t = Tween.Custom(this, 0, 1, 1f, delegate {
            // Debug.Log(val);
            numOnValueChangeCalled++;
            switch (numOnValueChangeCalled) {
                case 1: {
                    var numCompleted = Tween.CompleteAll(this);
                    Assert.AreEqual(1, numCompleted);
                    break;
                }
                case 2: {
                    var numCompleted = Tween.CompleteAll(this);
                    Assert.AreEqual(0, numCompleted);
                    break;
                }
                default:
                    throw new Exception();
            }
        }).OnComplete(() => numOnCompleteCalled++);
        Assert.IsTrue(t.isAlive);
        while (t.isAlive) {
            yield return null;
        }
        Assert.IsFalse(t.isAlive);
        Assert.AreEqual(1, numOnCompleteCalled);
        yield return null;
        Assert.AreEqual(0, tweensCount);
        Assert.AreEqual(2, numOnValueChangeCalled);
        LogAssert.NoUnexpectedReceived();
    }

    [UnityTest]
    public IEnumerator RecursiveCompleteCallFromOnComplete() {
        Tween.StopAll();
        Assert.AreEqual(0, tweensCount);
        int numOnCompleteCalled = 0;
        Tween t = default;
        t = Tween.Custom(this, 0, 1, minDuration, delegate {
        }).OnComplete(() => {
            numOnCompleteCalled++;
            t.Complete();
        });
        Assert.IsTrue(t.isAlive);
        while (t.isAlive) {
            yield return null;
        }
        Assert.IsFalse(t.isAlive);
        Assert.AreEqual(1, numOnCompleteCalled);
        yield return null;
        Assert.AreEqual(0, tweensCount);
    }

    [UnityTest]
    public IEnumerator RecursiveCompleteAllCallFromOnComplete() {
        Tween.StopAll();
        Assert.AreEqual(0, tweensCount);
        int numOnCompleteCalled = 0;
        var t = Tween.Custom(this, 0, 1, minDuration, delegate {
        }).OnComplete(() => {
            numOnCompleteCalled++;
            var numCompleted = Tween.CompleteAll(this);
            Assert.AreEqual(0, numCompleted);
        });
        Assert.IsTrue(t.isAlive);
        while (t.isAlive) {
            yield return null;
        }
        Assert.IsFalse(t.isAlive);
        Assert.AreEqual(1, numOnCompleteCalled);
        yield return null;
        Assert.AreEqual(0, tweensCount);
    }

    [UnityTest]
    public IEnumerator StopAllCalledFromOnValueChange2() {
        int numOnValChangedCalled = 0;
        var t = Tween.Custom(this, 0, 1, 0.0001f, (_, val) => {
            // Debug.Log(val);
            Assert.AreEqual(0, val);
            Assert.AreEqual(0, numOnValChangedCalled);
            numOnValChangedCalled++;
            var numStopped = Tween.StopAll(this);
            Assert.AreEqual(1, numStopped);
        });
        Assert.IsTrue(t.isAlive);
        yield return null;
        Assert.IsFalse(t.isAlive);
        Assert.AreEqual(1, numOnValChangedCalled);
    }

    [UnityTest]
    public IEnumerator TweenCanBeNullInProcessAllMethod() {
        Assert.AreEqual(0, tweensCount);
        Tween.Custom(this, 0, 1, 0.0001f, delegate {
            // Debug.Log($"t1 val {val}");
        });
        Tween.Custom(this, 0, 1, 0.0001f, delegate {
            // Debug.Log($"t2 val {val}");
            Assert.AreEqual(0, getNullTweensCount());
            Assert.AreEqual(Tween.StopAll(this), 2);
        });
        yield return null;
        LogAssert.NoUnexpectedReceived();
    }

    [UnityTest]
    public IEnumerator TweenCanBeNullInOnComplete() {
        Tween.StopAll();
        Assert.AreEqual(0, tweensCount);
        int numOnCompleteCalled = 0;
        Tween.Custom(this, 0, 1, 0.0001f, delegate{});
        Tween.Custom(this, 0, 1, 0.0001f, delegate {
        }).OnComplete(() => {
            numOnCompleteCalled++;
            Assert.AreEqual(1, getNullTweensCount());
            var numStopped = Tween.StopAll(this);
            Assert.AreEqual(0, numStopped);
        });
        yield return null;
        Assert.AreEqual(1, numOnCompleteCalled);
        LogAssert.NoUnexpectedReceived();
    }

    [UnityTest]
    public IEnumerator TweenShouldBeDeadInOnValueChangeAfterCallingComplete() {
        // Debug.Log(nameof(TweenShouldBeDeadInOnValueChangeAfterCallingComplete));
        var target = new GameObject(nameof(TweenShouldBeDeadInOnValueChangeAfterCallingComplete));
        int numOnValueChangeCalled = 0;
        Tween t = default;
        t = Tween.Custom(target, 0, 1, minDuration, (_, val) => {
            // Debug.Log(val);
            Assert.IsTrue(val == 0 || val == 1);
            numOnValueChangeCalled++;
            switch (numOnValueChangeCalled) {
                case 1:
                    Assert.IsTrue(t.isAlive);
                    if (Random.value < 0.5f) {
                        t.Complete();
                    } else {
                        Assert.AreEqual(Tween.CompleteAll(target), 1);
                    }
                    break;
                case 2:
                    // when Complete() is called, it's expected that onValueChange will be reported once again
                    break;
                default: throw new Exception();
            }
        });
        Assert.AreEqual(1, Tween.SetPausedAll(true, target));
        Assert.AreEqual(1, Tween.SetPausedAll(false, target));
        yield return null;
        Assert.IsFalse(t.isAlive);
        Assert.AreEqual(2, numOnValueChangeCalled);
    }

    [Test]
    public void NumProcessed() {
        Tween.StopAll();
        Assert.AreEqual(0, tweensCount);
        var target1 = new object();
        var target2 = new object();

        createWithTarget1(); // 1
        createWithTarget1(); // 2
        createWithTarget1(); // 3
        createWithTarget2();   // 1
        createWithTarget2();   // 2
        createWithTarget1(); // 4
        createWithTarget2();   // 3

        Assert.AreEqual(4, Tween.SetPausedAll(true, target1));
        Assert.AreEqual(4, Tween.SetPausedAll(false, target1));
        Assert.AreEqual(4, Tween.StopAll(target1));
        Assert.AreEqual(0, Tween.StopAll(target1));
        Assert.AreEqual(0, Tween.CompleteAll(target1));
        Assert.AreEqual(0, Tween.SetPausedAll(true, target1));

        Assert.AreEqual(3, Tween.SetPausedAll(true, target2));
        Assert.AreEqual(3, Tween.SetPausedAll(false, target2));
        Assert.AreEqual(3, Tween.CompleteAll(target2));
        Assert.AreEqual(0, Tween.CompleteAll(target2));
        Assert.AreEqual(0, Tween.StopAll(target2));

        void createWithTarget1() => Tween.Custom(target1, 0, 1, 0.0001f, delegate { });
        void createWithTarget2() => Tween.Custom(target2, 0, 1, 0.0001f, delegate { });
    }

    [UnityTest]
    public IEnumerator TweenIsAliveForWholeDuration() {
        int numOnValueChangedCalled = 0;
        int numOnValueChangedCalledAfterComplete = 0;
        Tween t = default;
        var target = new object();
        bool isCompleteCalled = false;
        const float duration = 0.3f;
        t = Tween.Custom(target, 0, 1, duration, (_, val) => {
            // Debug.Log(val);
            numOnValueChangedCalled++;
            if (isCompleteCalled) {
                numOnValueChangedCalledAfterComplete++;
            }
            Assert.AreEqual(!isCompleteCalled, t.isAlive);
            if (val > duration / 2) {
                isCompleteCalled = true;
                t.Complete();
            }
        }).OnComplete(() => {
            Assert.IsTrue(t.IsCreated);
            Assert.IsFalse(t.isAlive);
            Assert.AreEqual(0, Tween.StopAll(target));
        });
        while (t.isAlive) {
            yield return null;
        }
        Assert.IsTrue(numOnValueChangedCalled > 1);
        Assert.AreEqual(1, numOnValueChangedCalledAfterComplete);
    }

    [Test]
    public void SetPauseAll() {
        var target = new object();
        var t = Tween.Custom(target, 0, 1, 1, delegate{});
        Assert.AreEqual(0, Tween.SetPausedAll(false, target));
        Assert.AreEqual(1, Tween.SetPausedAll(true, target));
        Assert.AreEqual(0, Tween.SetPausedAll(true, target));
        Assert.AreEqual(1, Tween.SetPausedAll(false, target));
        Assert.AreEqual(0, Tween.SetPausedAll(false, target));
        t.Stop();
        Assert.AreEqual(0, Tween.SetPausedAll(true, target));
    }

    [UnityTest]
    public IEnumerator StopByTargetFromOnValueChange() {
        var target = new GameObject();
        int numOnValueChangeCalled = 0;
        var t = Tween.Custom(target, 0, 1, 1, delegate {
            numOnValueChangeCalled++;
            var numStopped = Tween.StopAll(target);
            Assert.AreEqual(1, numStopped);
        });
        Assert.AreEqual(0, numOnValueChangeCalled);
        Assert.IsTrue(t.isAlive);
        yield return null;
        Assert.IsFalse(t.isAlive);
        Assert.AreEqual(1, numOnValueChangeCalled);
    }

    [UnityTest]
    public IEnumerator TweenPropertiesDefault() {
        if (tweensCount != 0) {
            Tween.StopAll();
            Assert.AreEqual(0, tweensCount);
        }
        {
            yield return Tween.Delay(0.001f).ToYieldInstruction();
            Assert.AreEqual(1, tweensCount);
            yield return null;
            Assert.AreEqual(0, tweensCount);
        }
        {
            var t = Tween.Delay(0f);
            Assert.IsTrue(t.isAlive);
            validate(t, true);
        }
        {
            var t = new Tween();
            Assert.IsFalse(t.isAlive);
            expectError(false);
            Assert.AreEqual(0, t.cyclesTotal);
            validate(t, false, false);
        }
        {
            var t = Tween.Delay(1);
            t.Complete();
            Assert.IsFalse(t.isAlive);
            expectError();
            Assert.AreEqual(0, t.cyclesTotal);
            validate(t, false);
        }
        {
            var t = Tween.Delay(1);
            t.Stop();
            Assert.IsFalse(t.isAlive);
            expectError();
            Assert.AreEqual(0, t.cyclesTotal);
            validate(t, false);
        }
        void validate(Tween t, bool isAlive, bool isCreated = true) {
            if (!isAlive) {
                for (int i = 0; i < 8; i++) {
                    expectError(isCreated);
                }
            }
            Assert.AreEqual(0, t.elapsedTime);
            Assert.AreEqual(0, t.elapsedTimeTotal);
            Assert.AreEqual(0, t.cyclesDone);
            Assert.AreEqual(0, t.duration);
            Assert.AreEqual(0, t.durationTotal);
            Assert.AreEqual(0, t.progress);
            Assert.AreEqual(0, t.progressTotal);
            Assert.AreEqual(0, t.interpolationFactor);
            if (!isAlive) {
                expectError(isCreated);
            }
            Assert.AreEqual(1, t.timeScale);
        }
        {
            const float duration = 0.123f;
            var t = Tween.PositionY(transform, 0, duration, Ease.Linear, -1);
            Assert.AreEqual(duration, t.duration);
            Assert.IsTrue(float.IsPositiveInfinity(t.durationTotal));
            Assert.AreEqual(0, t.progress);
            Assert.AreEqual(0, t.progressTotal);
            t.Stop();
            validate(t, false);
        }

        void expectError(bool isCreated = true) {
            expectIsDeadError(isCreated);
        }
    }

    [UnityTest]
    public IEnumerator TweenProperties() {
        float duration = Mathf.Max(minDuration, getDt() * Random.Range(0.5f, 5f));
        int numCyclesExpected = Random.Range(1, 3);
        Tween t = default;
        float startDelay = getDt() * Random.Range(0.1f, 1.2f);
        float endDelay = getDt() * Random.Range(0.1f, 1.2f);
        float durationExpected = startDelay + duration + endDelay;
        float totalDurationExpected = durationExpected * numCyclesExpected;
        float timeStart = Time.time;
        t = Tween.Custom(this, 1f, 2f, duration, ease: Ease.Linear, cycles: numCyclesExpected, startDelay: startDelay, endDelay: endDelay, onValueChange: (_, val) => {
            val -= 1f;
            var elapsedTimeTotalExpected = Time.time - timeStart;
            var elapsedTimeExpected = elapsedTimeTotalExpected - durationExpected * t.cyclesDone;
            // Debug.Log($"val: {val}, progress: {t.progress}, elapsedTimeExpected: {elapsedTimeExpected}, elapsedTimeTotalExpected: {elapsedTimeTotalExpected}");
            const float tolerance = 0.001f;
            if (val < 1) {
                Assert.AreEqual(elapsedTimeExpected, t.elapsedTime, tolerance);
                Assert.AreEqual(elapsedTimeTotalExpected, t.elapsedTimeTotal, tolerance, $"val: {val},duration: {duration}, numCyclesExpected: {numCyclesExpected}");
                Assert.AreEqual(Mathf.Min(elapsedTimeTotalExpected / totalDurationExpected, 1f), t.progressTotal, tolerance);
                Assert.AreEqual(elapsedTimeExpected / durationExpected, t.progress, tolerance);
            }
            Assert.AreEqual(numCyclesExpected, t.cyclesTotal);
            Assert.AreEqual(durationExpected, t.duration);
            Assert.AreEqual(totalDurationExpected, t.durationTotal);
            Assert.AreEqual(t.interpolationFactor, val, tolerance);
        });
        yield return t.ToYieldInstruction();
        Assert.IsFalse(t.isAlive);
        for (int i = 0; i < 2; i++) {
            expectIsDeadError();
        }
        Assert.AreEqual(0, t.progress);
        Assert.AreEqual(0, t.progressTotal);

        var infT = Tween.Position(transform, Vector3.one, minDuration, cycles: -1);
        Assert.IsTrue(infT.isAlive);
        Assert.AreEqual(-1, infT.cyclesTotal);
        infT.Complete();
    }

    [UnityTest]
    public IEnumerator ZeroDurationOnTweenShouldReportValueAtLeastOnce() {
        Tween.StopAll();
        Assert.AreEqual(0, tweensCount);
        Assert.AreEqual(capacityForTest, tweensCapacity);

        const float p1 = 0.345f;
        Tween.PositionZ(transform, 0, p1, 0f).Complete();
        UnityEngine.Assertions.Assert.AreApproximatelyEqual(p1, transform.position.z);

        const float p2 = 0.123f;
        Tween.PositionZ(transform, p2, 0).Complete();
        UnityEngine.Assertions.Assert.AreApproximatelyEqual(p2, transform.position.z);

        const float p3 = 0.456f;
        Tween.PositionZ(transform, p3, 0);
        yield return null;
        UnityEngine.Assertions.Assert.AreApproximatelyEqual(p3, transform.position.z);

        yield return Tween.PositionZ(transform, p1, 0).OnComplete(() => { }).ToYieldInstruction();
        UnityEngine.Assertions.Assert.AreApproximatelyEqual(p1, transform.position.z);
    }

    [UnityTest]
    public IEnumerator OneShouldBeReportedExactlyOnce() {
        int numOneReported = 0;
        const int cycles = 1;
        for (int i = 0; i < 1; i++) {
            numOneReported = 0;
            yield return Tween.Custom(this, 0, 1, getDt() * Random.Range(0.5f, 1.5f), startDelay: getDt() * Random.Range(0.1f, 1.1f), endDelay: getDt() * Random.Range(0.5f, 3f), cycles: cycles, onValueChange: (_, val) => {
                // print($"val: {val}");
                if (val == 1f) {
                    numOneReported++;
                }
            }).ToYieldInstruction();
            Assert.AreEqual(cycles, numOneReported);
        }

        numOneReported = 0;
        yield return Tween.Custom(this, 0, 1, 0f, startDelay: getDt() * Random.Range(0.1f, 1.1f), endDelay: getDt() * Random.Range(0.1f, 1.1f), cycles: cycles, onValueChange: (_, val) => {
            if (val == 1) {
                numOneReported++;
            }
        }).ToYieldInstruction();
        Assert.AreEqual(cycles, numOneReported);

        numOneReported = 0;
        yield return Tween.Custom(this, 0, 1, 0f, cycles: cycles, onValueChange: (_, val) => {
            if (val == 1) {
                numOneReported++;
            }
        }).ToYieldInstruction();
        Assert.AreEqual(cycles, numOneReported);

        numOneReported = 0;
        yield return Tween.Custom(this, 0, 1, 0f, (_, val) => {
            if (val == 1) {
                numOneReported++;
            }
        }).ToYieldInstruction();
        Assert.AreEqual(1, numOneReported);

        yield return Tween.PositionY(transform, 3.14f, Mathf.Max(minDuration, getDt() * Random.Range(0.1f, 1.1f))).ToYieldInstruction();
        yield return Tween.PositionY(transform, 3.14f, 0f).ToYieldInstruction();
        yield return Tween.PositionY(transform, 0, 3.14f, 0f).ToYieldInstruction();
    }

    [UnityTest]
    public IEnumerator SingleFrameTween() {
        Application.targetFrameRate = 200;
        for (int i = 0; i < 1; i++) {
            int numOnValueChangeCalled = 0;
            yield return Tween.Custom(this, 0, 1, 0.0001f, (_, val) => {
                numOnValueChangeCalled++;
            Assert.IsTrue(val == 0 || val == 1);
            }).ToYieldInstruction();
            Assert.AreEqual(2, numOnValueChangeCalled);
        }
        Application.targetFrameRate = targetFrameRate;
    }

    [UnityTest]
    public IEnumerator TweensWithDurationOfDeltaTime() {
        for (int i = 0; i < 1; i++) {
            var go = new GameObject();
            go.AddComponent<TweensWithDurationOfDeltaTime>();
            // ReSharper disable once LoopVariableIsNeverChangedInsideLoop
            while (go != null) {
                yield return null;
            }
        }
    }

    [UnityTest]
    public IEnumerator TweenWithExactDurationOfDeltaTime1() {
        yield return Tween.Delay(this, Time.deltaTime).ToYieldInstruction();
    }

    [UnityTest]
    public IEnumerator TweenWithExactDurationOfDeltaTime2() {
        int numOnCompleteCalled = 0;
        yield return Tween.Delay(this, Time.deltaTime, () => numOnCompleteCalled++).ToYieldInstruction();
        Assert.AreEqual(1, numOnCompleteCalled);
    }

    [Test]
    public void TotalDurationWithCycles() {
        var duration = Random.value;
        var startDelay = Random.value;
        var endDelay = Random.value;
        var cycles = Random.Range(1, 10);
        var t = Tween.LocalPositionY(transform, new TweenSettings<float>(0, 1, duration, cycles: cycles, startDelay: startDelay, endDelay: endDelay));
        var durationTotalExpected = duration + startDelay + endDelay;
        const float tolerance = 0.0001f;
        Assert.AreEqual(durationTotalExpected, t.duration, tolerance);
        Assert.AreEqual(durationTotalExpected * cycles, t.durationTotal, tolerance);
        Assert.AreEqual(durationTotalExpected * cycles, t.durationTotal, tolerance);
        t.Complete();
    }

    [Test]
    public void DurationWithWaitDependencies() {
        var t1Dur = Random.value;
        var t1Cycles = Random.Range(1, 20);
        var t2Dur = Random.value;
        var t2Cycles = Random.Range(1, 20);
        // const int t1Dur = 1;
        // const int t1Cycles = 2;
        // const int t2Dur = 2;
        // const int t2Cycles = 2;
        var t1 = Tween.LocalPositionX(transform, 1, t1Dur, cycles: t1Cycles);
        var t2 = Tween.LocalPositionX(transform, 1, t2Dur, cycles: t2Cycles);
        var s = t1.Chain(t2);
        Assert.IsTrue(t1.isAlive);
        Assert.IsTrue(t2.isAlive);
        Assert.AreEqual(t1Dur * t1Cycles, t1.durationTotal);
        Assert.AreEqual(t2Dur * t2Cycles, t2.durationTotal);
        Assert.AreEqual(t1Dur * t1Cycles, t1.durationWithWaitDelay);
        Assert.AreEqual(t1Dur * t1Cycles + t2Dur * t2Cycles, t2.durationWithWaitDelay, 0.001f);
        s.Complete();
    }


    [Test]
    public void AwaitingDeadCompletesImmediately() {
        bool isCompleted = false;
        AwaitingDeadCompletesImmediatelyAsync(() => isCompleted = true);
        Assert.IsTrue(isCompleted);
    }

    static async void AwaitingDeadCompletesImmediatelyAsync([NotNull] Action callback) {
        var frame = Time.frameCount;
        await new Tween();
        await new Sequence();
        Assert.AreEqual(frame, Time.frameCount);
        callback();
    }

    [UnityTest]
    public IEnumerator TestAwaitByCallback() {
        bool isCompleted = false;
        var t = Tween.Delay(getDt() * 5f);
        waitForTweenAsync(t, () => isCompleted = true);
        Assert.IsFalse(isCompleted);
        yield return null;
        Assert.IsFalse(isCompleted);
        yield return t.ToYieldInstruction();
        Assert.IsTrue(isCompleted);
    }

    static async void waitForTweenAsync(Tween tween, [NotNull] Action callback) {
        await tween;
        callback();
    }

    [Test]
    public async Task AwaitTweenWithCallback() {
        bool isCompleted = false;
        var t = Tween.Delay(getDt() * 2f,() => isCompleted = true);
        Assert.IsTrue(t.isAlive);
        Assert.IsTrue(t.tween.managedData.HasOnComplete);
        await t;
        Assert.IsFalse(t.isAlive);
        Assert.IsTrue(isCompleted);
    }

    const float minDuration = TweenSettings.minDuration;

    [Test]
    public async Task AwaitTweenWithCallbackDoesntPostpone() {
        bool isCompleted = false;
        var t = Tween.Delay(minDuration,() => isCompleted = true);
        Assert.IsTrue(t.isAlive);
        Assert.IsTrue(t.tween.managedData.HasOnComplete);
        var frameStart = Time.frameCount;
        await t;
        Assert.AreEqual(1, Time.frameCount - frameStart);
        Assert.IsFalse(t.isAlive);
        Assert.IsTrue(isCompleted);
    }

    [Test]
    public async Task AwaitSequence() {
        bool isCompleted1 = false;
        bool isCompleted2 = false;
        await Sequence.Create(Tween.Delay(0.01f, () => isCompleted1 = true))
            .Chain(Tween.Delay(0.02f, () => isCompleted2 = true));
        Assert.IsTrue(isCompleted1);
        Assert.IsTrue(isCompleted2);
    }

    [Test]
    public async Task AwaitSequence2() {
        var t1 = Tween.Delay(getDt());
        var t2 = Tween.Delay(getDt());
        await t1.Chain(t2);
        Assert.IsFalse(t1.isAlive);
        Assert.IsFalse(t2.isAlive);
    }

    [UnityTest]
    public IEnumerator ToYieldInstruction() {
        var t = Tween.Delay(0.1f);
        var e = t.ToYieldInstruction();
        var frameStart = Time.frameCount;
        while (e.MoveNext()) {
            yield return e.Current;
            t.Complete();
        }
        Assert.AreEqual(1, Time.frameCount - frameStart);
        Assert.IsFalse(t.isAlive);
        yield return t.ToYieldInstruction();

        Tween defaultTween = default;
        defaultTween.ToYieldInstruction();

        Sequence defaultSequence = default;
        defaultSequence.ToYieldInstruction();

        t.Complete();
    }

    [UnityTest]
    public IEnumerator ImplicitConversionToIterator() {
        PrimeTweenConfig.warnStructBoxingAllocationInCoroutine = true;
        {
            var t2 = Tween.Delay(0.0001f);
            var frameStart = Time.frameCount;
            expectCoroutineBoxingWarning();
            yield return t2;
            Assert.AreEqual(1, Time.frameCount - frameStart);
            Assert.IsFalse(t2.isAlive);
        }
        {
            var s = Sequence.Create(Tween.Delay(0.0001f));
            var frameStart = Time.frameCount;
            // iterator boxing warning is shown only once
            yield return s;
            Assert.AreEqual(1, Time.frameCount - frameStart);
            Assert.IsFalse(s.isAlive);
        }
        LogAssert.NoUnexpectedReceived();
    }

    [Test]
    public async Task AwaitInfiniteTweenComplete() {
        Tween t = default;
        int numCompleted = 0;
        t = Tween.Custom(this, 0, 1, 1, cycles: -1, onValueChange: delegate {
                t.Complete();
            })
            .OnComplete(() => numCompleted++);
        await t;
        Assert.AreEqual(1, numCompleted);
    }

    [Test]
    public async Task AwaitInfiniteTweenStop() {
        Tween t = default;
        int numOnValueChanged = 0;
        t = Tween.Custom(this, 0, 1, 1f, cycles: -1, onValueChange: delegate {
            // Debug.Log(numOnValueChanged);
            numOnValueChanged++;
            Assert.AreEqual(1, numOnValueChanged);
            Assert.IsTrue(t.isAlive);
            t.Stop();
        });
        Assert.IsTrue(t.isAlive);
        await t;
        Assert.IsFalse(t.isAlive);
        Assert.AreEqual(1, numOnValueChanged);
    }

    [Test]
    public async Task TweenStoppedTweenWhileAwaiting() {
        var t = Tween.Delay(0.05f);
        #pragma warning disable CS4014
        Tween.Delay(0.01f).OnComplete(() => t.Stop());
        #pragma warning restore CS4014
        Assert.IsTrue(t.isAlive);
        await t;
        Assert.IsFalse(t.isAlive);
    }

    [Test]
    public void InvalidDurations() {
        for (int i = 0; i < 9; i++) {
            LogAssert.Expect(LogType.Error, Constants.durationInvalidError);
        }
        Tween.Delay(float.NaN);
        Tween.Delay(float.PositiveInfinity);
        Tween.Delay(float.NegativeInfinity);
        Tween.PositionZ(transform, new TweenSettings<float>(0, 1, new TweenSettings(1, startDelay: float.NaN)));
        Tween.PositionZ(transform, new TweenSettings<float>(0, 1, new TweenSettings(1, startDelay: float.PositiveInfinity)));
        Tween.PositionZ(transform, new TweenSettings<float>(0, 1, new TweenSettings(1, startDelay: float.NegativeInfinity)));
        Tween.PositionZ(transform, new TweenSettings<float>(0, 1, new TweenSettings(1, endDelay: float.NaN)));
        Tween.PositionZ(transform, new TweenSettings<float>(0, 1, new TweenSettings(1, endDelay: float.PositiveInfinity)));
        Tween.PositionZ(transform, new TweenSettings<float>(0, 1, new TweenSettings(1, endDelay: float.NegativeInfinity)));
    }

    [Test]
    public void MaterialTweens() {
        {
            var s = Shader.Find("Standard");
            if (s == null) {
                Assert.Ignore();
                return;
            }
            var m = new Material(s);

            {
                const string propName = "_EmissionColor";
                #if UNITY_2021_1_OR_NEWER
                Assert.IsTrue(m.HasColor(propName));
                #endif
                var to = Color.red;
                Tween.MaterialColor(m, Shader.PropertyToID(propName), to, 1f).Complete();
                Assert.AreEqual(to, m.GetColor(propName));
            }
            {
                const string propName = "_EmissionColor";
                #if UNITY_2021_1_OR_NEWER
                Assert.IsTrue(m.HasColor(propName));
                #endif
                var iniColor = new Color(Random.value, Random.value, Random.value, Random.value);
                m.SetColor(propName, iniColor);
                var toAlpha = Random.value;
                Tween.MaterialAlpha(m, Shader.PropertyToID(propName), toAlpha, 1f).Complete();
                var col = m.GetColor(propName);
                UnityEngine.Assertions.Assert.AreApproximatelyEqual(col.r, iniColor.r);
                UnityEngine.Assertions.Assert.AreApproximatelyEqual(col.g, iniColor.g);
                UnityEngine.Assertions.Assert.AreApproximatelyEqual(col.b, iniColor.b);
                UnityEngine.Assertions.Assert.AreApproximatelyEqual(col.a, toAlpha);
            }
            {
                const string propName = "_Cutoff";
                #if UNITY_2021_1_OR_NEWER
                Assert.IsTrue(m.HasFloat(propName));
                #endif
                var to = Random.value;
                Tween.MaterialProperty(m, Shader.PropertyToID(propName), to, 1f).Complete();
                UnityEngine.Assertions.Assert.AreApproximatelyEqual(to, m.GetFloat(propName));
            }
            {
                const string propName = "_MainTex";
                #if UNITY_2021_1_OR_NEWER
                Assert.IsTrue(m.HasTexture(propName));
                #endif
                var to = Random.value * Vector2.one;
                Tween.MaterialTextureOffset(m, Shader.PropertyToID(propName), to, 1f).Complete();
                Assert.AreEqual(to, m.GetTextureOffset(propName));
            }
            {
                const string propName = "_MainTex";
                #if UNITY_2021_1_OR_NEWER
                Assert.IsTrue(m.HasTexture(propName));
                #endif
                var to = Random.value * Vector2.one;
                Tween.MaterialTextureScale(m, Shader.PropertyToID(propName), to, 1f).Complete();
                Assert.IsTrue(to == m.GetTextureScale(propName));
            }
        }

        {
            var m = Resources.Load<Material>("Custom_TestShader");
            Assert.IsNotNull(m);
            const string propName = "_TestVectorProp";
            var to = Random.value * Vector4.one;
            int propId = Shader.PropertyToID(propName);
            m.SetVector(propId, default); // this makes the property available via HasVector
            Tween.MaterialProperty(m, propId, to, 1f).Complete();
            Assert.IsTrue(to == m.GetVector(propName));
        }
    }

    /// passing the serialized UnityEngine.Object reference that is not populated behaves like passing destroyed object
    [Test]
    public void PassingDestroyedUnityTarget() {
        LogAssert.NoUnexpectedReceived();

        var target = new GameObject().transform;
        Object.DestroyImmediate(target.gameObject);

        var s = Sequence.Create();
        expectError();
        s.ChainCallback(target, delegate { });

        expectError();
        Assert.IsFalse(Tween.Delay(target, 0.1f, delegate { }).IsCreated);
        expectError();
        Assert.IsFalse(Tween.Delay(target, 0.1f).IsCreated);
        expectError();
        Assert.IsFalse(Tween.Delay(target, 0.1f, () => {}).IsCreated);

        expectError();
        Assert.IsFalse(Tween.Position(target, new TweenSettings<Vector3>(default, default, 0.1f)).IsCreated);

        expectError();
        var deadTween = Tween.Custom(target, 0f, 0, 0.1f, delegate { });
        Assert.IsFalse(deadTween.isAlive);
        expectAddDeadToSequenceError();
        Sequence.Create(deadTween);

        LogAssert.Expect(LogType.Error, "Shake's strength is (0, 0, 0).");
        LogAssert.Expect(LogType.Error, new Regex("Shake's frequency should be > 0f"));
        expectError();
        Tween.ShakeLocalPosition(target, default);

        void expectError() {
            expectTargetIsNull();
        }
    }

    [Test]
    public void ShakeSettings() {
        {
            var s = new ShakeSettings(Vector3.one, 1f, 1);
            Assert.IsTrue(s.enableFalloff);
        }
        {
            var s = new ShakeSettings(Vector3.one, 1f, 1, true, Ease.InBack);
            Assert.IsTrue(s.enableFalloff);
        }
        {
            var s = new ShakeSettings(Vector3.one, 1f, 1, AnimationCurve.Linear(0,0,1,1));
            Assert.IsTrue(s.enableFalloff);
        }
    }

    [UnityTest]
    public IEnumerator ForceCompleteWhenWaitingForEndDelay() {
        var t = Tween.ShakeLocalPosition(transform, new ShakeSettings(Vector3.one, getDt() * 2f, endDelay: 100f));
        while (t.interpolationFactor < 1f) {
            yield return null;
        }
        Assert.IsTrue(t.isAlive);
        t.Complete();
        Assert.IsFalse(t.isAlive);
    }

    static void print(object o) => Debug.Log($"[{Time.frameCount}] {o}");

    [UnityTest]
    public IEnumerator StopAtEvenOrOddCycle() {
        for (int i = 0; i < 5; i++) {
            var t = Tween.Rotation(transform, Vector3.one, getDt() * 5f, cycles: 10, cycleMode: CycleMode.Yoyo);
            while (t.cyclesDone < Random.Range(2, 4)) {
                yield return null;
            }
            t.SetRemainingCycles(true);
            Assert.AreEqual(t.cyclesDone % 2 + 1, t.cyclesTotal - t.cyclesDone);
            t.SetRemainingCycles(false);
            Assert.AreEqual(t.cyclesDone % 2, (t.cyclesTotal - t.cyclesDone) % 2);
        }
    }

    [UnityTest]
    public IEnumerator SetCycles() {
        var t = Tween.Rotation(transform, Vector3.one, Mathf.Max(minDuration, getDt()) * 10, cycles: 10);
        while (t.cyclesDone != 2) {
            yield return null;
        }
        t.SetRemainingCycles(3);
        Assert.AreEqual(2, t.cyclesDone);
        Assert.AreEqual(5, t.cyclesTotal);
        t.Complete();
    }

    [Test]
    public void DOTweenAdapterEnabled() {
        #if !PRIME_TWEEN_DOTWEEN_ADAPTER
        Assert.Ignore("Please add the PRIME_TWEEN_DOTWEEN_ADAPTER define and run all tests again.");
        #endif
    }

    [Test]
    public void ExperimentalDefineSet() {
        #if PRIME_TWEEN_EXPERIMENTAL
        Assert.Ignore("Please remove the PRIME_TWEEN_EXPERIMENTAL define and run all tests again.");
        #endif
    }

    [UnityTest]
    public IEnumerator RecursiveKillAllCall() {
        // Calling Tween.StopAll/Complete() from onValueChange previously threw the 'Please don't call Tween.StopAll/CompleteAll() from the OnComplete() callback' exception before.
        // But this is no longer the case - current impl checks if Update/FixedUpdate() is safe to call
        yield return Tween.Custom(0, 1, 1f, val => {
            if (val != 0) {
                Tween.StopAll();
            }
        }).ToYieldInstruction();

        yield return Tween.Custom(0, 1, 0.01f, _ => {
            Tween.CompleteAll();
        }).ToYieldInstruction();
        LogAssert.NoUnexpectedReceived();
    }

    [UnityTest]
    public IEnumerator KillAllIsImmediate() {
        Tween.Delay(0.01f);
        Tween.StopAll(null);
        Assert.AreEqual(0, tweensCount);
        Tween.Delay(0.01f);
        Assert.AreEqual(1, tweensCount);
        Assert.AreEqual(Tween.CompleteAll(null), 1);
        Assert.AreEqual(0, tweensCount);
        yield return null;
        LogAssert.NoUnexpectedReceived();
    }

    [Test]
    public void SetCapacityImmediatelyAfterStopAll() {
        Tween.StopAll();
        Assert.AreEqual(0, tweensCount);
        PrimeTweenConfig.SetTweensCapacity(2);
        Tween.Delay(0.01f);
        Tween.Delay(0.01f);
        Assert.AreEqual(2, tweensCount);
        Tween.StopAll();
        Assert.AreEqual(0, tweensCount);
        PrimeTweenConfig.SetTweensCapacity(1);
        PrimeTweenConfig.SetTweensCapacity(capacityForTest);
        LogAssert.NoUnexpectedReceived();
    }

    [UnityTest]
    public IEnumerator RotationShakeEndVal() {
        var target = new GameObject(nameof(RotationShakeEndVal)).transform;
        var iniRot = Random.rotation.normalized;
        var strength = Random.insideUnitSphere;
        var freq = Random.value * 10;
        target.rotation = iniRot;
        var handle = Tween.ShakeLocalRotation(target, strength, 1f, freq);
        // skip frame so CacheDiff() is called
        yield return null;
        Assert.IsTrue(handle.isAlive);
        var t = handle.tween;
        Assert.IsTrue(iniRot == t.data.startValue.quaternion);
        Assert.IsTrue(Quaternion.identity == t.managedData.endValueOrDiff.quaternion);
        Assert.AreEqual(strength.x, t.shakeData.strengthPerAxis.x);
        Assert.AreEqual(strength.y, t.shakeData.strengthPerAxis.y);
        Assert.AreEqual(strength.z, t.shakeData.strengthPerAxis.z);
        Assert.AreEqual(freq, t.shakeData.frequency);
        Object.Destroy(target.gameObject);
        handle.Stop();
    }

    [UnityTest]
    public IEnumerator AtSpeed() {
        var target = new GameObject(nameof(AtSpeed)).transform;
        var speed = (Random.value + 0.1f) * 10f;
        const double tolerance = 0.001;
        var endValue = new Vector3(1, 0,0);
        {
            Assert.AreEqual(Vector3.zero, target.position);
            var t = Tween.PositionAtSpeed(target, endValue, speed);
            Assert.AreEqual(speed, 1 / t.duration, tolerance);
            yield return null;
            Assert.AreEqual(speed, 1 / t.duration, tolerance);
            t.Stop();
        }
        {
            target.position = Vector3.zero;
            Tween.PositionAtSpeed(target, endValue, speed).Complete();
            Assert.AreEqual(endValue, target.position);
        }
        {
            target.position = Vector3.zero;
            float startDelay = getDt();
            var t = Tween.PositionAtSpeed(target, endValue, speed, startDelay: startDelay);
            var expectedDuration = 1 / speed + startDelay;
            while (t.interpolationFactor == 0) {
                Assert.AreEqual(expectedDuration, t.duration, tolerance);
                yield return null;
            }
            Assert.AreEqual(expectedDuration, t.duration, tolerance);
            yield return null;
            Assert.AreEqual(expectedDuration, t.duration, tolerance);
            t.Stop();
        }
    }

    [UnityTest]
    public IEnumerator DelayInterpolationFactor() {
        for (int i = 0; i < 1; i++) {
            float duration = Random.Range(0.001f, 0.1f);
            var d = Tween.Delay(duration);
            float timeStart = Time.time;
            while (d.isAlive) {
                float expected = Mathf.Min(1f, (Time.time - timeStart) / duration);
                if (Math.Abs(expected - d.interpolationFactor) > 0.001f) {
                    Debug.LogError($"expected {expected}, got {d.interpolationFactor}, {Time.frameCount}");
                } else {
                    // Debug.Log($"ok {d.interpolationFactor}");
                }
                yield return null;
            }
        }
    }

    [Test]
    public void TweensCount() {
        Tween.StopAll();
        int count = Random.Range(1, 10);
        for (int i = 0; i < count; i++) {
            Tween.PositionX(transform, 10, 0.01f);
        }
        Assert.AreEqual(count, Tween.GetTweensCount());
        Assert.AreEqual(count, Tween.GetTweensCount(transform));
        Tween.StopAll();
        Assert.AreEqual(0, Tween.GetTweensCount());
    }

    [UnityTest]
    public IEnumerator StopCalledOnLastTweenFrame() {
        float dt = getDt();
        Tween tween = default;
        tween = Tween.Custom(0, 1, dt * 3, val => {
            if (val == 1f) {
                tween.Stop();
                Assert.IsFalse(tween.isAlive);
            }
        }).OnComplete(() => Assert.Fail());
        yield return tween.ToYieldInstruction();
    }

    [UnityTest]
    public IEnumerator StopCalledOnLastTweenFrameFromOnUpdate() {
        Tween tween = default;
        float duration = getDt() * 3f;
        tween = Tween.Custom(0, 1, duration, delegate { })
            .OnUpdate(this, delegate {
                if (tween.interpolationFactor == 1f) {
                    tween.Stop();
                    Assert.IsFalse(tween.isAlive);
                }
            })
            .OnComplete(() => Assert.Fail());
        yield return tween.ToYieldInstruction();
    }

    [UnityTest]
    public IEnumerator OnUpdateDestroyedTarget() {
        var onUpdateTarget = new GameObject(nameof(OnUpdateDestroyedTarget));
        int numUpdated = 0;
        expectOnUpdateRemoved();
        yield return Tween.Delay(getDt() * 5)
            .OnUpdate(onUpdateTarget, (target, _) => {
                Assert.AreEqual(0, numUpdated);
                numUpdated++;
                Object.Destroy(target);
            })
            .ToYieldInstruction();
        Assert.AreEqual(1, numUpdated);
        LogAssert.NoUnexpectedReceived();
    }

    [UnityTest]
    public IEnumerator OnUpdateException() {
        var onUpdateTarget = new GameObject(nameof(OnUpdateDestroyedTarget));
        expectOnUpdateRemoved();
        LogAssert.Expect(LogType.Exception, new Regex("Exception"));
        int numCompleted = 0;
        yield return Tween.PositionZ(transform, Random.value, 0.001f)
            .OnUpdate(onUpdateTarget, delegate { throw new Exception(); })
            .OnComplete(() => numCompleted++)
            .ToYieldInstruction();
        Assert.AreEqual(1, numCompleted);
        LogAssert.NoUnexpectedReceived();
    }

    [Test]
    public void OnUpdateInvalidUsage() {
        var t = Tween.Delay(0.001f);
        t.OnUpdate<object>(null, delegate { }); // ok
        expectException<AssertionException>(() => {
            t.OnUpdate<object>(null, delegate { }); // duplicate is not allowed
        }, "Only one OnUpdate() is allowed for one tween.");
        Assert.Throws<AssertionException>(() => {
            t.OnUpdate(this, null); // null onUpdate is not allowed
        });
    }

    [UnityTest]
    public IEnumerator OnUpdate() {
        {
            // with delay
            int numCalled = 0;
            yield return Tween.Delay(minDuration, () => numCalled++).ToYieldInstruction();
            Assert.AreEqual(1, numCalled);
        }
        {
            var t = Tween.Position(transform, Vector3.one, getDt(), endDelay: getDt() * 5);
            int numInterpolationCompleted = 0;
            yield return t.OnUpdate<object>(null, delegate {
                if (t.interpolationFactor == 1f) {
                    numInterpolationCompleted++;
                }
            }).ToYieldInstruction();
            Assert.AreEqual(1, numInterpolationCompleted);
        }
    }

    static void expectOnUpdateRemoved() => LogAssert.Expect(LogType.Error, new Regex("will not be called again because"));

    [UnityTest]
    public IEnumerator TimescaleTweenOutliveTheTarget() {
        var shortTween = Tween.Delay(0.001f);
        var timeScaleTween = Tween.TweenTimeScale(shortTween, 0.5f, 1)
            .OnComplete(Assert.Fail);
        expectOnCompleteIgnored();
        yield return shortTween.ToYieldInstruction();
        Assert.IsFalse(timeScaleTween.isAlive);
        LogAssert.NoUnexpectedReceived();
    }

    /*[UnityTest]
    public IEnumerator ConversionToEasing() {
        var curve = AnimationCurve.Linear(0, 0, 1, 1);
        new TweenSettings(1, curve);
        new TweenSettings<float>(0, 1, curve);
        new TweenSettings<float>(0, 1, 1, curve);
        Tween.Position(transform, Vector3.one, 1, curve);
        Tween.Position(transform, Vector3.one, 1, (Easing)Ease.InBack);
        Tween.Position(transform, Vector3.zero, Vector3.one, 1, curve);
        #if PRIME_TWEEN_EXPERIMENTAL
        Tween.PositionAdditive(transform, Vector3.one, 1, curve);
        #endif
        Tween.PositionAtSpeed(transform, Vector3.one, 10, curve);
        Tween.PositionAtSpeed(transform, Vector3.zero, Vector3.one, 10, curve);
        yield return null;
        Tween.StopAll();
    }*/

    /*[UnityTest]
    public IEnumerator Elastic() {
        for (int i = 0; i < 5; i++) {
            Assert.AreEqual(0, Easing.EvaluateParametricEase(0, parametricSettings(ParametricEase.Elastic, 1, 0.3f)));
            Assert.AreEqual(1, Easing.EvaluateParametricEase(1, parametricSettings(ParametricEase.Elastic, 1, 0.3f)));
            var strength = 1 + Random.value;
            var period = 0.1f + Random.value;
            var t = Tween.Position(transform, Vector3.one, 1, Easing.Elastic(strength, period));
            yield return null;
            Assert.IsTrue(t.isAlive);
            Assert.AreEqual(strength, t.tween.parametricEaseStrength);
            Assert.AreEqual(period, t.tween.parametricEasePeriod);
            Assert.AreEqual(Ease.Custom, t.tween.ease);
            Assert.AreEqual(ParametricEase.Elastic, t.tween.parametricEase);
            t.Stop();
        }
    }

    [NotNull]
    static TweenData parametricSettings(ParametricEase ease, float strength, float period) {
        return new TweenData {
            ease = Ease.Custom,
            animationDuration = 1,
            parametricEase = ease,
            parametricEaseStrength = strength,
            parametricEasePeriod = period
        };
    }

    [UnityTest]
    public IEnumerator Overshoot() {
        Assert.AreEqual(0, Easing.EvaluateParametricEase(0, parametricSettings(ParametricEase.Overshoot, 1, float.NaN)));
        Assert.AreEqual(1, Easing.EvaluateParametricEase(1, parametricSettings(ParametricEase.Overshoot, 1, float.NaN)));
        var strength = Random.value;
        var t = Tween.Position(transform, Vector3.one, 1, Easing.Overshoot(strength));
        yield return null;
        Assert.IsTrue(t.isAlive);
        Assert.AreEqual(strength * StandardEasing.backEaseConst, t.tween.parametricEaseStrength);
        Assert.AreEqual(ParametricEase.Overshoot, t.tween.parametricEase);
        Assert.AreEqual(Ease.Custom, t.tween.ease);
        t.Stop();
    }*/

    [UnityTest]
    [SuppressMessage("ReSharper", "AssignNullToNotNullAttribute")]
    public IEnumerator NullTarget() {
        const float duration = 0.05f;
        Transform t = null;

        expectTargetIsNull();
        Tween.Position(t, Vector3.one, duration).ToYieldInstruction();

        expectTargetIsNull();
        Tween.Delay(null, duration);

        expectTargetIsNull();
        Tween.Delay(t, duration, delegate { });

        expectTargetIsNull();
        Sequence.Create().ChainCallback(t, delegate { });

        Tween.Custom(0, 1, duration, delegate { });
        Tween.Delay(duration);

        // Time.timeScale = 0.99f;
        // Tween.GlobalTimeScale(1, 0.05f);

        Sequence.Create().ChainCallback(delegate { });
        #if PRIME_TWEEN_DOTWEEN_ADAPTER
        DOTween.Sequence().SetLoops(2).AppendCallback(delegate {});
        DOTween.Sequence().PrependInterval(0.01f);
        #endif
        yield return null;
        yield return null;
        Tween.StopAll();
        LogAssert.NoUnexpectedReceived();
    }

    [UnityTest]
    public IEnumerator MoreThanOneCyclePerFrame() {
        yield return null;
        {
            var zeroDurTween = Tween.ScaleY(transform, 2f, 0, cycles: int.MaxValue - 1);
            yield return null;
            Assert.IsFalse(zeroDurTween.isAlive, "0f duration completes all cycles immediately");
        }
        {
            var infiniteZeroDurTween = Tween.ScaleY(transform, 2f, 0, cycles: -1);
            yield return null;
            Assert.AreEqual(1, infiniteZeroDurTween.cyclesDone);
            Assert.IsTrue(infiniteZeroDurTween.isAlive);
        }
        {
            float duration = 0.001f + getDt() / 10f;
            Assert.IsTrue(duration >= 0.001f);
            var t = Tween.ScaleY(transform, 2f, duration, cycles: int.MaxValue - 1);
            yield return null;
            Assert.AreEqual(Mathf.FloorToInt(Time.deltaTime / duration), t.cyclesDone);
            t.Complete();
        }
    }

    [UnityTest]
    public IEnumerator ExecutionOrder() {
        yield return null;
        int i = 0;
        void validate(int order) {
            // Debug.Log($"-------------- validate {order}");
            Assert.AreEqual(i, order);
            i++;
        }
        {
            for (int k = 0; k < 1; k++) {
                yield return Sequence.Create(2)
                    .Chain(Tween.Custom(0, 1, getDur(), delegate { }, startDelay: getDur(), endDelay: getDur()).OnComplete(() => validate(0)))
                    .ChainCallback(() => validate(1))
                    .Chain(Tween.Delay(getDur(), () => validate(2)))
                    .Chain(Tween.Custom(0, 1, getDur(), delegate { }, startDelay: getDur(), endDelay: getDur()).OnComplete(() => validate(3)))
                    .ChainCallback(() => validate(4))
                    .ChainCallback(() => validate(5))
                    .ChainCallback(() => {
                        validate(6);
                        i = 0;
                    })
                    .ToYieldInstruction();
            }
        }
        {
            for (int k = 0; k < 1; k++) {
                i = 0;
                var seq = Sequence.Create();
                const int iterations = 5;
                for (int j = 0; j < iterations; j++) {
                    var expected = j;
                    if (Random.value > 0.5f) {
                        seq.Chain(Tween.Delay(getDur(), () => validate(expected)));
                    } else {
                        seq.ChainCallback(() => validate(expected));
                    }
                }
                seq.ChainCallback(() => {
                    validate(iterations);
                    i = 0;
                });
                seq.SetRemainingCycles(2);
                yield return seq.ToYieldInstruction();
            }
        }

        float getDur() => Mathf.Max(minDuration, getDt() * Mathf.Lerp(0.1f, 2f, Random.value));
    }

    static float getDt() => Application.targetFrameRate != -1 ? 1f / Application.targetFrameRate : Time.deltaTime;

    [Test]
    public void ShakeIsDeadOnNewReusableTween() {
        var t = new ColdData();
        Assert.IsFalse(t.shakeData.isAlive);
    }

    [UnityTest]
    public IEnumerator SmallDurationWithInfiniteCycles() {
        int numChanged = 0;
        int numUpdated = 0;
        const float duration = 0.001f;
        var t = Tween.Custom(0f, 1f, duration, cycles: -1, onValueChange: delegate {
                numChanged++;
                // print("onValueChange");
            })
            .OnUpdate(this, delegate {
                numUpdated++;
                Assert.AreEqual(numChanged, numUpdated);
                // print("OnUpdate()");
            });
        float timeStart = Time.time;
        yield return null;
        yield return null;
        yield return null;
        Assert.AreEqual(4, numChanged);
        Assert.AreEqual(4, numUpdated);
        var cyclesDoneExpected = Mathf.FloorToInt((Time.time - timeStart) / duration);
        Assert.AreEqual(cyclesDoneExpected, t.cyclesDone);
        t.Complete();
    }

    [UnityTest]
    public IEnumerator TweenArrayLock_InvalidOperationExceptionInsideCompleteAllBug() {
        for (int i = 0; i < 3; i++) {
            const float upper = 1.5f;
            Tween.Delay(getDt() * Random.Range(0.5f, upper), () => Tween.Delay(getDt() * Random.Range(0.5f, 1.5f)));
            Tween.Delay(getDt() * Random.Range(0.5f, upper), () => Tween.Delay(getDt() * Random.Range(0.5f, 1.5f)));
            Tween.Delay(getDt() * Random.Range(0.5f, upper), () => Tween.Delay(getDt() * Random.Range(0.5f, 1.5f)));
            Tween.Delay(getDt() * Random.Range(0.5f, upper), () => Tween.Delay(getDt() * Random.Range(0.5f, 1.5f)));
            Tween.Delay(getDt() * Random.Range(0.5f, upper), () => Tween.Delay(getDt() * Random.Range(0.5f, 1.5f)));
            int delayFramesBeforeCompleteAll = Random.Range(0, 2);
            for (int j = 0; j < delayFramesBeforeCompleteAll; j++) {
                yield return null;
            }
            if (Random.value >= 0.5f) {
                Tween.CompleteAll();
            }
        }
        yield return null;
        Tween.CompleteAll();
        Assert.AreEqual(0, tweensCount);
    }

    [Test]
    public void TweenArrayLock_CreateNewTweenOnCompleteWithCompleteAll() {
        Tween.StopAll();
        Assert.AreEqual(0, tweensCount);
        Tween.Delay(minDuration).OnComplete(() => {
            Tween.Delay(minDuration);
        });
        Assert.AreEqual(1, tweensCount);
        Tween.CompleteAll();
        Assert.AreEqual(0, tweensCount);
    }

    [UnityTest]
    public IEnumerator TweenArrayLock_CreateNewTweenOnCompleteWithCompleteAll2() {
        var t = Tween.Delay(minDuration)
            .OnComplete(() => {
                Tween.Delay(minDuration).OnComplete(() => {
                    Tween.CompleteAll();
                });
                Tween.CompleteAll();
            });
        yield return null;
        Assert.IsFalse(t.isAlive);
    }

    [UnityTest]
    public IEnumerator TweenArrayLock_CreateNewTweenOnOnValueChangeWithCompleteAll() {
        Tween.Custom(0f, 1f, 1f, val => {
            Tween.Delay(minDuration);
            Tween.CompleteAll();
        });
        yield return null;
    }

    [UnityTest]
    public IEnumerator TweenArrayLock_CreateNewTweenOnOnValueChangeWithStopAll() {
        Tween.Custom(0f, 1f, 1f, val => {
            Tween.Delay(minDuration);
            Tween.StopAll();
        });
        yield return null;
    }

    [UnityTest]
    public IEnumerator TweenElapsedTimeTotal() {
        var duration = getDt() * 2f;
        var t = Tween.Custom(0f, 1f, duration, delegate { });
        var s = Sequence.Create(2)
            .ChainDelay(duration)
            .Chain(t)
            .ChainDelay(duration);
        while (s.isAlive) {
            Assert.IsTrue(t.elapsedTimeTotal >= 0f);
            Assert.IsTrue(t.elapsedTimeTotal <= t.durationTotal);
            yield return null;
        }
    }

    [Test]
    public void SetCyclesWithDelay() {
        LogAssert.Expect(LogType.Error, new Regex("Applying cycles to Delay will not repeat"));
        var delay = Tween.Delay(this, duration: .5f, delegate {});
        delay.SetRemainingCycles(10);
        delay.Stop();
    }

    [Test]
    public void ElapsedTimeTotalIsClamped() {
        for (int i = 0; i < 1; i++) {
            float duration = Random.value + 0.001f;
            int cycles = Random.Range(3, 20);
            var t = Tween.Delay(duration);
            t.SetRemainingCycles(cycles);
            t.isPaused = true;

            t.elapsedTime = float.MaxValue;
            Assert.AreEqual(1, t.cyclesDone);
            Assert.AreEqual(duration, t.tween.data.elapsedTimeTotal);
            Assert.AreEqual(0f, t.elapsedTime); // new cycles has started, so should be 0f
            Assert.AreEqual(duration, t.elapsedTimeTotal);

            t.elapsedTimeTotal = 0f;
            Assert.AreEqual(0, t.cyclesDone);
            Assert.AreEqual(0f, t.tween.data.elapsedTimeTotal);
            Assert.AreEqual(0f, t.elapsedTime);
            Assert.AreEqual(0f, t.elapsedTimeTotal);

            t.elapsedTimeTotal = float.MaxValue;
            Assert.AreEqual(cycles, t.cyclesDone);
            Assert.AreEqual(t.durationTotal, t.tween.data.elapsedTimeTotal);
            Assert.AreEqual(t.duration, t.elapsedTime);
            Assert.AreEqual(t.durationTotal, t.elapsedTimeTotal);

            Assert.IsTrue(t.isAlive);
            t.Stop();
        }
    }

    [UnityTest]
    public IEnumerator WarnEndValueEqualsCurrent() {
        Assert.AreEqual(0, tweensCount);
        var iniPos = Vector3.one * Random.value;

        {
            transform.position = iniPos;
            var t = Tween.Position(transform, iniPos, 1f);
            yield return null;
            LogAssert.Expect(LogType.Warning, new Regex("Tween's 'endValue' equals to the current animated value"));
            t.Stop();
        }

        {
            PrimeTweenConfig.warnEndValueEqualsCurrent = false;
            transform.position = iniPos;
            var t = Tween.Position(transform, iniPos, 1f);
            yield return null;
            t.Stop();
            PrimeTweenConfig.warnEndValueEqualsCurrent = true;
        }

        LogAssert.NoUnexpectedReceived();
    }

    [Test]
    public void SetInfiniteTweenElapsedTime() {
        var t = Tween.Custom(0f, 1f, 0.01f, delegate { }, cycles: -1);
        for (int i = 0; i < 8; i++) {
            LogAssert.Expect(LogType.Error, new Regex("Invalid elapsedTime"));
        }
        t.elapsedTime = float.MaxValue; // ok
        t.elapsedTime = float.PositiveInfinity; // ok
        t.elapsedTime = -1f;
        t.elapsedTime = float.NegativeInfinity;
        t.elapsedTime = float.NaN;

        t.elapsedTimeTotal = 0f; // ok
        t.elapsedTimeTotal = 10f; // ok
        t.elapsedTimeTotal = -1f;
        t.elapsedTimeTotal = float.MaxValue;
        t.elapsedTimeTotal = float.PositiveInfinity;
        t.elapsedTimeTotal = float.NegativeInfinity;
        t.elapsedTimeTotal = float.NaN;
        t.Stop();
    }

    [Test]
    public void SetInfiniteTweenProgress() {
        var t = Tween.Custom(0f, 1f, 0.01f, delegate { }, cycles: -1);
        Assert.IsTrue(float.IsPositiveInfinity(t.durationTotal));

        // ok
        t.progress = 0f;
        t.progress = 0.5f;
        t.progress = 1f;

        for (int i = 0; i < 3; i++) {
            LogAssert.Expect(LogType.Error, new Regex("It's not allowed to set progressTotal on infinite tween"));
        }
        t.progressTotal = 0f;
        t.progressTotal = 0.5f;
        t.progressTotal = 1f;
        Assert.IsTrue(float.IsPositiveInfinity(t.durationTotal));
        t.Stop();
    }

    [UnityTest]
    public IEnumerator ZZ_SceneLoadSetsUnityObjectBug1() {
        var t = Tween.Delay(1f);
        LoadTestScene();
        yield return null;
        Assert.IsTrue(t.isAlive);
        t.Stop();
    }

    [UnityTest]
    public IEnumerator ZZ_SceneLoadSetsUnityObjectBug2() {
        var seq = Sequence.Create().ChainDelay(1f);
        LoadTestScene();
        yield return null;
        Assert.IsTrue(seq.isAlive);
        seq.Stop();
    }

    static void LoadTestScene() {
        const string testScenePath = "Packages/com.kyrylokuzyk.primetween/Tests/SceneLoadSetsUnityObjectBug.unity";
        #if UNITY_EDITOR
        if (Application.isEditor) {
            UnityEditor.SceneManagement.EditorSceneManager.LoadSceneInPlayMode(testScenePath, new LoadSceneParameters(LoadSceneMode.Single));
            return;
        }
        #endif
        SceneManager.LoadScene(testScenePath, LoadSceneMode.Single);
    }

    [UnityTest]
    public IEnumerator SetElapsedTimeRecursively() {
        Tween t = default;
        int i = 0;
        t = Tween.Custom(0f, 1f, getDt() / 5f, delegate {
                // print("custom");
                onUpdate();
            })
            .OnUpdate(this, delegate {
                // print("OnUpdate");
                onUpdate();
            });
        yield return t.ToYieldInstruction();

        void onUpdate() {
            i++;
            Assert.IsTrue(i < 100);
            expectRecursiveCallError();
            t.progress += 0.01f;
            expectRecursiveCallError();
            t.progressTotal += 0.01f;
            expectRecursiveCallError();
            t.elapsedTime += 0.01f;
            expectRecursiveCallError();
            t.elapsedTimeTotal += 0.01f;
        }
    }

    [UnityTest]
    public IEnumerator CompleteInfiniteTween() {
        int numCallbacks = 0;
        var duration = Mathf.Max(minDuration, Random.Range(0f, getDt()));
        Assert.IsTrue(duration >= minDuration);
        var t = Tween.Position(transform, Vector3.one, duration, cycles: -1)
            .OnComplete(() => numCallbacks++);
        float timeStart = Time.time;
        yield return null;
        yield return null;
        var cyclesDoneExpected = Mathf.FloorToInt((Time.time - timeStart) / duration);
        Assert.AreEqual(cyclesDoneExpected, t.cyclesDone);
        t.Complete();
        Assert.AreEqual(1, numCallbacks);
    }

    [Test]
    public void CompleteInfiniteTween2() {
        var target = new GameObject().transform;
        var iniValue = Vector3.one * 0.5f;
        target.localScale = iniValue;
        var endValue = Vector3.one * 2f;
        Tween.Scale(target, endValue, 0.5f, cycles: -1, cycleMode: CycleMode.Yoyo).Complete();
        Assert.AreEqual(endValue, target.localScale);
        {
            var t = Tween.Scale(target, iniValue, 0.5f, cycles: -1, cycleMode: CycleMode.Yoyo);
            t.SetRemainingCycles(true);
            t.Complete();
            Assert.AreEqual(iniValue, target.localScale);
        }
        {
            var t = Tween.Scale(target, endValue, 0.5f, cycles: -1, cycleMode: CycleMode.Yoyo);
            t.SetRemainingCycles(false);
            t.Complete();
            Assert.AreEqual(iniValue, target.localScale);
        }
        Object.DestroyImmediate(target.gameObject);
        LogAssert.NoUnexpectedReceived();
    }

    [UnityTest]
    public IEnumerator WarnEndValueEqualsCurrent2() {
        Tween.StopAll();
        yield return null;
        {
            transform.position = Vector3.one;
            expectWarning();
            var t = Tween.Position(transform, Vector3.one, 0.01f);
            yield return null;
            t.Stop();
        }
        {
            transform.position = Vector3.one;
            PrimeTweenConfig.warnEndValueEqualsCurrent = false;
            var t = Tween.Position(transform, Vector3.one, 0.01f);
            PrimeTweenConfig.warnEndValueEqualsCurrent = true;
            yield return null;
            t.Stop();
        }

        LogAssert.NoUnexpectedReceived();
        void expectWarning() => LogAssert.Expect(LogType.Warning, new Regex("Tween's 'endValue' equals to the current animated value"));
    }

    [UnityTest]
    public IEnumerator SetPropertiesOfInfiniteTween() {
        var t = Tween.Custom(0, 1, 1, _ => {
            // print(_);
        }, cycles: -1, cycleMode: CycleMode.Yoyo, ease: Ease.Linear);
        t.elapsedTime = 0.5f;
        yield return null;
        t.progress = 0.75f;
        yield return null;
        t.elapsedTimeTotal = 2.1f;
        yield return null;
        t.elapsedTime = 0.5f;
        yield return null;
        t.Stop();
    }

    [Test]
    public void NegativeIniCyclesBugReport() {
        // https://github.com/KyryloKuzyk/PrimeTween/issues/63
        Tween.StopAll();
        var startValue = Vector3.one * 10;
        var endValue = Vector3.one * 20;
        transform.position = startValue;
        var posTween = Tween.Position(transform, new TweenSettings<Vector3>(startValue, endValue, 1f, cycles: 2, cycleMode: CycleMode.Rewind));
        var seq = Sequence.Create()
            .ChainDelay(1f)
            .Chain(posTween);
        seq.elapsedTime = 1.1f;
        Assert.AreNotEqual(0f, posTween.interpolationFactor);
        Assert.AreNotEqual(startValue, transform.position);
        // Assert.AreEqual(-1, posTween.tween.cyclesDone);
        seq.elapsedTime = 0.5f;
        Assert.AreEqual(0f, posTween.interpolationFactor);
        Assert.AreEqual(startValue, transform.position);
        seq.Stop();
    }

    [Test]
    public void UpdateTypes() {
        var iniUpdateType = PrimeTweenConfig.defaultUpdateType;
        {
            Tween.StopAll();
            var t = Tween.Position(transform, new TweenSettings<Vector3>(default, 1f, updateType: default)).tween;
            Assert.AreEqual(_UpdateType.Update, t.data.updateType);
            Assert.AreEqual(t, PrimeTweenManager.Instance.newTweensUpdate.Single());
        }
        {
            Tween.StopAll();
            var t = Tween.Position(transform,  new TweenSettings<Vector3>(default, 1f, updateType: UpdateType.Update)).tween;
            Assert.AreEqual(_UpdateType.Update, t.data.updateType);
            Assert.AreEqual(t, PrimeTweenManager.Instance.newTweensUpdate.Single());
        }
        {
            Tween.StopAll();
            var t = Tween.Position(transform, new TweenSettings<Vector3>(default, 1f, updateType: UpdateType.LateUpdate)).tween;
            Assert.AreEqual(_UpdateType.LateUpdate, t.data.updateType);
            Assert.AreEqual(t, PrimeTweenManager.Instance.newTweensLateUpdate.Single());
        }
        {
            Tween.StopAll();
            var t = Tween.Position(transform, new TweenSettings<Vector3>(default, 1f, updateType: UpdateType.FixedUpdate)).tween;
            Assert.AreEqual(_UpdateType.FixedUpdate, t.data.updateType);
            Assert.AreEqual(t, PrimeTweenManager.Instance.newTweensFixedUpdate.Single());
        }
        {
            Tween.StopAll();
            Assert.AreEqual(0, tweensCount);
            UpdateType updateType = default;
            updateType.enumValue = (_UpdateType)100;
            Tween.Position(transform, new TweenSettings<Vector3>(default, 1f, updateType: updateType));
            LogAssert.Expect(LogType.Error, "Invalid update type: 100");
        }
        {
            Tween.StopAll();
            var t = Tween.ShakeLocalPosition(transform, new ShakeSettings(Vector3.one, updateType: default)).tween;
            Assert.AreEqual(_UpdateType.Update, t.data.updateType);
            Assert.AreEqual(t, PrimeTweenManager.Instance.newTweensUpdate.Single());
        }
        {
            Tween.StopAll();
            var t = Tween.ShakeLocalPosition(transform, new ShakeSettings(Vector3.one, updateType: UpdateType.Update)).tween;
            Assert.AreEqual(_UpdateType.Update, t.data.updateType);
            Assert.AreEqual(t, PrimeTweenManager.Instance.newTweensUpdate.Single());
        }
        {
            Tween.StopAll();
            var t = Tween.ShakeLocalPosition(transform, new ShakeSettings(Vector3.one, updateType: UpdateType.LateUpdate)).tween;
            Assert.AreEqual(_UpdateType.LateUpdate, t.data.updateType);
            Assert.AreEqual(t, PrimeTweenManager.Instance.newTweensLateUpdate.Single());
        }
        {
            Tween.StopAll();
            var t = Tween.ShakeLocalPosition(transform, new ShakeSettings(Vector3.one, updateType: UpdateType.FixedUpdate)).tween;
            Assert.AreEqual(_UpdateType.FixedUpdate, t.data.updateType);
            Assert.AreEqual(t, PrimeTweenManager.Instance.newTweensFixedUpdate.Single());
        }

        {
            Tween.StopAll();
            PrimeTweenConfig.defaultUpdateType = UpdateType.FixedUpdate;
            var t = Tween.Position(transform, new TweenSettings<Vector3>(default, 1f, updateType: default)).tween;
            Assert.AreEqual(_UpdateType.FixedUpdate, t.data.updateType);
            Assert.AreEqual(t, PrimeTweenManager.Instance.newTweensFixedUpdate.Single());
        }
        {
            Tween.StopAll();
            Assert.AreEqual(0, tweensCount);
            PrimeTweenConfig.defaultUpdateType = UpdateType.LateUpdate;
            var t = Tween.Position(transform, new TweenSettings<Vector3>(default, 1f, updateType: default)).tween;
            Assert.AreEqual(_UpdateType.LateUpdate, t.data.updateType);
            Assert.AreEqual(t, PrimeTweenManager.Instance.newTweensLateUpdate.Single());
        }
        Tween.StopAll();

        #pragma warning disable CS0618 // Type or member is obsolete
        Assert.AreEqual(false, new TweenSettings(1f, updateType: default).useFixedUpdate);
        Assert.AreEqual(false, new TweenSettings(1f, updateType: UpdateType.Update).useFixedUpdate);
        Assert.AreEqual(false, new TweenSettings(1f, updateType: UpdateType.LateUpdate).useFixedUpdate);
        Assert.AreEqual(true, new TweenSettings(1f, updateType: UpdateType.FixedUpdate).useFixedUpdate);
        #pragma warning restore CS0618 // Type or member is obsolete

        Assert.AreEqual(UpdateType.Default, new TweenSettings(1f, updateType: default).updateType);
        PrimeTweenConfig.defaultUpdateType = UpdateType.Update;
        Assert.AreEqual(UpdateType.Default, new TweenSettings(1f, updateType: default).updateType);
        PrimeTweenConfig.defaultUpdateType = UpdateType.LateUpdate;
        Assert.AreEqual(UpdateType.Update, new TweenSettings(1f, updateType: UpdateType.Update).updateType);
        Assert.AreEqual(UpdateType.FixedUpdate, new TweenSettings(1f, updateType: UpdateType.FixedUpdate).updateType);
        PrimeTweenConfig.defaultUpdateType = iniUpdateType;
    }

    #if PRIME_TWEEN_DOTWEEN_ADAPTER
    [Test]
    public async Task AdapterCustomEaseCurve() {
        var curve = new AnimationCurve(new Keyframe(0, 0), new Keyframe(1, 1));
        await transform.DOScale(1f, minDuration).SetEase(curve);
        LogAssert.Expect(LogType.Error, Constants.customAnimationCurveInvalidError);
        await transform.DOScale(2f, minDuration).SetEase(null);
    }
    #endif

    /*[Test]
    public async Task CustomEaseCurve() {
        var curve = new AnimationCurve(new Keyframe(0, 0), new Keyframe(1, 1));
        await Tween.Scale(transform, 1f, minDuration, curve);
        LogAssert.Expect(LogType.Error, Constants.customAnimationCurveInvalidError);
        await Tween.Scale(transform, 2f, minDuration, null);
    }*/

    [Test]
    public async Task InvalidEaseType() {
        const Ease invalidEase = (Ease)(-10);
        ExpectError();
        await Tween.Scale(transform, 2f, minDuration, invalidEase);

        #if PRIME_TWEEN_DOTWEEN_ADAPTER
        ExpectError();
        await transform.DOScale(1f, minDuration).SetEase(invalidEase);
        #endif

        void ExpectError() => LogAssert.Expect(LogType.Error, "Invalid ease type: -10.");
    }

    [Test]
    public void NestedTweenProgress() {
        var nested = Tween.Delay(1f); // 1
        var seq = Sequence.Create() // 2
            .ChainDelay(1f) // 3
            .Chain(nested);
        seq.isPaused = true;
        /*seq.elapsedTime = 0.9f;
        Assert.AreEqual(0f, nested.progress);
        Assert.AreEqual(0f, nested.progressTotal);
        Assert.AreEqual(0f, nested.elapsedTime);
        Assert.AreEqual(0f, nested.elapsedTimeTotal);
        Assert.AreEqual(0, nested.cyclesDone);*/
        seq.elapsedTime = 1.5f;
        Assert.AreEqual(0.5f, nested.progress, 0.01f);
        /*Assert.AreEqual(0.5f, nested.progressTotal, 0.01f);
        Assert.AreEqual(0.5f, nested.elapsedTime);
        Assert.AreEqual(0.5f, nested.elapsedTimeTotal);
        Assert.AreEqual(0, nested.cyclesDone);
        seq.progressTotal = 1f;
        Assert.AreEqual(1f, nested.progress);
        Assert.AreEqual(1f, nested.progressTotal);
        Assert.AreEqual(1f, nested.elapsedTime);
        Assert.AreEqual(1f, nested.elapsedTimeTotal);
        Assert.AreEqual(1, nested.cyclesDone);*/
    }

    [UnityTest]
    public IEnumerator ZeroDurationWithStartDelay() {
        bool warnZeroDuration = PrimeTweenConfig.warnZeroDuration;
        PrimeTweenConfig.warnZeroDuration = false;
        float val = 0f;
        var t = Tween.Custom(-1f, 1f, new TweenSettings(0f, startDelay: 1f, endDelay: 1f), x => val = x);
        yield return null;
        Assert.IsTrue(t.isAlive);
        t.elapsedTime = 0.9f;
        Assert.AreEqual(0f, val);

        t.elapsedTime = 1.1f;
        Assert.AreEqual(1f, val);

        t.elapsedTime = 0.9f;
        Assert.AreEqual(-1f, val);

        t.elapsedTime = 1.9f;
        Assert.AreEqual(1f, val);
        Assert.IsTrue(t.isAlive);

        t.elapsedTime = 2.01f;
        Assert.AreEqual(1f, val);
        Assert.IsFalse(t.isAlive);

        PrimeTweenConfig.warnZeroDuration = warnZeroDuration;
    }

    [UnityTest]
    public IEnumerator SetRemainingCyclesWithNegativeTimescale() {
        var t = Tween.Position(transform, -Vector3.one, Vector3.one, 0.01f, Ease.Linear, cycles: 10);
        t.progressTotal = 0.95f;
        Assert.AreEqual(9, t.cyclesDone);
        t.timeScale = -1f;
        t.SetRemainingCycles(2);
        yield return t.ToYieldInstruction();
        LogAssert.NoUnexpectedReceived();
    }

    [UnityTest]
    public IEnumerator PausedTweenDoesNotProgress() {
        var t = Tween.Delay(1f);
        t.isPaused = true;
        Assert.AreEqual(0f, t.elapsedTimeTotal);
        yield return null;
        Assert.AreEqual(0f, t.elapsedTimeTotal);
    }

    [UnityTest]
    public IEnumerator LateUpdateTweenDoesNotUpdateFirstTimeInTheSameFrame() {
        float val = 0f;
        Tween.Custom(0f, 1f, new TweenSettings(minDuration, updateType: UpdateType.LateUpdate), v => {
            // print($"value changed {v}");
            val = v;
        });
        yield return null;
        Assert.AreEqual(0f, val);
        yield return null;
        Assert.AreEqual(1f, val);
    }

    [UnityTest]
    public IEnumerator LateUpdateTweenDoesNotUpdateFirstTimeInTheSameFrame2() {
        var startValue = -Vector3.one;
        var endValue = Vector3.one;
        transform.position = startValue;
        Tween.Position(transform, new TweenSettings<Vector3>(endValue, minDuration, updateType: UpdateType.LateUpdate));

        yield return null;
        Assert.AreEqual(startValue, transform.position);
        yield return null;
        Assert.AreEqual(endValue, transform.position);
    }

    [Test]
    public void StructSizes() {
        unsafe {
            Assert.AreEqual(
            #if UNITY_ASSERTIONS && !PRIME_TWEEN_DISABLE_ASSERTIONS
                128
            #else
                64
            #endif
                , sizeof(UnmanagedTweenData), nameof(UnmanagedTweenData));
            Assert.AreEqual(1, sizeof(CycleMode), nameof(CycleMode));
            Assert.AreEqual(1, sizeof(PropType), nameof(PropType));
            Assert.AreEqual(1, sizeof(Ease), nameof(Ease));
            Assert.AreEqual(1, sizeof(TweenAnimation.TweenType), nameof(TweenAnimation.TweenType));
            Assert.AreEqual(4, sizeof(Flags), nameof(Flags));
        }
    }

    [Test]
    public void CallbackTweenProgress() {
        var seq = Sequence.Create()
            .ChainCallback(() => { });
        Tween callbackTween = default;
        foreach (var child in seq.GetSelfChildren()) {
            callbackTween = new Tween(child);
        }
        Assert.IsTrue(callbackTween.isAlive);
        Assert.AreEqual(TweenAnimation.TweenType.Delay, callbackTween.tween.data.tweenType);
        Assert.AreEqual(0f, callbackTween.progress);
        Assert.AreEqual(0f, callbackTween.progressTotal);
        seq.isPaused = true;
        seq.progressTotal = 1f;
        Assert.AreEqual(1f, callbackTween.progress);
        Assert.AreEqual(1f, callbackTween.progressTotal);
        seq.Stop();
    }
}
#endif
