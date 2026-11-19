#if UNITY_EDITOR && TEST_FRAMEWORK_INSTALLED
using PrimeTween;
using UnityEngine;

public class DestructionOrderTest : MonoBehaviour {
    void OnDestroy() {
        print($"PrimeTweenManager.HasInstance: {PrimeTweenManager.HasInstance}, PrimeTweenManager._instance != null:{PrimeTweenManager._instance != null}");
        Tween.StopAll(transform);
        Tween.CompleteAll(transform);
        Tween.Custom(0, 1, 1, delegate {});
        var go = new GameObject();
        {
            Tween.Alpha(go.AddComponent<SpriteRenderer>(), 0, 1);
            Tween.Delay(1);
            Tween.SetPausedAll(true);
            Tween.ShakeLocalPosition(go.transform, Vector3.one, 1);
            Tween.ShakeCustom(go, Vector3.zero, new ShakeSettings(Vector3.one, 1), delegate {});
            Sequence.Create();
            // Tween.GlobalTimeScale(0.5f, 0.1f);
            Tween.GetTweensCount(this);
            Tween.GetTweensCount();
            Sequence.Create(Tween.Delay(0.1f));
            // Tween.GlobalTimeScale(2f, 1f);
            Tween.TweenTimeScale(Tween.Delay(0.1f), 2f, 1f);
            Tween.TweenTimeScale(Sequence.Create(), 2f, 1f);
            Tween.StopAll();
            Tween.CompleteAll();

            PrimeTweenConfig.SetTweensCapacity(20);
            PrimeTweenConfig.defaultEase = Ease.InCirc;
            PrimeTweenConfig.warnZeroDuration = true;
            PrimeTweenConfig.warnTweenOnDisabledTarget = true;
            PrimeTweenConfig.validateCustomCurves = true;
            PrimeTweenConfig.warnBenchmarkWithAsserts = true;
            PrimeTweenConfig.warnEndValueEqualsCurrent = true;
            PrimeTweenConfig.warnStructBoxingAllocationInCoroutine = true;
        }
        DestroyImmediate(go);
    }
}
#endif