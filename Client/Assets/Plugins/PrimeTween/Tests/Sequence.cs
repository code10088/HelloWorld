// ReSharper disable AnnotateNotNullParameter
#if TEST_FRAMEWORK_INSTALLED
using System;
using System.Collections;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using JetBrains.Annotations;
using NUnit.Framework;
using PrimeTween;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using Assert = NUnit.Framework.Assert;
using Debug = UnityEngine.Debug;
using Mathf = UnityEngine.Mathf;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

public partial class Tests {
    [UnityTest]
    public IEnumerator ElapsedTime() {
        var timeStart = Time.time;
        var d1 = getDt() * 4f;
        var d2 = getDt() * 2f;
        var c1 = 2;
        var c2 = 3;
        var c3 = 2;
        var seq = Tween.Custom(0, 1, d1, delegate { }, cycles: c1)
            .Chain(Tween.Custom(0, 1, d2, delegate { }, cycles: c2));
        seq.SetRemainingCycles(c3);
        Assert.AreEqual((d1 * c1 + d2 * c2) * c3, seq.durationTotal, 0.00001f);
        while (seq.isAlive) {
            var expected = Time.time - timeStart;
            // Debug.Log($"expected {expected} / elapsedTimeTotal {seq.elapsedTimeTotal}");
            if (seq.cyclesDone == 0) {
                Assert.AreEqual(expected, seq.elapsedTime, 0.001f);
            }
            Assert.AreEqual(expected, seq.elapsedTimeTotal, getDt() * 2); // fails with lower tolerance because Sequence doesn't measure the elapsedTimeTotal, but calculates it from duration * cyclesDone
            yield return null;
        }
    }

    [UnityTest]
    public IEnumerator SequenceNestingCycles() {
        float duration = getDt();
        {
            int numCompleted = 0;
            var s1 = Sequence.Create(Tween.Delay(duration, () => numCompleted++));
            yield return Sequence.Create(Tween.Delay(duration))
                .Chain(s1)
                .ToYieldInstruction();
            Assert.AreEqual(1, numCompleted);
        }
        {
            int numCompleted = 0;
            var s1 = Sequence.Create(Tween.Delay(duration, () => numCompleted++));
            yield return Sequence.Create(2)
                .Chain(Tween.Delay(duration))
                .Chain(s1)
                .ToYieldInstruction();
            Assert.AreEqual(2, numCompleted);
        }
        {
            int numCompleted = 0;
            var s1 = Sequence.Create(2)
                .Chain(Tween.Delay(duration, () => numCompleted++));

            yield return Sequence.Create(2)
                .Chain(Tween.Delay(duration))
                .Chain(s1)
                .ToYieldInstruction();
            Assert.AreEqual(4, numCompleted);
        }
    }

    [UnityTest]
    public IEnumerator SequenceNestingDepsChain() {
        const float duration = 100f;
        var s1 = Sequence.Create(Tween.Delay(duration));
        var t1 = Tween.Delay(duration);
        var t2 = Tween.Delay(duration);
        var s2 = Sequence.Create(t1).Group(t2);
        s1.Chain(s2);
        Assert.AreEqual(duration, s2.root.tween.data.waitDelay);
        Assert.AreEqual(0f, t1.tween.data.waitDelay);
        Assert.AreEqual(0f, t2.tween.data.waitDelay);
        yield return null;
        Assert.AreEqual(0f, t1.elapsedTime);
        Assert.AreEqual(0f, t2.elapsedTime);
        s1.Stop();
    }

    [UnityTest]
    public IEnumerator SequenceNestingDepsGroup() {
        const float duration = 100f;
        var t1 = Tween.Delay(duration);
        var t2 = Tween.Delay(duration);
        var nested = Sequence.Create(t1).Group(t2);
        var seq = Sequence.Create(Tween.Delay(duration))
            .ChainDelay(duration)
            .Group(nested);
        Assert.AreEqual(0, t1.tween.data.waitDelay);
        Assert.AreEqual(0, t2.tween.data.waitDelay);
        yield return null;
        Assert.AreEqual(0, t1.elapsedTime);
        Assert.AreEqual(0, t2.elapsedTime);
        seq.Stop();
    }

    [UnityTest]
    public IEnumerator SequenceNestingAfterTweenChainOp() {
        var duration = 0.001f + Random.value * 0.05f; // longest
        var s1 = Sequence.Create(Tween.Delay(duration));
        var s2 = Sequence.Create();
        s1.Chain(s2);

        var t1 = Tween.Delay(0.01f);
        s1.Chain(t1);
        Assert.AreEqual(duration, t1.tween.data.waitDelay);

        var t2 = Tween.Delay(0.01f);
        s1.Group(t2);
        Assert.AreEqual(duration, t2.tween.data.waitDelay);

        yield return s1.ToYieldInstruction();
    }

    [UnityTest]
    public IEnumerator SequenceNestingChainOrder() {
        var t1 = Tween.Delay(0.01f);
        var s1 = Sequence.Create(t1);
        var t2 = Tween.Delay(0.05f);
        s1.Chain(t2);

        var t3 = Tween.Delay(0.01f);
        var t4 = Tween.Delay(0.01f);
        var s2 = Sequence.Create(t3).Group(t4);
        s1.Chain(s2);
        Assert.AreEqual(t2.durationWithWaitDelay, s2.root.tween.data.waitDelay);
        Assert.AreEqual(0f, t3.tween.data.waitDelay);
        Assert.AreEqual(0f, t4.tween.data.waitDelay);
        var t5 = Tween.Delay(0.01f);
        s1.Group(t5);
        Assert.AreEqual(t2.durationWithWaitDelay, t5.tween.data.waitDelay);
        yield return s1.ToYieldInstruction();
    }

    [UnityTest]
    public IEnumerator SequenceNestingGroupOrder() {
        const float t1Dur = 0.017f;
        var t1 = Tween.Delay(t1Dur);
        var s1 = Sequence.Create(t1);
        const float longestDur = 0.05f;
        var t2 = Tween.Delay(longestDur);
        s1.Chain(t2);
        Assert.AreEqual(t1.durationWithWaitDelay, t2.tween.data.waitDelay);
        var t3 = Tween.Delay(0.01f);
        s1.Group(t3);
        Assert.AreEqual(t1.durationWithWaitDelay, t3.tween.data.waitDelay);

        var s2 = Sequence.Create(Tween.Delay(0.01f));
        var t4 = Tween.Delay(0.01f);
        s2.Group(t4);

        s1.Group(s2);
        Assert.AreEqual(0f, t4.tween.data.waitDelay);

        var t5 = Tween.Delay(0.01f);
        s1.Group(t5);
        Assert.AreEqual(t1Dur, t5.tween.data.waitDelay);

        var t6 = Tween.Delay(0.01f);
        var seqDur = s1.durationTotal;
        s1.Chain(t6);
        Assert.AreEqual(seqDur, t6.tween.data.waitDelay);

        yield return s1.ToYieldInstruction();
    }

    [UnityTest]
    public IEnumerator SequenceNestingRestart() {
        int numCallback = 0;
        yield return Sequence.Create(2)
            .Chain(Tween.Delay(0.01f))
            .Chain(Sequence.Create(Tween.Delay(0.01f)))
            .ChainCallback(() => numCallback++)
            .ToYieldInstruction();
        Assert.AreEqual(2, numCallback);
    }

    [Test]
    public void SequenceNestingPause2() {
        {
            var s1 = Sequence.Create();

            var s2 = Sequence.Create();
            s2.isPaused = true;
            LogAssert.Expect(LogType.Error, new Regex("'isPaused' was ignored after adding"));
            s1.Group(s2);
            expectCantManipulateTweenInsideSequence();
            _ = s2.isPaused;
            s1.Complete();
        }
        {
            var s1 = Sequence.Create();
            s1.isPaused = true;
            var s2 = Sequence.Create();
            s2.isPaused = true;
            s1.Chain(s2);
            expectCantManipulateTweenInsideSequence();
            _ = s2.isPaused;
            s1.Complete();
            LogAssert.NoUnexpectedReceived();
        }
    }

    [Test]
    public void SequenceNestingPause() {
        var s = Sequence.Create(Tween.Delay(0.01f))
            .Chain(Sequence.Create(Tween.Delay(0.01f)));
        Assert.AreEqual(1, numNestedSequences(s));
        s.isPaused = true;
        int count = 0;
        foreach (var t in s.GetAllChildren()) {
            count++;
            var child = new Sequence(t);

            expectCantManipulateTweenInsideSequence();
            expectCantManipulateTweenInsideSequence();
            child.isPaused = !child.isPaused;

            expectCantManipulateTweenInsideSequence();
            expectCantManipulateTweenInsideSequence();
            child.timeScale += 1f;
        }
        Assert.AreEqual(3, count);

        s.isPaused = false;
        count = 0;
        foreach (var _ in s.GetAllTweens()) {
            count++;
        }
        Assert.AreEqual(4, count);
    }

    [Test]
    public void SequenceNestingLongestTween() {
        float dur = Random.value + 0.1f;
        var t = Tween.Delay(dur);
        var sequence = Sequence.Create(Tween.Delay(dur))
            .Chain(Sequence.Create(Tween.Delay(dur)))
            .Chain(Sequence.Create(t));
        Assert.AreEqual(dur * 3, sequence.durationTotal, 0.0001f);
        sequence.Stop();
        Assert.IsFalse(t.isAlive);
    }

    [UnityTest]
    public IEnumerator SequenceNestingStopChildInTheMiddle() {
        Tween.StopAll();
        int numS1Completed = 0;
        int numS2Completed = 0;
        int numS3Completed = 0;
        float duration = minDuration * Random.Range(1f, 10f);
        var s1 = Sequence.Create(Tween.Delay(duration, () => {
            Assert.AreEqual(0, numS1Completed);
            Assert.AreEqual(0, numS3Completed);
            numS1Completed++;
        }));
        var s2 = Sequence.Create(Tween.Delay(duration, () => numS2Completed++));
        var s3 = Sequence.Create(Tween.Delay(duration, () => {
            Assert.AreEqual(1, numS1Completed);
            Assert.AreEqual(0, numS3Completed);
            numS3Completed++;
        }));
        s1.Chain(s2).Chain(s3);
        Assert.AreEqual(2, numNestedSequences(s1));
        expectCantManipulateTweenInsideSequence();
        s2.Stop();
        Assert.AreEqual(2, numNestedSequences(s1));
        yield return s1.ToYieldInstruction();
        Assert.AreEqual(1, numS1Completed);
        Assert.AreEqual(1, numS2Completed);
        Assert.AreEqual(1, numS3Completed);
        yield return null;
        Assert.AreEqual(0, tweensCount);
    }

    [UnityTest]
    public IEnumerator SequenceNestingCompleteLastChild() {
        var duration = getDt();
        var s1 = Sequence.Create(Tween.Delay(duration));
        var s2 = Sequence.Create(Tween.Delay(duration));
        s1.Chain(s2);
        Assert.AreEqual(1, numNestedSequences(s1));
        expectCantManipulateTweenInsideSequence();
        s2.Complete();
        Assert.AreEqual(1, numNestedSequences(s1));
        Assert.IsTrue(s2.isAlive);
        Assert.IsTrue(s1.isAlive);
        yield return s1.ToYieldInstruction();
        }

