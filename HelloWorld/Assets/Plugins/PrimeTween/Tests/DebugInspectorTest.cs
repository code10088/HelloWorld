using JetBrains.Annotations;
using PrimeTween;
using UnityEngine;

public class DebugInspectorTest : MonoBehaviour {
    // [SerializeField] TweenData managedData;
    // [SerializeField] TweenData reusableTween2;
    [UsedImplicitly] string debug;
    // [SerializeField] int serializedInt;
    // int notSerializedInt;
    //
    // [ContextMenu(nameof(debugSerializedObject))]
    // void debugSerializedObject() {
    //     var so = new SerializedObject(this);
    //     var prop = typeof(SerializedObject).GetProperty("inspectorMode", BindingFlags.Instance | BindingFlags.NonPublic);
    //     Assert.IsNotNull(prop);
    //     prop.SetValue(so, 1);
    //     var i = so.GetIterator();
    //     do {
    //         print($"{i.propertyPath}");
    //     } while (i.Next(true));
    // }
    
    void Update() {
        if (Time.frameCount == 2) {
            test();
        }
        updateDebug();
    }

    [ContextMenu(nameof(updateDebug))]
    void updateDebug() {
        throw new System.Exception();
        /*var tweens = PrimeTweenManager.Instance.tweens;
        if (tweens.Count == 0) {
            debug = "no tweens";
            return;
        }
        debug = $"is destroyed {tweens[0].IsUnityTargetDestroyed()}";*/
    }

    void test() {
        Sequence.Create()
            .Chain(Tween.Delay(0.5f))
            .Chain(Tween.Delay(0.5f))
            ;
    }
}