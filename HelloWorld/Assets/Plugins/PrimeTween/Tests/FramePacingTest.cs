#if TEST_FRAMEWORK_INSTALLED
using System.Collections;
using PrimeTween;
using UnityEngine;
using Assert = NUnit.Framework.Assert;

public class FramePacingTest : MonoBehaviour {
    int frame;
    int numRunning;
    
    void Update() {
        if (frame == 0) {
            StartCoroutine(delayCor());
            StartCoroutine(zeroFramesAnimationCor());
            StartCoroutine(DelaysInSequenceDurationTest());
            for (int i = 2; i <= 10; i++) {
                StartCoroutine(testMult(i));
            }
            Assert.AreEqual(numRunning, 12);
        }
        if (numRunning == 0) {
            Assert.AreEqual(0, Tween.StopAll(this));
            Destroy(gameObject);
        }
        frame++;
    }

    IEnumerator DelaysInSequenceDurationTest() {
        for (int i = 0; i < 1; i++) {
            yield return test();
        }
        IEnumerator test() {
            numRunning++;
            int frameStart = Time.frameCount;
            int numFrames = Random.Range(1, 3);
            float duration = numFrames * (1f / Application.targetFrameRate);
            yield return Tween.Delay(duration)
                .Chain(Tween.Delay(duration))
                .ChainCallback(assert)
                .ToYieldInstruction();
            assert();
            numRunning--;

            void assert() {
                validate(numFrames * 2, frameStart);
            }
        }
    }
    
    IEnumerator delayCor() {
        numRunning++;
        int frameStart = Time.frameCount;
        float deltaTime = 1f / Application.targetFrameRate;
        yield return Tween.Delay(this, deltaTime)
            .OnComplete(() => validate(1, frameStart))
            .ToYieldInstruction();
        validate(1, frameStart);
        numRunning--;
    }

    static void validate(int expected, int frameStart) {
        var frameDelta = Time.frameCount - frameStart;
        var diff = frameDelta - expected;
        /*if (diff != 0) {
            Debug.LogWarning($"frameDelta: {frameDelta}, expected: {expected}");
        } else {
            Debug.Log("ok");
        }*/
        Assert.IsTrue(diff == 0 || diff == 1, $"frameDelta: {frameDelta}, expected: {expected}");
    }
    
    IEnumerator zeroFramesAnimationCor() {
        numRunning++;
        int frameStart = Time.frameCount;
        float duration = 1f / Application.targetFrameRate;
        // print($"zeroFramesAnimationCor {duration}");
        yield return Tween.Custom(this, 0f, 0f, duration, delegate {
                // Assert.AreEqual(Time.frameCount, frameStart + 1);
            })
            .OnComplete(() => validate(1, frameStart))
            .ToYieldInstruction();
        validate(1, frameStart);
        numRunning--;
    }

    IEnumerator testMult(int numFrames) {
        numRunning++;
        int frameStart = Time.frameCount;
        float duration = 1f / Application.targetFrameRate * numFrames;
        // print($"multipleFrameAnimationCor {numFrames}, {duration}");
        yield return Tween.Custom(this, 0f, 0f, duration, delegate {
                // print(Time.deltaTime);
            })
            .OnComplete(() => validate(numFrames, frameStart))
            .ToYieldInstruction();
        validate(numFrames, frameStart);
        // print($"multipleFrameAnimationCor done {numFrames}");
        numRunning--;
    }
}
#endif