    static int numNestedSequences(Sequence seq) {
        Assert.IsTrue(seq.isAlive);
        int result = 0;
        foreach (var t in seq.GetAllTweens()) {
            if (t.data.IsSequenceRoot() && !t.data.IsMainSequenceRoot()) {
                result++;
            }
        }
        Assert.IsTrue(result >= 0);
        return result;
    }

    [UnityTest]
    public IEnumerator SequenceNesting() {
        if (tweensCount > 0) {
            Tween.StopAll();
            Assert.AreEqual(0, tweensCount);
        }
        int numS1Completed = 0;
        int numS2Completed = 0;
        var s1 = Sequence.Create(Tween.Delay(0.02f, () => {
            // Debug.Log("1");
            Assert.AreEqual(0, numS1Completed);
            Assert.AreEqual(0, numS2Completed);
            numS1Completed++;
        }));
        var s2 = Sequence.Create(Tween.Delay(0.01f, () => {
            // Debug.Log("2");
            Assert.AreEqual(1, numS1Completed);
            Assert.AreEqual(0, numS2Completed);
            numS2Completed++;
        }));
        s1.Chain(s2);
        Assert.AreEqual(1, numNestedSequences(s1));
        yield return s1.ToYieldInstruction();
        Assert.AreEqual(1, numS1Completed);
        Assert.AreEqual(1, numS2Completed);
        yield return null;
        Assert.AreEqual(0, tweensCount);
    }

    [Test]
    public void SequenceNestingDifferentSettings() {
        var seq = Sequence.Create();
        seq.timeScale = Random.Range(0.01f, 1.5f);

        expectErrors();
        seq.Group(createSequenceWithNonDefaultSettings());

        expectErrors();
        seq.Chain(createSequenceWithNonDefaultSettings());
        LogAssert.NoUnexpectedReceived();

        Sequence.Create(updateType: UpdateType.FixedUpdate)
            .Group(Tween.PositionY(transform, new TweenSettings<float>(5, 0.1f)))
            .ChainCallback(() => {});

        return;

        void expectErrors() {
            LogAssert.Expect(LogType.Error, new Regex("'isPaused' was ignored after adding"));
            LogAssert.Expect(LogType.Error, new Regex("'timeScale' was ignored after adding"));
            LogAssert.Expect(LogType.Error, new Regex("'useUnscaledTime' was ignored after adding"));
            LogAssert.Expect(LogType.Error, new Regex("'updateType' was ignored after adding"));
        }

        Sequence createSequenceWithNonDefaultSettings() {
            var s2 = Sequence.Create(useUnscaledTime: true, updateType: UpdateType.FixedUpdate);
            s2.isPaused = true;
            s2.timeScale = 0.5f;
            return s2;
        }
    }

    [UnityTest]
    public IEnumerator SequenceNestingInfinite() {
        var s1 = Sequence.Create();
        var infSeq = Sequence.Create(cycles: -1);
        expectInfiniteTweenInSequenceError();
        s1.Group(infSeq);

        expectInfiniteTweenInSequenceError();
        s1.Chain(infSeq);

        yield return null;
        infSeq.Complete();
    }

    [UnityTest]
    public IEnumerator GetSequenceElapsedTimeWhenAllTargetsDestroyed() {
        var target = new GameObject();
        var s = Sequence.Create(Tween.Delay(target, 0.1f));
        Object.DestroyImmediate(target);
        Assert.IsTrue(s.isAlive);
        var _ = s.elapsedTime;
        yield return s.ToYieldInstruction();
    }

    [UnityTest]
    public IEnumerator StopAllWhenSequenceHasCompletedTweenAndTargetDestroyed() {
        Tween.StopAll();
        var target = new GameObject();
        var duration = getDt() * 5f;
        var t = Tween.Delay(target, duration);
        int numCompleted = 0;
        var s = Sequence.Create(t)
            .Chain(Tween.Delay(target, duration, () => numCompleted++));
        while (t.interpolationFactor < 1f) {
            yield return null;
        }
        Assert.IsTrue(t.isAlive);
        Assert.IsTrue(s.isAlive);
        int aliveCount = 0;
        foreach (var _cold in s.GetAllTweens()) {
            if (_cold.data.isAlive) {
                aliveCount++;
            }
        }
        Assert.AreEqual(3, aliveCount, "tweens now always alive while their sequence is alive");
        Object.DestroyImmediate(target);
        Assert.AreEqual(3, tweensCount);
        expectOnCompleteIgnored();
        var numCompleteAll = Tween.CompleteAll();
        Assert.AreEqual(3, numCompleteAll);
        Assert.AreEqual(0, numCompleted);
        Assert.AreEqual(0, tweensCount);
        Assert.AreEqual(0, numCompleted);
        LogAssert.NoUnexpectedReceived();
    }

    [UnityTest]
    public IEnumerator StopAllWhenSequenceHasCompletedTween() {
        var duration = getDt() * 10f;
        var t = Tween.Delay(duration);
        var s = Sequence.Create(t)
            .ChainDelay(duration);
        while (t.interpolationFactor < 1f) {
            yield return null;
        }
        Assert.IsTrue(t.isAlive);
        Assert.IsTrue(s.isAlive);
        Assert.AreEqual(3, tweensCount);
        Tween.StopAll();
        Assert.AreEqual(0, tweensCount);
    }

    [UnityTest]
    public IEnumerator StopSequenceFromCallback() {
        var aliveCount = tweensCount;
        if (aliveCount > 0) {
            Assert.AreEqual(Tween.StopAll(null), aliveCount);
        }
        Sequence s = default;
        s = Sequence.Create()
            .ChainCallback(() => {
                s.Stop();
                Assert.IsFalse(s.isAlive);
            });
        yield return s.ToYieldInstruction();
        yield return null;
        Assert.AreEqual(0, tweensCount);
    }

    [UnityTest]
    public IEnumerator ExceptionInOnCompleteNestedSequence() {
        var aliveCount = tweensCount;
        if (aliveCount > 0) {
            Assert.AreEqual(Tween.StopAll(), aliveCount);
        }
        var main = Sequence.Create(Tween.Delay(getDt() * 2));
        expectOnCompleteException();
        var nested = Sequence.Create().ChainCallback(() => throw new Exception());
        main.Group(nested);
        yield return main.ToYieldInstruction();
        yield return null;
        Assert.AreEqual(0, tweensCount);
        LogAssert.NoUnexpectedReceived();
    }

    static void expectOnCompleteException() {
        LogAssert.Expect(LogType.Error, new Regex("Tween's onComplete callback raised exception"));
        LogAssert.Expect(LogType.Exception, new Regex("Exception"));
    }

    [UnityTest]
    public IEnumerator ExceptionInOnValueChangeNestedSequence() {
        var aliveCount = tweensCount;
        if (aliveCount > 0) {
            Assert.AreEqual(Tween.StopAll(null), aliveCount);
        }
        var main = Sequence.Create(Tween.Delay(getDt() * 2));
        expectTweenWasStoppedBecauseException();
        var nested = Sequence.Create(Tween.Custom(0f, 1f, getDt() * 2, delegate { throw new Exception(); }));
        main.Group(nested);
        yield return main.ToYieldInstruction();
        yield return null;
        Assert.AreEqual(0, tweensCount);
        LogAssert.NoUnexpectedReceived();
    }

    [Test]
    public async Task SequenceLongestTween() {
        {
            var t1 = Tween.Delay(0.01f);
            var t2 = Tween.Delay(0.02f);
            var s = t1.Group(t2);
            Assert.AreEqual(t2.durationTotal, s.durationTotal);
            await s;
        }
        {
            var t1 = Tween.Delay(0.01f);
            var t2 = Tween.Delay(0.02f);
            var s = t2.Group(t1);
            Assert.AreEqual(t2.durationTotal, s.durationTotal);
        }
        {
            var t1 = Tween.Delay(0.01f);
            var t2 = Tween.Delay(0.02f);
            var s = t1.Chain(t2);
            Assert.AreEqual(t1.durationTotal + t2.durationTotal, s.durationTotal);
        }
        {
            var t1 = Tween.Delay(0.01f);
            var t2 = Tween.Delay(0.02f);
            var s = t2.Chain(t1);
            Assert.AreEqual(t1.durationTotal + t2.durationTotal, s.durationTotal);
        }
        {
            var t1 = Tween.PositionX(transform, 1f, 0.1f, cycles: 3); // longest
            var t2 = Tween.PositionX(transform, 1f, 0.1f);
            var s = t1.Group(t2);
            Assert.AreEqual(t1.durationTotal, s.durationTotal);
        }
        {
            var t1 = Tween.PositionX(transform, 1f, 0.1f, cycles: 3);
            var t2 = Tween.PositionX(transform, 1f, 0.1f);
            var s = t1.Chain(t2);
            Assert.AreEqual(t1.durationTotal + t2.durationTotal, s.durationTotal);
        }
    }

    [Test]
    public async Task AwaitFinishedSequence() {
        var t = Tween.Delay(0.01f);
        var s = Sequence.Create(t);
        await s;
        Assert.IsTrue(s.IsCreated);
        Assert.IsFalse(s.isAlive);
        await s;
    }

    [UnityTest]
    public IEnumerator SequenceRestartFromTo() {
        for (int i = 0; i < 1; i++) {
            var target = new GameObject(nameof(SequenceRestartFromTo)).transform;
            Assert.AreEqual(Vector3.zero, target.localPosition);
            var s = Tween.LocalPositionX(target, new TweenSettings<float>(0f, 1f, getDt() * 2f))
                .Chain(Tween.LocalPositionX(target, new TweenSettings<float>(1f, 0.5f, getDt())));
            s.SetRemainingCycles(2);
            if (Random.value < 0.5f) {
                s.Complete();
            } else {
                yield return s.ToYieldInstruction();
            }
            UnityEngine.Assertions.Assert.AreApproximatelyEqual(0.5f, target.localPosition.x);
            Object.Destroy(target.gameObject);
        }
    }

    [UnityTest]
    public IEnumerator SequenceRestartTo() {
        var target = new GameObject(nameof(SequenceRestartTo)).transform;
        Assert.AreEqual(Vector3.zero, target.localPosition);
        var duration = getDt() * 3f;
        var s = Tween.LocalPositionX(target, 1f, duration)
            .Chain(Tween.LocalPositionX(target, 0.5f, duration));
        s.SetRemainingCycles(2);
        if (Random.value < 0.5f) {
            s.Complete();
        } else {
            yield return s.ToYieldInstruction();
        }
        UnityEngine.Assertions.Assert.AreApproximatelyEqual(0.5f, target.localPosition.x);
        Object.Destroy(target.gameObject);
    }

    [UnityTest]
    public IEnumerator SequenceRestartWhenTweenHaveStartDelay() {
        var data = new TweenSettings<Vector3>(Vector3.zero, Vector3.one, getDt() * Random.Range(0.5f, 2f), startDelay: getDt() * Random.Range(0.5f, 2f));
        var t1 = Tween.Position(transform, data);
        var t2 = Tween.Position(transform, data);
        PrimeTweenConfig.warnStructBoxingAllocationInCoroutine = true;
        expectCoroutineBoxingWarning();
        var seq = t1.Chain(t2);
        seq.SetRemainingCycles(2);
        yield return seq;
        LogAssert.NoUnexpectedReceived();
    }

