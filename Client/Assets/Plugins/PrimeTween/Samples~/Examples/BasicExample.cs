using PrimeTween;
using UnityEngine;
#if INPUT_SYSTEM_INSTALLED && ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using TouchPhase = UnityEngine.InputSystem.TouchPhase;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;
#endif

namespace PrimeTweenDemo {
    public class BasicExample : MonoBehaviour {
        [Tooltip("Tweak the animation that will be played in Awake() right from the Inspector.\n\n" +
                 "Click with mouse to play the animation created from code.")]
        [SerializeField]
        TweenSettings<Vector3> tweenSettings = new TweenSettings<Vector3>(Vector3.zero, new Vector3(0, 5), 1);

        void Awake() {
            Tween.LocalPosition(transform, tweenSettings);
        }

        void Update() {
            if (GetInputDown()) {
                Tween.LocalPositionX(transform, 0, 3f, 1f, Ease.OutBounce, 2, CycleMode.Yoyo);
            }
        }

        public static bool GetInputDown() {
            #if INPUT_SYSTEM_INSTALLED && ENABLE_INPUT_SYSTEM
            if (Mouse.current != null) {
                return Mouse.current.leftButton.wasPressedThisFrame;
            }
            return Touch.activeTouches.Count > 0 && Touch.activeTouches[0].phase == TouchPhase.Began;
            #elif ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetMouseButtonDown(0);
            #else
            return false;
            #endif
        }
    }
}
