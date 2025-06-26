// GPU Instancer Pro
// Copyright (c) GurBu Technologies

using UnityEngine;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM && GPUI_INPUTSYSTEM
using UnityEngine.InputSystem.UI;
#endif

namespace GPUInstancerPro
{
    [RequireComponent(typeof(EventSystem))]
    public class GPUIInputModuleHandler : MonoBehaviour
    {
        void Start()
        {
#if ENABLE_INPUT_SYSTEM && GPUI_INPUTSYSTEM
            if (TryGetComponent(out StandaloneInputModule im))
                Destroy(im);
            gameObject.AddOrGetComponent<InputSystemUIInputModule>();
#else
            gameObject.AddOrGetComponent<StandaloneInputModule>();
#endif
        }
    }
}