    static void expectCoroutineBoxingWarning() {
        LogAssert.Expect(LogType.Warning, new Regex("Please use Tween/Sequence.ToYieldInstruction"));
    }

    [UnityTest]
    public IEnumerator StopAllWhenTweenInSequenceIsCompleted() {
        var target = new object();
        var duration = getDt() * 2f;
        var t1 = Tween.Delay(target, duration);
        var t2 = Tween.Delay(target, duration);
        yield return t1.Chain(t2).ToYieldInstruction();
        Tween.StopAll(target);
        Tween.SetPausedAll(true, target);
        Tween.SetPausedAll(false, target);
    }

    [UnityTest]
    public IEnumerator SequenceElapsedTime() {
        yield return null;
        float dt = getDt();
        var dur1 = dt * Random.Range(2, 5);
        var dur2 = dt * Random.Range(2, 5);
        var cycles1 = Random.Range(1, 4);
        var cycles2 = Random.Range(1, 4);
        var t1 = Tween.Position(transform, Vector3.one, dur1, cycles: cycles1);
        var t2 = Tween.Position(transform, Vector3.one, dur2, cycles: cycles2);
        var s = Sequence.Create(t1)
            .Chain(t2);

        float timeStart = Time.time;
        Assert.IsTrue(t1.isAlive);
        while (t1.interpolationFactor < 1f) {
            yield return null;
        }
        Assert.AreEqual(dur1 * cycles1, Time.time - timeStart, Time.deltaTime);

        Assert.IsTrue(s.isAlive);
        yield return s;
        Assert.AreEqual(dur1 * cycles1 + dur2 * cycles2, Time.time - timeStart, Time.deltaTime);
    }

    [UnityTest]
    public IEnumerator DefaultPropertiesOfSequence() {
        {
            Assert.AreEqual(0, tweensCount);
            var duration = 0.001f;
            var s = Sequence.Create(-1).Chain(Tween.Delay(duration));
            Assert.AreEqual(-1, s.cyclesTotal);
            Assert.AreEqual(duration, s.duration);
            Assert.IsTrue(float.IsPositiveInfinity(s.durationTotal));

            yield return null;
            const int expectedCount = 2;
            Assert.AreEqual(expectedCount, tweensCount);
            yield return null;
            Assert.AreEqual(expectedCount, tweensCount);
            yield return null;
            Assert.AreEqual(expectedCount, tweensCount);
            s.Stop();
            Assert.AreEqual(expectedCount, tweensCount);
            yield return null;
            Assert.AreEqual(0, tweensCount);
        }
        {
            var s = Sequence.Create();
            Assert.AreEqual(1, s.cyclesTotal);
            validate(s);
        }
        {
            var s = Sequence.Create();
            validate(s);
        }

        void validate(Sequence s) {
            Assert.AreEqual(0, s.duration);
            Assert.AreEqual(0, s.durationTotal);
            Assert.AreEqual(0, s.progress);
            Assert.AreEqual(0, s.progressTotal);
            Assert.AreEqual(0, s.cyclesDone);
            Assert.AreEqual(0, s.elapsedTime);
            Assert.AreEqual(0, s.elapsedTimeTotal);
        }
    }

    [Test]
    public async Task SequenceProperties() {
        for (int i = 0; i < 1; i++) {
            int cyclesDone = 0;
            Tween tween = default;
            var duration = Mathf.Max(TweenSettings.minDuration, getDt() * Random.Range(0.5f, 6f));
            var sequenceCycles = Random.Range(2, 5);
            // const float duration = 0.1f;
            // const int sequenceCycles = 2;
            Sequence sequence = default;
            validateSequenceDefaultProps(false);
            sequence = Sequence.Create(sequenceCycles);
            float timeStart = Time.time;
            tween = Tween.Custom(transform, 0, 1, duration, ease: Ease.Linear, onValueChange: (_, val) => {
                // Debug.Log($"elapsedTimeTotal: {sequence.elapsedTimeTotal}, val: {val}");
                Assert.IsTrue(tween.isAlive);
                Assert.IsTrue(sequence.isAlive);
                UnityEngine.Assertions.Assert.AreApproximatelyEqual(sequence.duration, duration);
                var durationTotalExpected = duration * sequenceCycles;
                UnityEngine.Assertions.Assert.AreApproximatelyEqual(sequence.durationTotal, durationTotalExpected);
                Assert.AreEqual(tween.duration, duration);
                Assert.AreEqual(tween.durationTotal, duration);
                var elapsedTimeExpected = Time.time - timeStart;
                if (val < 1f) {
                    if (sequence.cyclesDone == 0) {
                        UnityEngine.Assertions.Assert.AreApproximatelyEqual(sequence.elapsedTime, tween.elapsedTime);
                        UnityEngine.Assertions.Assert.AreApproximatelyEqual(tween.elapsedTime, elapsedTimeExpected);
                        UnityEngine.Assertions.Assert.AreApproximatelyEqual(sequence.elapsedTime, elapsedTimeExpected);
                    }
                    if (sequence.root.tween.data.elapsedTimeTotal >= 0f) {
                        Assert.AreEqual(elapsedTimeExpected, sequence.elapsedTimeTotal, getDt());
                        Assert.AreEqual(sequence.progress, Mathf.Clamp01(sequence.elapsedTime / sequence.duration));
                        Assert.AreEqual(sequence.progressTotal, Mathf.Clamp01(elapsedTimeExpected / durationTotalExpected), 0.001f);
                    }
                }
            });
            #pragma warning disable 4014
            sequence.Group(tween);
            #pragma warning restore
            await sequence.ChainCallback(() => cyclesDone++);
            Assert.AreEqual(sequenceCycles, cyclesDone);
            Assert.IsFalse(sequence.isAlive);
            validateSequenceDefaultProps(true);
            Assert.IsFalse(tween.isAlive);

            void validateSequenceDefaultProps(bool isCreated) {
                for (int j = 0; j < 8; j++) {
                    expectIsDeadError(isCreated);
                }
                Assert.AreEqual(0, sequence.elapsedTime);
                Assert.AreEqual(0, sequence.elapsedTimeTotal);
                Assert.AreEqual(0, sequence.cyclesDone);
                Assert.AreEqual(0, sequence.cyclesTotal);
                Assert.AreEqual(0, sequence.duration);
                Assert.AreEqual(0, sequence.durationTotal);
                Assert.AreEqual(0, sequence.progress);
                Assert.AreEqual(0, sequence.progressTotal);
            }
        }
    }

    [UnityTest]
    public IEnumerator ExceptionInSequenceOnCycleComplete() {
        int numExceptions = 0;
        Sequence s = default;
        s = Sequence.Create(2, Sequence.SequenceCycleMode.Restart)
            .Chain(Tween.Delay(getDt()))
            .Chain(Tween.Custom(0f, 1f, getDt(), val => {
                // print($"{s.cyclesDone}, {val}");
                if (s.cyclesDone == 1 && val == 0f) {
                    numExceptions++;
                    expectTweenWasStoppedBecauseException();
                    throw new Exception();
                }
            }))
            .Chain(Tween.Delay(getDt()));
        yield return s;
        Assert.AreEqual(1, numExceptions);
    }

    [Test]
    public async Task SequenceIncrementalTween() {
        await Tween.Delay(getDt());
        float dur = getDt() * (Random.value + 0.1f);
        var t = Tween.Position(transform, Vector3.one, dur, cycles: 2, cycleMode: CycleMode.Incremental);
        int cyclesDone = 0;
        const int sequenceCycles = 4;
        await Sequence.Create(sequenceCycles)
            .Chain(t)
            .ChainCallback(() => {
                cyclesDone++;
                Assert.AreEqual(0f, t.interpolationFactor % 1f);
            });
        Assert.AreEqual(sequenceCycles, cyclesDone);
    }

    [Test]
    public void SettingInfiniteLoopsToTweenInSequence() {
        var t = createTween();
        var s = Sequence.Create(t);
        expectCantManipulateTweenInsideSequence();
        t.SetRemainingCycles(-1);
        s.Complete();
    }

    [Test]
    public void TweenIsReleasedFromSequenceOnReleaseAll() {
        test(true);
        test(false);
        void test(bool isStop) {
            var t = createTween();
            var s = Sequence.Create(t);
            Assert.IsTrue(t.isAlive);
            Assert.IsTrue(s.isAlive);
            if (isStop) {
                Tween.StopAll();
            } else {
                Tween.CompleteAll();
            }
            Assert.IsFalse(t.isAlive);
            Assert.IsFalse(s.isAlive);
            Assert.IsNull(t.tween.sequence);
        }
    }

    [Test]
    public void SequenceComplete() {
        int numTweenCompleted = 0;
        var s = Sequence.Create(createCustomTween(0.01f).OnComplete(() => numTweenCompleted++));
        Assert.IsTrue(s.isAlive);
        s.Complete();
        Assert.IsFalse(s.isAlive);
        Assert.AreEqual(1, numTweenCompleted);
    }

    [Test]
    public void SequenceStop() {
        var s = Sequence.Create(createCustomTween(0.01f));
        Assert.IsTrue(s.isAlive);
        s.Stop();
        Assert.IsFalse(s.isAlive);
        s.Stop();
        Assert.IsFalse(s.isAlive);
    }

    /*[UnityTest]
    public IEnumerator SequenceRevert() {
        var val = 0f;
        var t = Tween.Custom(this, 0, 1, 1, (_, newVal) => {
            val = newVal;
        });
        var s = Sequence.Create(t);
        yield return null;
        Assert.AreNotEqual(0, val);
        s.Revert();
        Assert.IsFalse(s.isAlive);
        Assert.AreEqual(0, val);
    }*/

    [UnityTest]
    public IEnumerator CompletedTweenInsideSequenceDoesntCompleteSecondTimeOnCompletingSequence() {
        var numFirstCompleted = 0;
        var numSecondCompleted = 0;
        Tween first = createCustomTween(getDt()).OnComplete(() => numFirstCompleted++);
        Tween second = createCustomTween(getDt() * 5f).OnComplete(() => numSecondCompleted++);
        var sequence = Sequence.Create(first)
            .Group(second);
        while (first.interpolationFactor < 1f) {
            yield return null;
        }
        Assert.IsTrue(first.isAlive);
        Assert.IsTrue(first.tween.data.isAlive);
        Assert.AreEqual(1, numFirstCompleted);

        Assert.IsTrue(second.isAlive);
        Assert.IsTrue(sequence.isAlive);
        sequence.Complete();
        Assert.AreEqual(1, numFirstCompleted);
        Assert.AreEqual(1, numSecondCompleted);
    }

    static Tween createCustomTween(float duration) {
        return Tween.Custom(PrimeTweenManager.Instance, 0, 1, duration, delegate{});
    }

