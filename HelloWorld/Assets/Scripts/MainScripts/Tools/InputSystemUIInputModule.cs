using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using UnityEngine.InputSystem.LowLevel;

public class InputSystemUIInputModule : UnityEngine.InputSystem.UI.InputSystemUIInputModule
{
    public static void EnableTouchSimulation()
    {
        TouchSimulation.Enable();
    }
    public static void Click(Vector2 pos)
    {
        StateEvent.From(Touchscreen.current, out InputEventPtr eventPtr);
        Touchscreen.current.position.WriteValueIntoEvent(pos, eventPtr);
        InputSystem.QueueEvent(eventPtr);
    }
}