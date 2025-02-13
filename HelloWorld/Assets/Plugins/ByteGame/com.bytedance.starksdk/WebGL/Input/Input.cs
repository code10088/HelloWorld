#if UNITY_WEBGL && STARK_UNITY_INPUT_OVERRIDE

using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Interface into the Input system. (Overridden by TTSDK)
/// </summary>
/// <remarks>
/// 覆盖 Input，用于将 Input.GetTouch() 等方法重载至 TTSDK 中的实现。
/// 因为编译器解析引用时，全局命名空间中的类优先级更高，Input.GetTouch() 等方法将优先选择当前类而非 UnityEngine.Input.
/// * 通过 STARK_UNITY_INPUT_OVERRIDE 符号控制开关，默认关闭。关闭时与 UnityEngine.Input 无差异。
/// * 不会重载以完全限定名形式（UnityEngine.Input）书写的调用，仅重载 Input.XXX。
/// * 不会重载动态链接库（DLL）中对 UnityEngine.Input 的直接调用。
/// </remarks>
public static class Input
{
    static Input()
    {
        Debug.Log("Global Input overridden by TTSDK.");
    }

    #region Touch Override

    private static BaseInputModule CurrentInputModule => EventSystem.current.currentInputModule;

    private static bool Enabled => true;

    #endregion

    #region Touch

    public static Touch GetTouch(int index)
    {
        if (!Enabled) return UnityEngine.Input.GetTouch(index);

        return CurrentInputModule.input.GetTouch(index);
    }

    public static int touchCount
    {
        get { return Enabled ? CurrentInputModule.input.touchCount : UnityEngine.Input.touchCount; }
    }

    public static bool touchPressureSupported => UnityEngine.Input.touchPressureSupported;

    public static bool stylusTouchSupported => UnityEngine.Input.stylusTouchSupported;

    public static bool touchSupported
    {
        get { return Enabled ? CurrentInputModule.input.touchSupported : UnityEngine.Input.touchSupported; }
    }

    public static bool multiTouchEnabled => UnityEngine.Input.multiTouchEnabled;

    public static Touch[] touches
    {
        get
        {
            if (!Enabled) return UnityEngine.Input.touches;

            var touchCount = CurrentInputModule.input.touchCount;
            var touches = new Touch[touchCount];
            for (var index = 0; index < touchCount; ++index) touches[index] = CurrentInputModule.input.GetTouch(index);
            return touches;
        }
    }

    #endregion

    #region Mouse

    public static Vector3 mousePosition
    {
        get
        {
            if (!Enabled) return UnityEngine.Input.mousePosition;

            return new Vector3(CurrentInputModule.input.mousePosition.x, CurrentInputModule.input.mousePosition.y,
                0.0f);
        }
    }

    public static Vector2 mouseScrollDelta
    {
        get { return Enabled ? CurrentInputModule.input.mouseScrollDelta : UnityEngine.Input.mouseScrollDelta; }
    }

    public static bool mousePresent => Enabled ? CurrentInputModule.input.mousePresent : UnityEngine.Input.mousePresent;

    public static bool GetMouseButton(int button)
    {
        return Enabled ? CurrentInputModule.input.GetMouseButton(button) : UnityEngine.Input.GetMouseButton(button);
    }

    public static bool GetMouseButtonDown(int button)
    {
        return Enabled ? CurrentInputModule.input.GetMouseButtonDown(button) : UnityEngine.Input.GetMouseButton(button);
    }

    public static bool GetMouseButtonUp(int button)
    {
        return Enabled ? CurrentInputModule.input.GetMouseButtonUp(button) : UnityEngine.Input.GetMouseButtonUp(button);
    }

    #endregion

    public static float GetAxis(string axisName)
    {
        return UnityEngine.Input.GetAxis(axisName);
    }

    public static float GetAxisRaw(string axisName)
    {
        return UnityEngine.Input.GetAxisRaw(axisName);
    }

    public static bool GetButton(string buttonName)
    {
        return UnityEngine.Input.GetButton(buttonName);
    }

    public static bool GetButtonDown(string buttonName)
    {
        return UnityEngine.Input.GetButtonDown(buttonName);
    }

    public static bool GetButtonUp(string buttonName)
    {
        return UnityEngine.Input.GetButtonUp(buttonName);
    }

    public static void ResetInputAxes()
    {
        UnityEngine.Input.ResetInputAxes();
    }

#if UNITY_STANDLONE_LINUX
    public static bool IsJoystickPreconfigured(string joystickName)
    {
      return UnityEngine.Input.IsJoystickPreconfigured(joystickName);
    }
#endif

    public static string[] GetJoystickNames()
    {
        return UnityEngine.Input.GetJoystickNames();
    }

    public static AccelerationEvent GetAccelerationEvent(int index)
    {
        return UnityEngine.Input.GetAccelerationEvent(index);
    }

    public static bool GetKey(KeyCode key) => UnityEngine.Input.GetKey(key);

    public static bool GetKey(string name) => UnityEngine.Input.GetKey(name);

    public static bool GetKeyUp(KeyCode key) => UnityEngine.Input.GetKeyUp(key);

    public static bool GetKeyUp(string name) => UnityEngine.Input.GetKeyUp(name);

    public static bool GetKeyDown(KeyCode key) => UnityEngine.Input.GetKeyDown(key);

    public static bool GetKeyDown(string name) => UnityEngine.Input.GetKeyDown(name);

    public static bool simulateMouseWithTouches
    {
        get => UnityEngine.Input.simulateMouseWithTouches;
        set => UnityEngine.Input.simulateMouseWithTouches = value;
    }

    public static bool anyKey => UnityEngine.Input.anyKey;

    public static bool anyKeyDown => UnityEngine.Input.anyKeyDown;

    public static string inputString => UnityEngine.Input.inputString;

    public static IMECompositionMode imeCompositionMode
    {
        get => UnityEngine.Input.imeCompositionMode;
        set => UnityEngine.Input.imeCompositionMode = value;
    }

    public static string compositionString => UnityEngine.Input.compositionString;

    public static bool imeIsSelected => UnityEngine.Input.imeIsSelected;

    public static Vector2 compositionCursorPos
    {
        get => UnityEngine.Input.compositionCursorPos;
        set => UnityEngine.Input.compositionCursorPos = value;
    }

    public static bool eatKeyPressOnTextFieldFocus
    {
        get => UnityEngine.Input.eatKeyPressOnTextFieldFocus;
        set => UnityEngine.Input.eatKeyPressOnTextFieldFocus = value;
    }

    public static bool isGyroAvailable => UnityEngine.Input.isGyroAvailable;

    public static DeviceOrientation deviceOrientation => UnityEngine.Input.deviceOrientation;

    public static Vector3 acceleration => UnityEngine.Input.acceleration;

    public static bool compensateSensors
    {
        get => UnityEngine.Input.compensateSensors;
        set => UnityEngine.Input.compensateSensors = value;
    }

    public static int accelerationEventCount => UnityEngine.Input.accelerationEventCount;

    public static bool backButtonLeavesApp
    {
        get => UnityEngine.Input.backButtonLeavesApp;
        set => UnityEngine.Input.backButtonLeavesApp = value;
    }

    public static LocationService location => UnityEngine.Input.location;

    public static Compass compass => UnityEngine.Input.compass;

    public static Gyroscope gyro => UnityEngine.Input.gyro;

    public static AccelerationEvent[] accelerationEvents => UnityEngine.Input.accelerationEvents;
}

#endif