    [Test]
    public void CompletingSequenceCompletesAllTweensInside() {
        var numFirstCompleted = 0;
        var numSecondCompleted = 0;
        Sequence.Create(createTween().OnComplete(() => numFirstCompleted++))
            .Group(createTween().OnComplete(() => numSecondCompleted++))
            .Complete();
        Assert.AreEqual(1, numFirstCompleted);
        Assert.AreEqual(1, numSecondCompleted);
    }

    [UnityTest]
    public IEnumerator Cycles() {
        var tweenCyclesDone = 0;
        var sequenceCyclesDone = 0;
        const int sequenceCycles = 5;
        const int tweenCycles = 3;
        // tween cycles doesn't matter because OnComplete will only be executed when all cycles complete
        var tween = Tween.Custom(this, 0, 1, Mathf.Max(minDuration, getDt() * Random.Range(0.1f, 0.5f)), cycles: tweenCycles, onValueChange: delegate{})
            .OnComplete(() => {
                // Debug.Log($"[{Time.frameCount}] tweenCycles++");
                tweenCyclesDone++;
            });
        Sequence sequence = default;
        sequence = Sequence.Create(sequenceCycles)
            .Chain(tween)
            .ChainCallback(() => {
                // Debug.Log($"[{Time.frameCount}] sequenceCycles: {sequenceCyclesDone}, actual cyclesDone: {sequence.cyclesDone}");
                Assert.AreEqual(sequenceCyclesDone, sequence.cyclesDone);
                Assert.AreEqual(sequenceCycles, sequence.cyclesTotal);
                sequenceCyclesDone++;
            });
        yield return sequence.ToYieldInstruction();
        Assert.IsFalse(tween.isAlive);
        Assert.IsFalse(sequence.isAlive);
        Assert.AreEqual(sequenceCycles, tweenCyclesDone);
        Assert.AreEqual(sequenceCycles, sequenceCyclesDone);
    }

    /*static IEnumerator Timeout([NotNull] Func<bool> continueWaiting, float timeout) {
        var timeStarted = Time.time;
        while (continueWaiting()) {
            if (Time.time - timeStarted > timeout) {
                throw new Exception($"Timeout of {timeout} seconds reached.");
            }
            yield return null;
        }
    }*/

    [Test]
    public void GroupDeadTweenToSequence() {
        var deadTween = createTween();
        deadTween.Complete();
        expectAddDeadToSequenceError();
        Sequence.Create(Tween.Delay(0.001f)).Group(deadTween);
    }

    Tween createTween() {
        var t = Tween.LocalPosition(transform, Vector3.one, 1);
        Assert.IsTrue(t.isAlive);
        return t;
    }

    [Test]
    public void ChainDeadTweenToSequence() {
        var tweener = Tween.LocalPosition(transform, Vector3.one, 1);
        tweener.Complete();
        Assert.IsFalse(tweener.isAlive);
        expectAddDeadToSequenceError();
        Sequence.Create(Tween.Delay(0.0001f)).Chain(tweener);
    }

    static void expectAddDeadToSequenceError() {
        LogAssert.Expect(LogType.Error, Constants.addDeadTweenToSequenceError);
    }

    [UnityTest]
    public IEnumerator SequenceRelease() {
        Tween.StopAll();
        for (int i = 0; i < 5; i++) {
            Assert.AreEqual(0, tweensCount);
            var duration = Mathf.Max(TweenSettings.minDuration, getDt());
            var t = Tween.Delay(duration);
            var s = Sequence.Create(t);
            yield return null;
            // Debug.Log($"duration: {duration}, dt {Time.deltaTime}");
            // print(Time.deltaTime > duration);
            if (Time.deltaTime > duration) {
                Assert.IsFalse(t.isAlive);
                Assert.IsFalse(s.isAlive);
                Tween.StopAll();
                continue;
            }
            Assert.IsTrue(t.isAlive);
            Assert.IsTrue(s.isAlive);
            s.Stop();
            Assert.AreEqual(2, tweensCount);
            yield return null;
            Assert.AreEqual(0, tweensCount);
        }
    }

    [UnityTest]
    public IEnumerator ManipulatingTweensInsideSequenceCoroutine() {
        {
            var nestedTween = Tween.Delay(getDt());
            Sequence.Create(nestedTween);
            expectCantManipulateTweenInsideSequence();
            yield return nestedTween.ToYieldInstruction();
        }
        {
            var nestedSequence = Sequence.Create();
            Sequence.Create().Chain(nestedSequence);
            expectCantManipulateTweenInsideSequence();
            yield return nestedSequence.ToYieldInstruction();
        }
    }

    [Test]
    public async Task ManipulatingTweensInsideSequenceAsync() {
        var t = Tween.Delay(getDt());
        #pragma warning disable 4014
        Sequence.Create(t);
        #pragma warning restore
        expectCantManipulateTweenInsideSequence();
        await t;
    }

    [Test]
    public void ManipulatingTweensInsideSequence() {
        var go = new GameObject();
        var tween = Tween.Custom(go, 0, 1, 1, delegate{}, cycles: 3, cycleMode: CycleMode.Yoyo);
        Sequence.Create(tween);
        Assert.IsTrue(tween.isAlive);

        expectCantManipulateTweenInsideSequence();
        tween.Stop();

        expectCantManipulateTweenInsideSequence();
        tween.Complete();

        expectCantManipulateTweenInsideSequence();
        tween.SetRemainingCycles(5);

        expectCantManipulateTweenInsideSequence();
        tween.SetRemainingCycles(true);

        expectCantManipulateTweenInsideSequence();
        tween.Group(new Tween());

        expectCantManipulateTweenInsideSequence();
        tween.Chain(new Tween());

        expectCantManipulateTweenInsideSequence();
        tween.Group(new Sequence());

        expectCantManipulateTweenInsideSequence();
        tween.Chain(new Sequence());

        expectCantManipulateTweenInsideSequence();
        Assert.AreEqual(0 , Tween.StopAll(go));

        expectCantManipulateTweenInsideSequence();
        Assert.AreEqual(0, Tween.CompleteAll(go));

        expectCantManipulateTweenInsideSequence();
        Tween.SetPausedAll(true, go);

        expectCantManipulateTweenInsideSequence();
        Tween.SetPausedAll(false, go);

        expectCantManipulateTweenInsideSequence();
        expectCantManipulateTweenInsideSequence();
        tween.isPaused = !tween.isPaused;

        expectCantManipulateTweenInsideSequence();
        expectCantManipulateTweenInsideSequence();
        tween.timeScale += 0.5f;

        expectCantManipulateTweenInsideSequence();
        tween.elapsedTime += 0.1f;

        expectCantManipulateTweenInsideSequence();
        tween.elapsedTimeTotal += 0.1f;

        Tween.StopAll();
        Assert.AreEqual(0, tweensCount);
        LogAssert.NoUnexpectedReceived();
    }

    static void expectCantManipulateTweenInsideSequence() {
        LogAssert.Expect(LogType.Error, new Regex("It's not allowed to manipulate 'nested'"));
    }

    [UnityTest]
    public IEnumerator AddingInfiniteTweenToSequenceThrows() {
        if (tweensCount > 0) {
            Tween.StopAll();
        }

        var infiniteTween = createInfiniteTween();
        expectInfiniteTweenInSequenceError();
        Sequence.Create(infiniteTween);

        expectInfiniteTweenInSequenceError();
        Sequence.Create(infiniteTween);

        expectInfiniteTweenInSequenceError();
        Sequence.Create(Tween.Delay(minDuration)).Chain(infiniteTween);

        infiniteTween.Stop();
        yield return null;
        yield return null;
        Assert.AreEqual(0, tweensCount);
    }

    static void expectInfiniteTweenInSequenceError() => LogAssert.Expect(LogType.Error, Constants.infiniteTweenInSequenceError);

    static void expectException<T>(Action code, [NotNull] string message) where T: Exception {
        Assert.IsFalse(string.IsNullOrEmpty(message));
        try {
            code();
        } catch (T e) {
            if (!e.Message.Contains(message)) {
                Debug.LogException(e);
                Assert.Fail();
            }
            return;
        }
        Assert.Fail($"Exception of type {typeof(T)} didn't appear: {message}");
    }

    [Test]
    public void TestDeadSequence() {
        var s = createDeadSequence();

        expectError();
        s.Group(createCustomTween(1));

        expectError();
        s.ChainCallback(delegate { });

        expectError();
        s.ChainCallback(this, delegate { });

        expectError();
        s.Chain(createTween());

        #if PRIME_TWEEN_DOTWEEN_ADAPTER
        expectError();
        s.ChainLast(createTween());
        #endif

        s.Stop();
        s.Complete();
        // s.Revert();

        expectError();
        s.SetRemainingCycles(5);

        expectError();
        s.isPaused = true;

        void expectError() {
            expectIsDeadError();
        }
    }

    static Sequence createDeadSequence() {
        var s = Sequence.Create(createCustomTween(1));
        s.Complete();
        Assert.IsFalse(s.isAlive);
        return s;
    }

    [UnityTest]
    public IEnumerator AddingTweenToSequenceModifiesIsPausedToMatchSequence() {
        Tween.StopAll();
        Assert.AreEqual(0, tweensCount);
        var s = Sequence.Create(Tween.PositionY(transform,10f, 0.04f));
        Assert.IsFalse(s.isPaused);

        var t = Tween.Delay(0.04f);
        Assert.IsFalse(t.isPaused);
        t.isPaused = true;
        Assert.IsTrue(t.isPaused);

        LogAssert.Expect(LogType.Error, new Regex("'isPaused' was ignored after adding"));
        s.Group(t);
        LogAssert.NoUnexpectedReceived();
        yield return s.ToYieldInstruction();
    }


    [Test]
    public void DuplicateTweenAddedToSequence() {
        var t = createTween();
        var s = Sequence.Create(t);
        expectNestTweenTwiceError();
        s.Group(t);

        expectNestTweenTwiceError();
        var s2 = Sequence.Create();
        s2.Group(t);

        expectNestTweenTwiceError();
        Sequence.Create(t);
    }

    static void expectNestTweenTwiceError() => LogAssert.Expect(LogType.Error, new Regex(Constants.nestTwiceError));

    [UnityTest]
    public IEnumerator TweenIsNotReleasedFromSequenceUntilSequenceReleasesAllTweens() {
        var t = createCustomTween(minDuration);
        var delay = Tween.Delay(getDt() * 4f);
        var s = Sequence.Create(t).Group(delay);
        Assert.IsTrue(t.isAlive);
        yield return s.ToYieldInstruction();
        Assert.IsFalse(t.isAlive);
    }

    [UnityTest]
    public IEnumerator ProcessAllDoesntLeaveUnreleasableTweens() {
        Tween.StopAll();
        Assert.AreEqual(0, tweensCount);
        var t1 = createCustomTween(0.0001f);
        var t2 = createCustomTween(0.1f);
        if (Random.value < 0.5f) {
            t1.Group(t2);
        } else {
            t1.Chain(t2);
        }
        while (t1.interpolationFactor < 1f) {
            yield return null;
        }
        Assert.IsTrue(t1.isAlive);
        Assert.IsTrue(t2.isAlive);
        Assert.AreNotEqual(0, tweensCount);
        Tween.StopAll();
        Assert.AreEqual(0, tweensCount);
    }

