using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem;

namespace HotAssembly
{
    public class InputControl
    {
        public static void Update()
        {
            Touchscreen ts = Touchscreen.current;
            if (ts == null) return;
            TouchControl tc = ts.touches[0];
            UnityEngine.InputSystem.TouchPhase tp = tc.phase.ReadValue();
            switch (tp)
            {
                case UnityEngine.InputSystem.TouchPhase.None:
                    break;
                case UnityEngine.InputSystem.TouchPhase.Began:
                    break;
                case UnityEngine.InputSystem.TouchPhase.Moved:
                    //ÆÁÄ»×ø±êtc.startPosition.ReadValue()
                    //ÆÁÄ»×ø±êtc.position.ReadValue()
                    break;
                case UnityEngine.InputSystem.TouchPhase.Ended:
                    break;
                case UnityEngine.InputSystem.TouchPhase.Canceled:
                    break;
                case UnityEngine.InputSystem.TouchPhase.Stationary:
                    break;
            }
        }
    }
}