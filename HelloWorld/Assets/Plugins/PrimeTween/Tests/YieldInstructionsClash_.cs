using System.Collections;
using System.Diagnostics;
using PrimeTween;
using UnityEngine;
using UnityEngine.Assertions;
using Assert = UnityEngine.Assertions.Assert;
using Debug = UnityEngine.Debug;

internal class YieldInstructionsClash : MonoBehaviour {
    int frame;

    void Update() {
        log($"{Time.frameCount} Update()");
        switch (frame) {
            case 0:
                StartCoroutine(cor());
                break;
            case 1:
                Tween.Delay(TweenSettings.minDuration).ToYieldInstruction();
                break;
        }
        frame++;
    }

    IEnumerator cor() {
        log($"{Time.frameCount} cor start");
        int frameStart = Time.frameCount;
        var t = Tween.Delay(TweenSettings.minDuration);
        var enumerator = t.ToYieldInstruction();
        while (enumerator.MoveNext()) {
            var coroutineEnumerator = enumerator as ColdData;
            Assert.AreEqual(t.id, coroutineEnumerator.id);
            yield return enumerator.Current;
        }
        Destroy(gameObject);
        var diff = Time.frameCount - frameStart;
        Assert.AreEqual(1, diff);
        log($"{Time.frameCount} cor DONE");
    }

    [Conditional("_")]
    static void log(string msg) {
        Debug.Log(msg);
    }
}