    [Test]
    public void ProcessAllDoesntLeaveUnreleasableTweensWhenTargetDestroyed() {
        if (tweensCount != 0) {
            Tween.StopAll();
        }
        Assert.AreEqual(0, tweensCount);
        var target1 = new GameObject();
        var t1 = Tween.Custom(target1, 0, 1,0.0001f, delegate{});
        var t2 = createCustomTween(0.1f);
        t1.Group(t2);
        Object.DestroyImmediate(target1);
        Assert.AreEqual(3, tweensCount);
        Assert.IsTrue(t1.isAlive);
        Tween.StopAll();
        Assert.AreEqual(0, tweensCount);
    }

    [Test]
    public void SequenceCompleteWhenMoreThanTwo() {
        createTween()
            .Group(createTween())
            .Group(createTween())
            .Complete();
    }

    [Test]
    public void SequenceDuration() {
        var dur1 = Random.value;
        var cycles1 = Random.Range(1, 5);
        var s = Sequence.Create(Tween.Custom(this, 0, 1, dur1, cycles: cycles1, onValueChange: delegate{}));
        Assert.AreEqual(dur1 * cycles1, s.duration);
        Assert.AreEqual(dur1 * cycles1, s.durationTotal);
        var dur2 = dur1 + Random.value;
        var cycles2 = cycles1 + Random.Range(1, 5);
        s.Group(Tween.Custom(this, 0, 1, dur2, cycles: cycles2, onValueChange: delegate{}));
        Assert.AreEqual(dur2 * cycles2, s.duration);
        Assert.AreEqual(dur2 * cycles2, s.durationTotal);
        s.Chain(Tween.Delay(1));
        var expected = dur2 * cycles2 + 1;
        const float tolerance = 0.0001f;
        Assert.AreEqual(expected, s.duration, tolerance);
        Assert.AreEqual(expected, s.durationTotal, tolerance);
        var sequenceCycles = Random.Range(1, 100);
        s.SetRemainingCycles(sequenceCycles);
        Assert.AreEqual(expected, s.duration, tolerance);
        Assert.AreEqual(expected * sequenceCycles, s.durationTotal, tolerance);
        s.Complete();
    }

    [UnityTest]
    public IEnumerator SequenceCycles() {
        Application.targetFrameRate = 200;
        int cyclesDone = 0;
        var iniCycles = Random.Range(3, 10);
        // const int iniCycles = 3;
        var s = Sequence.Create(iniCycles)
            .Chain(Tween.PositionX(transform, 3.14f, getDt() * 2f, cycles: 2))
            .ChainCallback(this, _ => cyclesDone++);
        Assert.AreEqual(iniCycles, s.cyclesTotal);
        Assert.AreEqual(0, s.cyclesDone);
        while (s.cyclesDone == 0) {
            yield return null;
        }
        Assert.AreEqual(iniCycles, s.cyclesTotal);
        Assert.AreEqual(cyclesDone, s.cyclesDone);
        var pendingCycles = Random.Range(2, 10);
        // const int pendingCycles = 2;
        s.SetRemainingCycles(pendingCycles);
        var expectedCycles = cyclesDone + pendingCycles;
        Assert.AreEqual(expectedCycles, s.cyclesTotal);
        Assert.AreEqual(1, s.cyclesDone);
        while (s.cyclesDone == 1) {
            yield return null;
        }
        Assert.AreEqual(expectedCycles, s.cyclesTotal);
        Assert.AreEqual(cyclesDone, s.cyclesDone);
        s.Complete();
        Application.targetFrameRate = targetFrameRate;
    }

    [Test]
    public void AwaitNewlyCreatedSequenceDoesntCompleteSyncButInSameFrame() {
        bool isCompleted = false;
        #pragma warning disable CS4014
        AwaitNewlyCreatedSequenceDoesntCompleteSyncButInSameFrame_internal(() => isCompleted = true);
        #pragma warning restore CS4014
        Assert.IsFalse(isCompleted);
    }

    static async Task AwaitNewlyCreatedSequenceDoesntCompleteSyncButInSameFrame_internal([NotNull] Action callback) {
        int frameStarted = Time.frameCount;
        await Sequence.Create();
        Assert.AreEqual(frameStarted, Time.frameCount); // completes in same frame, but not immediately
        callback();
    }

    [Test]
    public void UsingDefaultSequenceCtor() {
        for (int i = 0; i < 26; i++) {
            expectDefaultCtorError();
        }
        var seq = new Sequence();
        seq.elapsedTime += seq.elapsedTime; // +3
        _ = seq.cyclesTotal;
        _ = seq.cyclesDone;
        _ = seq.duration;
        seq.elapsedTimeTotal += seq.elapsedTimeTotal; // +3
        _ = seq.durationTotal;
        _ = seq.progress;
        _ = seq.progressTotal;
        var t = Tween.Delay(0.0001f);
        seq.Group(t);
        seq.Chain(t);
        seq.ChainCallback(() => {});
        seq.ChainCallback(this, delegate {});
        seq.ChainDelay(1f);
        seq.Stop(); // no error
        seq.Complete(); // no error
        seq.SetRemainingCycles(false);
        seq.SetRemainingCycles(10);
        seq.isPaused = !seq.isPaused; // +2
        seq.Chain(Sequence.Create());
        seq.Group(Sequence.Create());
        seq.timeScale += seq.timeScale; // +3

        #if PRIME_TWEEN_DOTWEEN_ADAPTER
        expectDefaultCtorError();
        seq.ChainLast(t);
        #endif
        Tween.StopAll();
    }

    static void expectDefaultCtorError() {
        LogAssert.Expect(LogType.Error, Constants.defaultCtorError);
    }

    [UnityTest]
    public IEnumerator OnCompleteCallbackIsCalledOnlyOnce() {
        int numT1Completed = 0;
        var t1 = Tween.Delay(0.05f).OnComplete(() => numT1Completed++);
        var s = t1.Chain(Tween.Delay(0.05f));
        while (t1.interpolationFactor < 1f) {
            yield return null;
        }
        Assert.IsTrue(t1.isAlive);
        Assert.AreEqual(1, numT1Completed);
        s.Complete();
        Assert.AreEqual(1, numT1Completed);
    }

    [UnityTest]
    public IEnumerator SequenceElapsedTime2() {
        Application.targetFrameRate = 200;
        float dt = getDt();
        var cycleTimeStart = Time.time;
        float duration = dt * 5f;
        var s = Tween.Delay(duration)
            .Chain(Tween.Delay(duration))
            .ChainCallback(() => cycleTimeStart = Time.time);
        s.SetRemainingCycles(2);
        var timeStart = Time.time;
        while (s.isAlive) {
            float tolerance = dt * 2;
            Assert.AreEqual(Time.time - timeStart, s.elapsedTimeTotal, tolerance);
            Assert.AreEqual(Time.time - cycleTimeStart, s.elapsedTime, tolerance);
            yield return null;
        }
        Application.targetFrameRate = targetFrameRate;
    }

    [Test]
    public void SequenceTimeScale() {
        var seq = Sequence.Create();
        const float seqTimeScale = 1.5f;
        seq.timeScale = seqTimeScale;
        var t2 = Tween.Delay(0.1f);
        t2.timeScale = 0.5f;
        LogAssert.Expect(LogType.Error, new Regex("'timeScale' was ignored after adding"));
        seq.Chain(t2);
        foreach (var t in seq.GetAllChildren()) {
            Assert.AreEqual(0.5f, t.data.timeScale);
        }

        seq.timeScale = 2f;
        Assert.AreEqual(2f, seq.root.timeScale);
        foreach (var t in seq.GetAllChildren()) {
            Assert.AreEqual(0.5f, t.data.timeScale);
        }

        LogAssert.NoUnexpectedReceived();
        Tween.StopAll();
    }

    [Test]
    public void CompletingMainSequenceCompletesAllChildrenSequencesAndTweens() {
        int numCallback = 0;
        var nestedSeq2 = Sequence.Create()
            .ChainDelay(1)
            .ChainCallback(() => {
                Assert.AreEqual(1, numCallback);
                numCallback++;
            });
        var nestedSeq1 = Sequence.Create()
            .ChainDelay(1)
            .ChainCallback(() => {
                Assert.AreEqual(0, numCallback);
                numCallback++;
            })
            .Chain(nestedSeq2);
        Sequence.Create(Tween.Delay(1))
            .Group(nestedSeq1)
            .Complete();
        Assert.AreEqual(2, numCallback);
    }

    [Test]
    public void ManipulateNestedSequence() {
        var nested = Sequence.Create(Tween.Delay(getDt()));
        Sequence.Create().Group(nested);

        var tween = Tween.Delay(getDt());

        expectCantManipulateTweenInsideSequence();
        nested.Group(tween);

        expectCantManipulateTweenInsideSequence();
        nested.Chain(Tween.Delay(getDt()));

        expectCantManipulateTweenInsideSequence();
        nested.ChainCallback(delegate {});

        expectCantManipulateTweenInsideSequence();
        nested.ChainCallback(this, delegate {});

        expectCantManipulateTweenInsideSequence();
        nested.ChainCallback(this, delegate {});

        expectCantManipulateTweenInsideSequence();
        nested.ChainDelay(0.1f);

        expectCantManipulateTweenInsideSequence();
        nested.Stop();

        expectCantManipulateTweenInsideSequence();
        nested.Complete();

        expectCantManipulateTweenInsideSequence();
        nested.SetRemainingCycles(10);

        expectCantManipulateTweenInsideSequence();
        expectCantManipulateTweenInsideSequence();
        nested.isPaused = !nested.isPaused;

        expectCantManipulateTweenInsideSequence();
        nested.Chain(Sequence.Create());

        expectCantManipulateTweenInsideSequence();
        nested.Group(Sequence.Create());

        expectCantManipulateTweenInsideSequence();
        nested.elapsedTime += 0.1f;

        expectCantManipulateTweenInsideSequence();
        nested.elapsedTimeTotal += 0.1f;

        expectCantManipulateTweenInsideSequence();
        expectCantManipulateTweenInsideSequence();
        nested.timeScale += 1f;
    }

    [Test]
    public async Task OnCompleteExceptionInsideSequenceDoesntStopSequence() {
        // by design: if one tween's onComplete throws exception, Sequence should continue to play further
        {
            int numCallbacks = 0;
            expectOnCompleteException();
            await Sequence.Create(
                    Tween.Delay(getDt(), () => throw new Exception())
                    )
                .Chain(Tween.Delay(getDt(), () => numCallbacks++))
                .ChainCallback(() => numCallbacks++)
                ;
            Assert.AreEqual(2, numCallbacks);
        }
        {
            int numCallbacks = 0;
            expectOnCompleteException();
            await Sequence.Create(
                    Tween.Delay(this, getDt(), _ => throw new Exception())
                    )
                .Chain(Tween.Delay(getDt(), () => numCallbacks++))
                .ChainCallback(() => numCallbacks++);
            Assert.AreEqual(2, numCallbacks);
        }
    }

