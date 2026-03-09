#if TEST_FRAMEWORK_INSTALLED
using System.Collections;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using PrimeTween;
using UnityEngine;
using UnityEngine.TestTools;
using Assert = NUnit.Framework.Assert;
using Object = UnityEngine.Object;

public partial class Tests {
    Transform transform;
    static TweenSettings<Vector3> settingsVector3 => new TweenSettings<Vector3>(Vector3.zero, Vector3.one, getDt() * 2f);
    static bool setTweenCapacityBeforeSplashScreenLoggedError;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSplashScreen)]
    static void beforeSplashScreen() {
        Application.logMessageReceived += OnLogMessageReceived;
        PrimeTweenConfig.SetTweensCapacity(capacityForTest);
        Application.logMessageReceived -= OnLogMessageReceived;
        void OnLogMessageReceived(string condition, string stacktrace, LogType type) => setTweenCapacityBeforeSplashScreenLoggedError = true;
    }

    bool _resetCapacityBetweenTests;

    [OneTimeSetUp]
    public void oneTimeSetup() {
        Assert.IsFalse(setTweenCapacityBeforeSplashScreenLoggedError, "setTweenCapacityBeforeSplashScreenLoggedError");
        transform = GameObject.CreatePrimitive(PrimitiveType.Cube).transform;
        PrimeTweenConfig.SetTweensCapacity(capacityForTest);

        #if UNITY_EDITOR
        var gameView = Resources.FindObjectsOfTypeAll<UnityEditor.EditorWindow>()
            .FirstOrDefault(w => w.GetType().Name == "GameView");
        Assert.IsNotNull(gameView);
        UnityEditor.EditorWindow.GetWindow(gameView.GetType());
        #endif
    }

    const int targetFrameRate = -1;

    [SetUp]
    public void setUp() {
        Application.targetFrameRate = targetFrameRate;
        if (_resetCapacityBetweenTests) {
            PrimeTweenConfig.SetTweensCapacity(1);
        }
    }

    [UnityTearDown]
    public IEnumerator TearDown() {
        if (_resetCapacityBetweenTests && tweensCount > 0) {
            Tween.StopAll();
            yield return null;
            Assert.AreEqual(0, tweensCount);
        }
    }

    static int tweensCount => PrimeTweenManager.Instance.tweensCount;

    [Test]
    public void CreatingTweenWithDestroyedTargetReturnsTweenToPool() {
        if (tweensCount != 0) {
            Tween.StopAll();
        }
        Assert.AreEqual(0, tweensCount);
        var target = new GameObject();
        var targetTr = target.transform;
        Object.DestroyImmediate(target);

        var pool = PrimeTweenManager.Instance.pool;
        var poolCount = pool.Count;
        {
            expectTargetIsNull();
            var t = Tween.Delay(target, 0.0001f);
            Assert.IsFalse(t.isAlive);
            Assert.AreEqual(poolCount, pool.Count);
        }
        {
            expectTargetIsNull();
            var t = Tween.Custom(target, 0, 0, 1, delegate { });
            Assert.IsFalse(t.isAlive);
            Assert.AreEqual(poolCount, pool.Count);
        }
        {
            expectTargetIsNull();
            Assert.IsTrue(targetTr == null);
            var t = Tween.Position(targetTr, default, 1);
            Assert.IsFalse(t.isAlive);
            Assert.AreEqual(poolCount, pool.Count);
        }
        Assert.AreEqual(0, tweensCount);
    }

    [UnityTest]
    public IEnumerator TweenTargetDestroyedInSequenceWithCallbacks() {
        expectOnCompleteIgnored();
        var target = new GameObject("t1");
        var duration = getDt();
        yield return Sequence.Create()
            .Group(Tween.Custom(target, 0, 1, duration, delegate { }))
            .Chain(Tween.Custom(target, 0, 1, duration, (_target, _) => Object.DestroyImmediate(_target)).OnComplete(() => {}))
            .Chain(Tween.Delay(target, duration))
            .ToYieldInstruction();
        yield return null;
        Assert.AreEqual(0, tweensCount, "TweenCoroutineEnumerator should not check the target's destruction.");
        LogAssert.NoUnexpectedReceived();
    }

    [UnityTest]
    public IEnumerator TweenTargetDestroyedInSequence() {
        var target = new GameObject("t1");
        const float duration = 0.03f;
        yield return Sequence.Create()
            .Group(Tween.Custom(target, 0, 1, duration, delegate { }))
            .Chain(Tween.Custom(target, 0, 1, duration, (_target, _) => Object.DestroyImmediate(_target)))
            .Chain(Tween.Delay(target, duration))
            .ToYieldInstruction();
        LogAssert.NoUnexpectedReceived();
    }

    [Test]
    public void SequenceTargetDestroyedBeforeCallingStop() {
        var tweener = createTweenAndDestroyTargetImmediately(false);
        Sequence.Create(tweener).Stop();
    }

    [Test]
    public void SequenceTargetDestroyedBeforeCallingComplete() {
        var tweener = createTweenAndDestroyTargetImmediately().OnComplete(Assert.Fail);
        Sequence.Create(tweener).Complete();
    }

    [UnityTest]
    public IEnumerator TargetDestroyedBeforeCallingCompleteAll() {
        createTweenAndDestroyTargetImmediately().OnComplete(Assert.Fail);
        Tween.CompleteAll();
        Tween.SetPausedAll(true);
        Tween.SetPausedAll(false);
        Assert.AreEqual(0, getCurrentTweensCount());
        yield break;
    }

    static int getCurrentTweensCount() => tweensCount;

    [UnityTest]
    public IEnumerator TargetDestroyedBeforeCallingCompleteByTarget() {
        var tweener = createTweenAndDestroyTargetImmediately().OnComplete(Assert.Fail);
        Assert.AreEqual(Tween.CompleteAll(tweener.tween.managedData.target), 1);
        Tween.SetPausedAll(true);
        Tween.SetPausedAll(false);
        Assert.AreEqual(1, getCurrentTweensCount());
        yield return tweener;
        Assert.AreEqual(0, tweensCount);
    }

    [Test]
    public void TargetDestroyedBeforeCallingComplete() {
        createTweenAndDestroyTargetImmediately().OnComplete(Assert.Fail).Complete();
    }

    [UnityTest]
    public IEnumerator TargetDestroyedBeforeAddingOnComplete1() {
        yield return createTweenAndDestroyTargetImmediately()
            .OnComplete(delegate { })
            .ToYieldInstruction();
    }

    [UnityTest]
    public IEnumerator TargetDestroyedBeforeAddingOnComplete2() {
        yield return createTweenAndDestroyTargetImmediately()
            .OnComplete(this, delegate { })
            .ToYieldInstruction();
    }

    [UnityTest]
    public IEnumerator TargetDestroyedSetIsPaused() {
        var t = createTweenAndDestroyTargetImmediately().OnComplete(Assert.Fail);
        t.isPaused = true; // changing isPaused is ok
        t.isPaused = false;
        yield return t.ToYieldInstruction();
    }

    static Tween createTweenAndDestroyTargetImmediately(bool expectOnCompleteIgnoredWarning = true) {
        if (expectOnCompleteIgnoredWarning) {
            expectOnCompleteIgnored();
        }
        var tempTransform = new GameObject().transform;
        var tweener = Tween.LocalPosition(tempTransform, settingsVector3);
        Object.DestroyImmediate(tempTransform.gameObject);
        Assert.IsTrue(tweener.isAlive);
        return tweener;
    }

    [UnityTest]
    public IEnumerator OnCompleteIsNotCalledIfTargetDestroyed() {
        expectOnCompleteIgnored();
        var tempTransform = new GameObject().transform;
        var tweener = Tween.LocalPosition(tempTransform, settingsVector3).OnComplete(Assert.Fail);
        Object.DestroyImmediate(tempTransform.gameObject);
        while (tweener.isAlive) {
            yield return null;
        }
    }

    [UnityTest]
    public IEnumerator SettingCyclesOnDestroyedTween() {
        var t = createTweenAndDestroyTargetImmediately().OnComplete(Assert.Fail);
        t.SetRemainingCycles(2);
        yield return t;
    }

    [Test]
    public void IgnoreIfOnCompleteTargetDestroyed() {
        {
            var target = new GameObject(nameof(IgnoreIfOnCompleteTargetDestroyed));
            var t = Tween.Delay(1f).OnComplete(target, _ => Assert.Fail());
            Object.DestroyImmediate(target);
            expectOnCompleteIgnored();
            t.Complete();
        }
        {
            var target = new GameObject(nameof(IgnoreIfOnCompleteTargetDestroyed));
            var t = Tween.Delay(1f).OnComplete(target, _ => Assert.Fail(), false);
            Object.DestroyImmediate(target);
            t.Complete();
        }
        LogAssert.NoUnexpectedReceived();
    }

    [Test]
    public async Task TargetDestructionAsync() {
        {
            expectOnCompleteIgnored();
            var target = new GameObject(nameof(TargetDestructionAsync));
            await Tween.Custom(target, 0, 1, 1, delegate { Object.DestroyImmediate(target); })
                .OnComplete(Assert.Fail);
        }
        {
            var target = new GameObject(nameof(TargetDestructionAsync));
            await Tween.Custom(target, 0, 1, 1, delegate { Object.DestroyImmediate(target); })
                .OnComplete(Assert.Fail, false);
        }
        LogAssert.NoUnexpectedReceived();
    }

    [UnityTest]
    public IEnumerator TargetDestructionInCoroutine() {
        if (tweensCount > 0) {
            Tween.StopAll();
        }
        Assert.AreEqual(0, tweensCount);
        {
            PrimeTweenConfig.warnStructBoxingAllocationInCoroutine = true;
            expectCoroutineBoxingWarning();
            expectOnCompleteIgnored();
            var target = new GameObject(nameof(TargetDestructionInCoroutine));
            yield return Tween.Custom(target, 0, 1, 1, delegate {
                    Object.DestroyImmediate(target);
                }).OnComplete(Assert.Fail);
        }
        {
            var target = new GameObject(nameof(TargetDestructionInCoroutine));
            yield return Tween.Custom(target, 0, 1, 1, delegate {
                    Object.DestroyImmediate(target);
                }).OnComplete(Assert.Fail, false)
                .ToYieldInstruction();
        }
        LogAssert.NoUnexpectedReceived();
    }

    [UnityTest]
    public IEnumerator TargetDestructionWhilePaused() {
        var target = new GameObject(nameof(TargetDestructionWhilePaused));
        var tween = Tween.Delay(target, 0.05f);
        tween.isPaused = true;
        Object.DestroyImmediate(target);
        yield return null;
        Assert.IsFalse(tween.isAlive);
    }
}
#endif
