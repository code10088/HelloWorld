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
            TouchPhase tp = tc.phase.ReadValue();
            switch (tp)
            {
                case TouchPhase.None:
                    break;
                case TouchPhase.Began:
                    break;
                case TouchPhase.Moved:
                    //ÆÁÄ»×ø±êtc.startPosition.ReadValue()
                    //ÆÁÄ»×ø±êtc.position.ReadValue()
                    break;
                case TouchPhase.Ended:
                    break;
                case TouchPhase.Canceled:
                    break;
                case TouchPhase.Stationary:
                    break;
            }
        }
    }
}