    [UnityTest]
    public IEnumerator TestSequenceEnumerator() {
        const float duration = 0.0001f;
        var t1 = Tween.Delay(duration);
        var s1 = Sequence.Create(t1);

        var t2 = Tween.Delay(duration);
        var s2 = Sequence.Create(t2);

        var t3 = Tween.Delay(duration);
        var s3 = Sequence.Create(t3);

        s2.Chain(s3);
        s1.Chain(s2);

        Tween[] expected = new[] {
            s1.root, t1,
            s2.root, t2,
            s3.root, t3
        };
        int i = 0;
        foreach (var t in s1.GetAllTweens()) {
            Assert.AreEqual(expected[i], new Tween(t));
            i++;
        }
        Assert.AreEqual(6, i);
        i = 1;
        foreach (var t in s1.GetSelfChildren()) {
            Assert.AreEqual(expected[i], new Tween(t));
            i++;
        }
        Assert.AreEqual(3, i);

        i = 3;
        foreach (var t in s2.GetSelfChildren()) {
            Assert.AreEqual(expected[i], new Tween(t));
            i++;
        }
        Assert.AreEqual(5, i);

        i = 5;
        foreach (var t in s3.GetSelfChildren()) {
            Assert.AreEqual(expected[i], new Tween(t));
            i++;
        }
        Assert.AreEqual(6, i);

        yield return null;
    }

    [Test]
    public void TestSequenceEnumeratorWithEmptySequences() {
        var s1 = Sequence.Create();
        var s2 = Sequence.Create();
        var s3 = Sequence.Create();
        s1.Chain(s2)
            .Chain(s3);

        var expected = new[] { s1.root, s2.root, s3.root };

        int i = 0;
        foreach (var t in s1.GetAllTweens()) {
            Assert.AreEqual(expected[i], new Tween(t));
            i++;
        }

        i = 1;
        foreach (var t in s1.GetSelfChildren()) {
            Assert.AreEqual(expected[i], new Tween(t));
            i++;
        }

        foreach (var _ in s2.GetSelfChildren()) {
            Assert.Fail();
        }

        foreach (var _ in s3.GetSelfChildren()) {
            Assert.Fail();
        }
    }

    [Test]
    public void MainSequenceSiblingShouldBeEmpty() {
        Sequence main = Sequence.Create();
        Sequence nested1 = Sequence.Create(Tween.Delay(getDt()));
        main.Chain(nested1);
        Assert.AreEqual(main.root.tween.next, nested1.root.tween);
        Assert.IsNull(main.root.tween.nextSibling);
        var nested2 = Sequence.Create(Tween.Delay(getDt()));
        main.Chain(nested2);
        Assert.AreEqual(main.root.tween.next, nested1.root.tween);
        Assert.IsNull(main.root.tween.nextSibling);
    }

    [Test]
    public void CompleteSequenceOnBackwardCycle() {
        var target = new GameObject().transform;
        const float duration = 1f;
        Sequence.Create(2, Sequence.SequenceCycleMode.Rewind)
            .Chain(Tween.PositionY(target, 1, duration))
            .Chain(Tween.PositionY(target, 0, duration)).Complete();
        Assert.AreEqual(0f, target.position.y);
        Object.Destroy(target.gameObject);
    }

    [UnityTest]
    public IEnumerator CompleteSequenceOnBackwardCycle2() {
        var target = GameObject.CreatePrimitive(PrimitiveType.Cube).transform;
        yield return test(2, false);
        yield return test(2, true);
        yield return test(3, false);
        yield return test(3, true);
        yield return test(4, false);
        yield return test(4, true);

        IEnumerator test(int cycles, bool completeImmediately) {
            // print($"test {cycles}, {completeImmediately}");
            const float duration = 0.0001f;
            int numCompleted = 0;
            var seq = Sequence.Create(cycles, Sequence.SequenceCycleMode.Yoyo, Ease.OutBounce)
                .Chain(Tween.PositionY(target, 1f, duration).OnComplete(() => numCompleted++))
                .Chain(Tween.PositionY(target, 0f, duration));
            if (completeImmediately) {
                seq.Complete();
            } else {
                yield return seq.ToYieldInstruction();
            }
            if (completeImmediately) {
                Assert.AreEqual(1, numCompleted);
            } else {
                Assert.AreEqual((cycles + 1) / 2, numCompleted);
            }
            Assert.AreEqual(0f, target.position.y);
        }

        Object.Destroy(target.gameObject);
    }

    [UnityTest]
    public IEnumerator CompleteInfiniteSequence() {
        int numCallbacks = 0;
        var duration = Mathf.Max(minDuration, Random.Range(0f, getDt()));
        Assert.IsTrue(duration >= minDuration);
        var s = Sequence.Create(-1)
            .ChainDelay(duration)
            .ChainCallback(() => numCallbacks++);
        float timeStart = Time.time;
        yield return null;
        yield return null;
        var cyclesDoneExpected = Mathf.FloorToInt((Time.time - timeStart) / duration);
        Assert.AreEqual(cyclesDoneExpected, s.cyclesDone);
        s.Complete();
        Assert.AreEqual(cyclesDoneExpected + 1, numCallbacks);
    }

    [Test]
    public void OnCompleteWithSequenceBackward() {
        int numCompleted = 0;
        int cycles = Random.Range(1, 7);
        var seq = Sequence.Create(cycles)
            .Group(Sequence.Create(cycles)
                .Group(Sequence.Create(cycles)
                    .Group(Tween.Delay(0.1f, () => numCompleted++))));
        seq.isPaused = true;
        seq.elapsedTimeTotal = float.MaxValue;
        int cyclesTotal = cycles * cycles * cycles;
        int cyclesExpected = cyclesTotal;
        Assert.AreEqual(cyclesExpected, numCompleted);
        seq.elapsedTimeTotal = 0f;
        cyclesExpected += cyclesTotal - 1;
        Assert.AreEqual(cyclesExpected, numCompleted);

        seq.elapsedTimeTotal = float.MaxValue;
        cyclesExpected += cyclesTotal;
        Assert.AreEqual(cyclesExpected, numCompleted);

        seq.elapsedTimeTotal = 0f;
        cyclesExpected += cyclesTotal - 1;
        Assert.AreEqual(cyclesExpected, numCompleted);

        seq.Complete();
        cyclesExpected += cycles * cycles;
        Assert.AreEqual(cyclesExpected, numCompleted);
    }

    [UnityTest]
    public IEnumerator UnpauseCompletedSequenceShouldRelease() {
        var s = Tween.Delay(getDt()).Chain(Tween.Delay(getDt()));
        s.isPaused = true;
        s.elapsedTimeTotal = s.durationTotal;
        yield return null;
        yield return null;
        Assert.IsTrue(s.isAlive);
        s.isPaused = false;
        Assert.IsFalse(s.isAlive);
    }

    [UnityTest]
    public IEnumerator UnpauseCompletedTweenShouldRelease() {
        var t = Tween.Delay(getDt());
        t.isPaused = true;
        t.elapsedTimeTotal = t.durationTotal;
        yield return null;
        yield return null;
        Assert.IsTrue(t.isAlive);
        t.isPaused = false;
        Assert.IsFalse(t.isAlive);
    }

    [UnityTest]
    public IEnumerator NestedSequenceCycles() {
        string result = "";
        void append(int i) => result += i.ToString();

        yield return createNestedSequences(getDt()).ToYieldInstruction();
        Assert.AreEqual("12332331233233", result);

        createNestedSequences(getDt()).Complete();
        Assert.AreEqual("1233233", result);

        PrimeTweenConfig.warnZeroDuration = false;
        yield return createNestedSequences(0f).ToYieldInstruction();
        Assert.AreEqual("12332331233233", result);

        createNestedSequences(0f).Complete();
        Assert.AreEqual("1233233", result);

        Sequence createNestedSequences(float duration) {
            result = "";
            var s3 = Sequence.Create(2).ChainDelay(duration).ChainCallback(() => append(3));
            var s2 = Sequence.Create(2).ChainDelay(duration).ChainCallback(() => append(2)).Chain(s3);
            var s1 = Sequence.Create(2).ChainDelay(duration).ChainCallback(() => append(1)).Chain(s2);
            return s1;
        }
    }

    [UnityTest]
    public IEnumerator AddTweenToStartedSequence() {
        var s = Sequence.Create(Tween.Delay(1f));
        yield return null;
        Assert.IsTrue(s.isAlive);
        Assert.AreNotEqual(0f, s.elapsedTimeTotal);

        var t = Tween.Delay(0.1f);

        expectError();
        s.Chain(t);
        expectError();
        s.Group(t);
        expectError();
        s.Chain(Sequence.Create());
        expectError();
        s.Group(Sequence.Create());

        Tween.StopAll();
        LogAssert.NoUnexpectedReceived();
        void expectError() => LogAssert.Expect(LogType.Error, Constants.animationAlreadyStarted);
    }

    [UnityTest]
    public IEnumerator OnCompleteIgnoredErrorOnSequenceEmergencyStop() {
        expectTweenWasStoppedBecauseException();
        expectOnCompleteIgnored();
        yield return Tween.Custom(0f, 1f, getDt(), _ => throw new Exception())
            .Chain(Tween.Delay(getDt(), Assert.Fail))
            .ToYieldInstruction();
    }

    [UnityTest]
    public IEnumerator SequenceIsEmptyEnumerationBug() {
        yield return Sequence.Create()
            .Chain(Sequence.Create().Chain(Sequence.Create()))
            .Chain(Sequence.Create().Chain(Sequence.Create()))
            .ToYieldInstruction();
    }

    [UnityTest]
    public IEnumerator SequenceIsEmptyEnumerationCausesStackOverflowBug() {
        var duration = getDt() * 2f;
        yield return Sequence.Create()
            .Chain(Tween.Delay(duration).Chain(Sequence.Create()))
            .Chain(Tween.Delay(duration).Chain(Sequence.Create()))
            .ToYieldInstruction();
    }

    [UnityTest]
    public IEnumerator SequenceOnComplete() {
        int numCompleted = 0;
        yield return Sequence.Create(2, Sequence.SequenceCycleMode.Yoyo)
            .ChainDelay(getDt())
            .OnComplete(() => numCompleted++)
            .ToYieldInstruction();
        Assert.AreEqual(1, numCompleted);

        numCompleted = 0;
        yield return Sequence.Create(2, Sequence.SequenceCycleMode.Yoyo)
            .ChainDelay(getDt())
            .OnComplete(this, _ => numCompleted++)
            .ToYieldInstruction();
        Assert.AreEqual(1, numCompleted);
    }

    [Test]
    public void LongGroupAfterChainOpSequence() {
        var seq = Tween.Delay(0.1f)
            .Chain(Tween.Delay(0.1f));

        seq.Group(Sequence.Create(Tween.Delay(1f)));
        Assert.AreEqual(1.1f, seq.duration);

        seq.Group(Sequence.Create(Tween.Delay(2f)));
        Assert.AreEqual(2.1f, seq.duration);

        seq.Chain(Sequence.Create(Tween.Delay(1f)));
        Assert.AreEqual(3.1f, seq.duration);

        seq.Stop();
    }

