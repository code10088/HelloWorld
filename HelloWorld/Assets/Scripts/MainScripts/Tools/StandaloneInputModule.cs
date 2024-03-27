using UnityEngine;
using UnityEngine.EventSystems;

public class StandaloneInputModule : UnityEngine.EventSystems.StandaloneInputModule
{
    public static void Click(Vector2 pos)
    {
        var sim = EventSystem.current.currentInputModule as StandaloneInputModule;

        var touch = new Touch();
        touch.position = pos;

        bool released;
        bool pressed;
        var pointer = sim.GetTouchPointerEventData(touch, out pressed, out released);
        sim.ProcessTouchPress(pointer, pressed, released);
    }
}