#if UNITY_EDITOR && TEST_FRAMEWORK_INSTALLED
// ReSharper disable NotAccessedField.Local
// ReSharper disable UnusedMember.Local
// ReSharper disable PartialTypeWithSinglePart
using System;
using PrimeTween;
using UnityEngine;
using Assert = NUnit.Framework.Assert;

[ExecuteInEditMode]
public partial class EditModeTest : MonoBehaviour {
    [SerializeField] TweenSettings _settings = CreateSettings();
    static TweenSettings CreateSettings() {
        TweenSettings res = default;
        if (!PrimeTweenManager.HasInstance) {
            ExpectException(() => res = new TweenSettings(1, AnimationCurve.Linear(0, 0, 1, 1)));
        }
        return res;
    }

    Tween tween = TestWithPossibleException();

    static Tween TestWithPossibleException() {
        if (PrimeTweenManager.HasInstance) {
            Tween.StopAll();
            return Test();
        }
        ExpectException(() => Tween.StopAll());
        ExpectException(() => Sequence.Create());
        ExpectException(() => PrimeTweenConfig.SetTweensCapacity(PrimeTweenManager.Instance.currentPoolCapacity + 1));
        ExpectException(() => PrimeTweenConfig.warnZeroDuration = !PrimeTweenConfig.warnZeroDuration);
        // ExpectException(() => Tween.GlobalTimeScale(1f, 0.1f));
        ExpectException(() => Tween.GetTweensCount());
        ExpectException(() => {
            Sequence.Create()
                .ChainCallback(() => {})
                .InsertCallback(0f, delegate {})
                .Group(StartTween())
                .Chain(StartTween())
                .Insert(0f, Sequence.Create())
                .Insert(0, StartTween());
        });
        ExpectException(() => Tween.Delay(new object(), 1f, () => {}));
        ExpectException(() => Tween.Delay(new object(), 1f, _ => {}));
        ExpectException(() => Tween.Delay(1f, () => { }));
        ExpectException(() => Tween.Custom(0, 1, 1, delegate {}));
        return default;
    }

    static void ExpectException(Action action) {
        try {
            action();
        } catch (Exception e) {
            string message = e.Message;
            Assert.IsTrue(message.Contains("is not allowed to be called from a MonoBehaviour constructor"), message);
        }
    }

    static Tween Test() {
        PrimeTweenConfig.SetTweensCapacity(PrimeTweenManager.Instance.currentPoolCapacity + 1);
        Assert.DoesNotThrow(() => PrimeTweenConfig.warnZeroDuration = false);

        // PrimeTweenConfig.warnEndValueEqualsCurrent = false;
        // Tween.GlobalTimeScale(1f, 0.1f);
        // PrimeTweenConfig.warnEndValueEqualsCurrent = true;

        Tween.GetTweensCount();
        Sequence.Create()
            .ChainCallback(() => {})
            .InsertCallback(0f, delegate {})
            .Group(StartTween())
            .Chain(StartTween())
            .Insert(0f, Sequence.Create())
            .Insert(0, StartTween());
        Tween.Delay(new object(), 1f, () => {});
        Tween.Delay(new object(), 1f, _ => {});
        Tween.Delay(1f, () => { });
        return Tween.Custom(0, 1, 1, delegate {});
    }

    static Tween StartTween() => Tween.Custom(0f, 1f, 1f, delegate { });
    
    void Awake() => TestWithPossibleException();
    void OnValidate() => TestWithPossibleException();
    void Reset() => TestWithPossibleException();
    void OnEnable() => TestWithPossibleException();
    void OnDisable() => TestWithPossibleException();
    void OnDestroy() => Test();
}

/*[UnityEditor.InitializeOnLoad]
public partial class EditModeTest {
    static EditModeTest() => TestWithPossibleException();
    EditModeTest() => TestWithPossibleException();

    [RuntimeInitializeOnLoadMethod]
    static void runtimeInitOnLoad() => Test();
}*/
#endif