    [Test]
    public void LongGroupAfterChainOpTween() {
        var seq = Tween.Delay(0.1f)
            .Chain(Tween.Delay(0.1f));

        seq.Group(Tween.Delay(1f));
        Assert.AreEqual(1.1f, seq.duration);

        seq.Group(Tween.Delay(2f));
        Assert.AreEqual(2.1f, seq.duration);

        seq.Chain(Tween.Delay(1f));
        Assert.AreEqual(3.1f, seq.duration);

        seq.Stop();
    }
    [UnityTest]
    public IEnumerator SequenceRecursiveCreate() {
        var target = new GameObject();
        float duration = getDt() * 2f;
        Sequence seq1 = default;
        Sequence seq2 = default;
        createSeq1();

        void createSeq1() {
            // print("createSeq1");
            seq1.Stop();
            seq1 = Sequence.Create()
                .Chain(Tween.Delay(target, duration: duration))
                .ChainCallback(() => {
                })
                .Chain(Tween.Delay(target, duration: duration))
                .ChainCallback(() => {
                    createSeq2();
                });
            createSeq2();
        }

        void createSeq2() {
            // print("createSeq2");
            seq2.Stop();
            seq2 = Sequence.Create()
                .Chain(Tween.Delay(target, duration: duration))
                .ChainCallback(() => {
                })
                .Chain(Tween.Delay(target, duration: duration))
                .ChainCallback(() => {
                    seq1.Complete();
                });
        }

        while (seq1.isAlive || seq2.isAlive) {
            yield return null;
        }
        yield return null;
    }

    [UnityTest]
    public IEnumerator StopSequenceFromCallback2() {
        Sequence s = default;
        int numCyclesDone = 0;
        var duration = getDt() * 2f;
        s = Sequence.Create(2)
            .ChainDelay(duration)
            .ChainDelay(duration)
            .ChainCallback(() => {
                numCyclesDone++;
                // print($"numCyclesDone: {numCyclesDone}");
                if (numCyclesDone == 2) {
                    s.timeScale = 0f;
                    s.SetRemainingCycles(3);
                    Tween.StopAll();
                }
            });
        yield return s.ToYieldInstruction();
        yield return null;
    }

    [UnityTest]
    public IEnumerator StopSequenceFromCallback3() {
        var dt = getDt();
        var s2 = Sequence.Create().ChainDelay(dt * 10f);
        var s1 = Sequence.Create()
            .ChainDelay(dt)
            .ChainCallback(() => {
                Assert.IsTrue(s2.isAlive);
                s2.Stop();
                Assert.IsFalse(s2.isAlive);
            });
        yield return s1.ToYieldInstruction();
        yield return null;
    }

    [UnityTest]
    public IEnumerator StopSequenceFromCallback4() {
        Sequence s = default;
        s = Sequence.Create(2)
            .ChainDelay(getDt())
            .ChainCallback(() => {
                s.Stop();
            });
        yield return s.ToYieldInstruction();
        yield return null;
    }

    [UnityTest]
    public IEnumerator SequenceOnCompleteIsCalledAfterAllChildren() {
        bool delayCompleted = false;
        bool sequenceCompleted = false;
        var delay = Tween.Delay(getDt(), () => {
            Assert.IsTrue(!delayCompleted && !sequenceCompleted);
            delayCompleted = true;
        });
        yield return Sequence.Create(delay).OnComplete(() => {
            Assert.IsTrue(delayCompleted);
            Assert.IsFalse(sequenceCompleted);
            sequenceCompleted = true;
        });
        Assert.IsTrue(delayCompleted && sequenceCompleted);
    }

    [UnityTest]
    public IEnumerator SequenceIsNotAliveInOnComplete() {
        Sequence s = default;
        int onCompleted = 0;
        s = Sequence.Create()
            .OnComplete(() => {
                Assert.IsFalse(s.isAlive);
                onCompleted++;
            });
        yield return null;
        Assert.AreEqual(1, onCompleted);
        Assert.IsFalse(s.isAlive);
    }

    [UnityTest]
    public IEnumerator CompleteSequenceInOnComplete() {
        Sequence s = default;
        s = Sequence.Create()
            .OnComplete(() => {
                Assert.IsFalse(s.isAlive);
                s.Complete();
            });
        yield return s.ToYieldInstruction();
    }

    [UnityTest]
    public IEnumerator SequenceCyclesDoneGetterFromCustomTweenChild() {
        Sequence s = default;
        s = Sequence.Create()
            .Chain(Tween.Custom(0f, 1f, getDt(), val => {
                expectRecursiveCallError();
                s.progress += 0f;
                Assert.AreEqual(0, s.cyclesDone);
            }));
        yield return s.ToYieldInstruction();
    }

    [UnityTest]
    public IEnumerator SequenceSetProgressRecursively() {
        int i = 0;
        Sequence s = default;
        s = Sequence.Create()
            .Chain(Tween.Custom(0f, 1f, getDt() * 2f, _ => {
                i++;
                Assert.IsTrue(i < 100);
                expectRecursiveCallError();
                s.progress += 0.01f;
                expectRecursiveCallError();
                s.progressTotal += 0.01f;
                expectRecursiveCallError();
                s.elapsedTime += 0.01f;
                expectRecursiveCallError();
                s.elapsedTimeTotal += 0.01f;
            }));
        yield return s.ToYieldInstruction();
    }

    static void expectRecursiveCallError() => LogAssert.Expect(LogType.Error, Constants.recursiveCallError);

    [UnityTest]
    public IEnumerator SetSequenceProgressTo_1_AtCycleEnd() {
        Sequence s = default;
        s = Sequence.Create(2)
            .ChainDelay(0.01f)
            .Chain(Tween.Custom(0f, 1f, 0.01f, val => {
                if (val == 1f) {
                    // print("custom val == 1f");
                    expectRecursiveCallError();
                    s.progress = 1f;
                }
            }));
        yield return s.ToYieldInstruction();
    }
    [UnityTest]
    public IEnumerator SetSequenceProgressTo_0_AtCycleEnd() {
        Sequence s = default;
        s = Sequence.Create(2)
            .Chain(Tween.Custom(0f, 1f, 0.01f, val => {
                if (val == 1f) {
                    // print("custom val == 1f");
                    expectRecursiveCallError();
                    s.progress = 0f;
                }
            }));
        yield return s.ToYieldInstruction();
    }

    [UnityTest]
    public IEnumerator SequenceStopOrCompleteFromCustomCallback() {
        yield return test(s => {
            // callback can be called twice when Time.delta time is less than TweenSetting.minDuration
            s.Complete();
        });
        yield return test(s => s.Stop());
        IEnumerator test(Action<Sequence> action) {
            Sequence s = default;
            s = Sequence.Create(2)
                .ChainDelay(minDuration)
                .Chain(Tween.Custom(0f, 1f, minDuration, _ => {
                    Assert.IsTrue(s.isAlive);
                    action(s);
                    Assert.IsFalse(s.isAlive);
                }));
            yield return s.ToYieldInstruction();
        }
    }

    [UnityTest]
    public IEnumerator StopSequenceFromSequenceOnComplete() {
        Sequence sequence = default;
        sequence = Sequence.Create()
            .OnComplete(() => {
                Assert.IsFalse(sequence.isAlive);
                sequence.Stop();
            });
        yield return sequence.ToYieldInstruction();
    }

    [UnityTest]
    public IEnumerator SettingSequenceRemainingCyclesInOnComplete() {
        Sequence s = default;
        s = Sequence.Create()
            .OnComplete(() => {
                Assert.IsFalse(s.isAlive);
                expectIsDeadError();
                s.SetRemainingCycles(2);
            });
        yield return s.ToYieldInstruction();
    }

    [UnityTest]
    public IEnumerator AddingStartedTweenToSequenceIsOk() {
        var tween = Tween.PositionY(transform, getDt() * 3, 1f);
        yield return null;
        Assert.IsTrue(tween.isAlive);
        Sequence.Create(tween).Stop();
        Assert.IsFalse(tween.isAlive);
    }

    #if PRIME_TWEEN_DOTWEEN_ADAPTER
    [Test]
    public void SequenceDurationAfterChainLast() {
        var seq = Tween.Delay(1f)
            .Group(Tween.Delay(0.5f))
            .ChainLast(Tween.Delay(0.1f));
        Assert.AreEqual(1f, seq.duration);

        seq.ChainLast(Tween.Delay(1f));
        Assert.AreEqual(1.6f, seq.duration);
    }

    [UnityTest]
    public IEnumerator PrependInterval() {
        {
            var duration = getDt();
            yield return Sequence.Create(Tween.Delay(duration)).PrependInterval(duration).ToYieldInstruction();
        }
        {
            var s = Sequence.Create()
                .ChainCallback(() => Assert.Fail())
                .PrependInterval(100f);
            yield return null;
            s.Stop();
        }
    }

    [Test]
    public void DontLogCantManipulateErrorInAdapter() {
        var target = new GameObject().transform;
        {
            var t = Tween.Delay(target, 1f);
            Sequence.Create(t);
            Assert.AreEqual(0, target.DOKill());
            Assert.AreEqual(0, target.DOKill(true));
            LogAssert.NoUnexpectedReceived();
        }
        Object.DestroyImmediate(target.gameObject);
    }

    [UnityTest]
    public IEnumerator PrependInterval2() {
        var duration = getDt();
        var s = Sequence.Create(Tween.Delay(duration)).PrependInterval(duration);
        Assert.AreEqual(duration * 2f, s.duration);
        yield return s.ToYieldInstruction();
    }

    [Test]
    public void AdapterJoinWithStartDelay() {
        var t1 = Tween.Delay(1f);
        var t2 = Tween.Position(transform, default, 1f, startDelay: 1f);
        var seq = Sequence.Create(t1)
            .Join(t2);
        Assert.AreEqual(0f, t1.tween.data.waitDelay);
        Assert.AreEqual(1f, t2.tween.data.waitDelay);
        Assert.AreEqual(2f, seq.duration);
        var t3 = Tween.Position(transform, default, 1f, startDelay: 1f);
        seq.Join(t3);
        Assert.AreEqual(2f, t3.tween.data.waitDelay);
        Assert.AreEqual(3f, seq.duration);
    }

    [Test]
    public void AdapterAppendWithStartDelay() {
        var t1 = Tween.Delay(1f);
        var t2 = Tween.Position(transform, default, 1f, startDelay: 1f);
        var seq = Sequence.Create(t1)
            .Append(t2);
        Assert.AreEqual(0f, t1.tween.data.waitDelay);
        Assert.AreEqual(2f, t2.tween.data.waitDelay);
        Assert.AreEqual(3f, seq.duration);
        var t3 = Tween.Position(transform, default, 1f, startDelay: 1f);
        seq.Append(t3);
        Assert.AreEqual(4f, t3.tween.data.waitDelay);
        Assert.AreEqual(5f, seq.duration);
    }

    [Test]
    public void AdapterSetDelay() {
        var t = Tween.Position(transform, default, 1f);
        t.SetDelay(1f);
        Assert.AreEqual(1f, t.tween.data.startDelay);
        Assert.AreEqual(2f, t.duration);
    }

