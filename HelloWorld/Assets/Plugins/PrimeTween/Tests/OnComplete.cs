#if TEST_FRAMEWORK_INSTALLED
using System;
using System.Collections;
using System.Threading.Tasks;
using NUnit.Framework;
using PrimeTween;
using UnityEngine;
using UnityEngine.TestTools;
using Assert = NUnit.Framework.Assert;
using Object = UnityEngine.Object;

public partial class Tests {
    [Test]
    public void OnCompleteIsCalledImmediatelyAfterCallingComplete() {
        var onCompleteIsCalled = false;
        var t = createTween().OnComplete(() => onCompleteIsCalled = true);
        Assert.IsFalse(onCompleteIsCalled);
        t.Complete();
        Assert.IsTrue(onCompleteIsCalled);
    }

    [Test]
    public void OnCompleteDuplicationThrows() {
        var t = createTween().OnComplete(() => {});
        try {
            t.OnComplete(() => { });
        } catch (Exception e) {
            Assert.IsTrue(e.Message.Contains("Tween already has an onComplete callback"));
            return;
        }
        Assert.Fail();
    }

    [Test]
    public void AddingOnCompleteToInfiniteTween() {
        int numCompleted = 0;
        createInfiniteTween().OnComplete(() => numCompleted++).Complete();
        Assert.AreEqual(1, numCompleted);
    }

    Tween createInfiniteTween() {
        return Tween.Custom(this, 0, 1, 0.01f, cycles: -1, onValueChange: delegate { });
    }

    [Test]
    public void AddingOnCompleteOnDeadTweenDisplaysError() {
        var t = createTween();
        Assert.IsTrue(t.isAlive);
        t.Complete();
        Assert.IsFalse(t.isAlive);
        expectIsDeadError();
        t.OnComplete(delegate { });
        expectIsDeadError();
        t.OnComplete(this, delegate { });
    }

    [Test]
    public async Task OnCompleteTargetDestructionWhileTweenRunning() {
        expectOnCompleteIgnored();
        LogAssert.NoUnexpectedReceived();
        var target = new GameObject();
        await Tween.Custom(0, 1, 0.001f, _ => {
            Object.DestroyImmediate(target);
        }).OnComplete(target, _ => Assert.Fail());
    }

    [Test]
    public void PassingNullToOnComplete() {
        expectOnCompleteIgnored();
        Tween.Delay(minDuration).OnComplete<GameObject>(null, _ => Assert.Fail());
    }

    [UnityTest]
    public IEnumerator PassingDestroyedObjectToOnComplete() {
        var target = new GameObject();
        Object.DestroyImmediate(target);
        expectOnCompleteIgnored();
        yield return Tween.Delay(minDuration).OnComplete(target, _ => Assert.Fail()).ToYieldInstruction();
        LogAssert.NoUnexpectedReceived();
    }
}
#endif
