using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem;

namespace HotAssembly
{
    public class InputControl
    {
        public void Update()
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
                    OnBegan(tc);
                    break;
                case TouchPhase.Moved:
                    OnMoved(tc);
                    break;
                case TouchPhase.Ended:
                    OnEnded(tc);
                    break;
                case TouchPhase.Canceled:
                    OnCanceled(tc);
                    break;
                case TouchPhase.Stationary:
                    OnStationary(tc);
                    break;
            }
        }
        public virtual void OnBegan(TouchControl tc)
        {

        }
        public virtual void OnMoved(TouchControl tc)
        {
            //ÆÁÄ»×ø±êtc.startPosition.ReadValue()
            //ÆÁÄ»×ø±êtc.position.ReadValue()
        }
        public virtual void OnEnded(TouchControl tc)
        {

        }
        public virtual void OnCanceled(TouchControl tc)
        {

        }
        public virtual void OnStationary(TouchControl tc)
        {

        }
    }
}