    [Test]
    public void AdapterSetRelative() {
        var startValue = Vector3.one;
        transform.position = startValue;
        var diff = new Vector3(1f, 2f, 3f);
        var t = transform.DOMove(diff, 1f)
            .SetRelative();
        var expected = startValue + diff;
        Assert.AreEqual(expected, t.tween.managedData.endValueOrDiff.vector3);
        t.Complete();
        Assert.AreEqual(expected, transform.position);
    }

    [Test]
    public void AdapterSetTarget() {
        var t = DOVirtual.Float(-1f, 1f, 1f, delegate { })
            .SetTarget(transform);
        Assert.AreEqual(transform, t.tween.managedData.target);
    }

    [Test]
    public void AdapterSetFrom() {
        transform.position = new Vector3(1f, 2f, 3f);
        var startValue = -Vector3.one;
        var endValue = Vector3.one;
        var t = transform.DOMove(endValue, 1f)
            .From(startValue);
        Assert.IsFalse(t.tween.data.startFromCurrent);
        Assert.AreEqual(t.tween.data.startValue.vector3, startValue);
        Assert.AreEqual(t.tween.managedData.endValueOrDiff.vector3, endValue - startValue);
        t.progress = 0f;
        Assert.AreEqual(startValue, transform.position);
        t.progress = 1f;
        Assert.AreEqual(endValue, transform.position);
    }
    #endif

    [UnityTest]
    public IEnumerator Insert1() {
        var seq = Sequence.Create();
        int numCallbacks = 0;
        seq.InsertCallback(0f, () => numCallbacks++);
        Assert.AreEqual(0f, seq.duration);
        yield return seq.ToYieldInstruction();
        Assert.AreEqual(1, numCallbacks);
        LogAssert.NoUnexpectedReceived();
    }

    [UnityTest]
    public IEnumerator Insert2() {
        var seq = Sequence.Create();
        int i = 0;
        seq.InsertCallback(0f, transform, delegate {
            Assert.AreEqual(0, i);
            i++;
        });
        Assert.AreEqual(0f, seq.duration);

        float dt = getDt();
        float longestTime = dt * 10;
        seq.InsertCallback(longestTime, () => {
            Assert.AreEqual(2, i);
            i++;
        });
        Assert.AreEqual(longestTime, seq.duration);

        seq.InsertCallback(dt, () => {
            Assert.AreEqual(1, i);
            i++;
        });
        Assert.AreEqual(longestTime, seq.duration);

        yield return seq.ToYieldInstruction();
    }

    [UnityTest]
    public IEnumerator Insert3() {
        var seq = Sequence.Create();

        seq.Insert(0f, Tween.Delay(1f));
        Assert.AreEqual(1f, seq.duration);

        seq.Insert(0.5f, Tween.Delay(1f));
        Assert.AreEqual(1.5f, seq.duration);

        seq.Insert(0.5f, Tween.Delay(1f));
        Assert.AreEqual(1.5f, seq.duration);

        seq.Insert(2f, Tween.Delay(1f));
        Assert.AreEqual(3f, seq.duration);

        seq.Chain(Tween.Delay(1f));
        Assert.AreEqual(4f, seq.duration);

        seq.Insert(5f, Tween.Delay(1f));
        Assert.AreEqual(6f, seq.duration);

        seq.Group(Tween.Delay(1f));
        Assert.AreEqual(6f, seq.duration);

        seq.Group(Tween.Delay(1.5f));
        Assert.AreEqual(6.5f, seq.duration);

        seq.Stop();
        yield break;
    }

    [Test]
    public void NestSequenceTwice() {
        var main = Sequence.Create();
        var nested = Sequence.Create();
        main.Group(nested);
        LogAssert.Expect(LogType.Error, Constants.nestTwiceError);
        main.Group(nested);
    }

    [UnityTest]
    public IEnumerator SequenceCompleteRemainingCycles() {
        var yoyoOrRewind = Random.value < 0.5f ? Sequence.SequenceCycleMode.Yoyo : Sequence.SequenceCycleMode.Rewind;
        {
            float val = -1f;
            int numCompleted = 0;
            Sequence.Create(9, yoyoOrRewind)
                .Chain(Tween.Custom(0f, 1f, 1f, x => val = x))
                .ChainCallback(() => numCompleted++)
                .Complete();
            Assert.AreEqual(1, numCompleted);
            Assert.AreEqual(1f, val);
        }
        {
            float val = -1f;
            int numCompleted = 0;
            Sequence.Create(10, yoyoOrRewind)
                .Chain(Tween.Custom(0f, 1f, 1f, x => val = x))
                .ChainCallback(() => numCompleted++)
                .Complete();
            Assert.AreEqual(1, numCompleted);
            Assert.AreEqual(0f, val);
        }

        {
            float val = -1f;
            int numCompleted = 0;
            var seq = Sequence.Create(9, yoyoOrRewind)
                .Chain(Tween.Custom(0f, 1f, 100f, x => val = x))
                .ChainCallback(() => numCompleted++);
            seq.progress = 1f;
            yield return null;
            Assert.AreEqual(1, seq.cyclesDone);
            Assert.AreEqual(1, numCompleted);
            seq.Complete();
            Assert.AreEqual(2, numCompleted);
            Assert.AreEqual(1f, val);
        }
        {
            float val = -1f;
            int numCompleted = 0;
            var seq = Sequence.Create(10, yoyoOrRewind)
                .Chain(Tween.Custom(0f, 1f, 100f, x => val = x))
                .ChainCallback(() => numCompleted++);
            seq.progress = 1f;
            yield return null;
            Assert.AreEqual(1, seq.cyclesDone);
            Assert.AreEqual(1, numCompleted);
            seq.Complete();
            Assert.AreEqual(1, numCompleted);
            Assert.AreEqual(0f, val);
        }

        {
            float val = -1f;
            int numCompleted = 0;
            Sequence.Create(Random.Range(1, 100), Sequence.SequenceCycleMode.Restart)
                .Chain(Tween.Custom(0f, 1f, 1f, x => val = x))
                .ChainCallback(() => numCompleted++)
                .Complete();
            Assert.AreEqual(1, numCompleted);
            Assert.AreEqual(1f, val);
        }
    }

    [Test]
    public void GetTweensCountOnSequence() {
        Tween.StopAll();
        Assert.AreEqual(0, Tween.GetTweensCount());
        var seq = Tween.Position(transform, Vector3.one, 1f)
            .Chain(Tween.Position(transform, Vector3.zero, 1f));
        Assert.AreEqual(3, Tween.GetTweensCount()); // two tweens + sequence
        Assert.AreEqual(2, Tween.GetTweensCount(transform)); // two tween on transform
        seq.Stop();
    }

    /// report: https://forum.unity.com/threads/primetween-high-performance-animations-and-sequences.1479609/page-6#post-9802575
    [UnityTest]
    public IEnumerator SequenceUpdatesChildrenOnlyIfUpdatedSelf() {
        var seq = Sequence.Create()
            .ChainDelay(1)
            .Chain(Sequence.Create()
                .Chain(Tween.Custom(0f, 1f, 1f, _ => Assert.Fail())));
        yield return null;
        seq.Stop();
    }

    /// https://github.com/KyryloKuzyk/PrimeTween/discussions/112
    [Test]
    public void SequenceChainOrInsertCallbackBug1() {
        var seq = Sequence.Create()
            .ChainDelay(1f)
            .ChainCallback(() => { })
            .Group(Tween.Delay(1f));
        Assert.AreEqual(2f, seq.duration);
    }
    [Test]
    public void SequenceChainOrInsertCallbackBug2() {
        var seq = Sequence.Create()
            .ChainDelay(1f)
            .ChainCallback(this, _ => { })
            .Group(Tween.Delay(1f));
        Assert.AreEqual(2f, seq.duration);
    }
    [Test]
    public void SequenceChainOrInsertCallbackBug3() {
        var seq = Sequence.Create()
            .ChainDelay(1f)
            .InsertCallback(0.5f, () => { })
            .Group(Tween.Delay(1f));
        Assert.AreEqual(1.5f, seq.duration);
    }
    [Test]
    public void SequenceChainOrInsertCallbackBug4() {
        var seq = Sequence.Create()
            .ChainDelay(1f)
            .InsertCallback(0.5f, this, _ => { })
            .Group(Tween.Delay(1f));
        Assert.AreEqual(1.5f, seq.duration);
    }

    [Test]
    public void SequenceInsertRejectsNegativeOffsets() { // https://github.com/KyryloKuzyk/PrimeTween/issues/202
        var go = new GameObject("NegativeInsert");
        try {
            var tween = Tween.LocalPositionX(go.transform, endValue: 1f, duration: 1f);
            var sequence = Sequence.Create();
            var initialDuration = sequence.duration;
            Assert.AreEqual(0f, initialDuration, "Fresh sequence should start with zero duration");

            LogAssert.Expect(LogType.Error, "Inserting at negative time (-0.1) is not allowed.");
            sequence.Insert(-0.1f, tween);

            Assert.GreaterOrEqual(
                sequence.duration,
                tween.duration,
                "Sequence duration should not shrink when inserting children");
        } finally {
            Object.DestroyImmediate(go);
        }
    }

    [Test]
    public void CompleteWithNegativeTimeScaleTween() {
        var t = Tween.Delay(1f);
        t.timeScale = -1f;
        t.Complete();
        Assert.IsFalse(t.isAlive);
    }

    [Test]
    public void CompleteWithNegativeTimeScaleSequence() {
        var s = Sequence.Create().ChainDelay(1f);
        s.timeScale = -1f;
        s.Complete();
        Assert.IsFalse(s.isAlive);
    }

    [Test]
    public void CompleteSequenceAfterPause() {
        var s = Sequence.Create().ChainDelay(1f);
        s.isPaused = true;
        s.progress = 1f;
        Assert.IsTrue(s.isAlive);
        s.Complete();
        Assert.IsFalse(s.isAlive);
    }

#if PRIME_TWEEN_EXPERIMENTAL
    [Test]
    public void GroupCallback() {
        {
            var seq = Sequence.Create()
                .ChainDelay(1f)
                .ChainDelay(1f)
                .GroupCallback(delegate { });
            ColdData lastTween = GetLastChild(seq);
            Assert.IsNotNull(lastTween.onComplete);
            Assert.IsNotNull(lastTween.onCompleteCallback);
            Assert.IsNull(lastTween.onCompleteTarget);
            Assert.AreEqual(1f, lastTween.data.waitDelay);
            seq.Stop();
        }
        {
            var seq = Sequence.Create()
                .ChainDelay(1f)
                .ChainDelay(1f)
                .GroupCallback(this, delegate { });
            ColdData lastTween = GetLastChild(seq);
            Assert.IsNotNull(lastTween.onComplete);
            Assert.IsNotNull(lastTween.onCompleteCallback);
            Assert.IsNotNull(lastTween.onCompleteTarget);
            Assert.AreEqual(1f, lastTween.data.waitDelay);
            seq.Stop();
        }
    }

    static ColdData GetLastChild(Sequence seq) {
        ColdData lastChild = null;
        foreach (var child in seq.GetSelfChildren()) {
            lastChild = child;
        }
        Assert.IsNotNull(lastChild);
        return lastChild;
    }
#endif
}
#endif
