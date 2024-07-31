using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem;
using UnityEngine;
using TouchPhase = UnityEngine.InputSystem.TouchPhase;

namespace HotAssembly
{
    public class InputControl
    {
        protected TouchControl tc;
        public Vector2 MousePos => tc.position.value;
        public Vector2 MouseStartPos => tc.startPosition.value;

        public void Update()
        {
            Touchscreen ts = Touchscreen.current;
            if (ts == null) return;
            tc = ts.touches[0];
            switch (tc.phase.value)
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