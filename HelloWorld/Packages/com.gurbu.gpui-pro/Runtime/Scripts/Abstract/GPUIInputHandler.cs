// GPU Instancer Pro
// Copyright (c) GurBu Technologies

using System;
using System.Collections.Generic;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM && GPUI_INPUTSYSTEM
using UnityEngine.InputSystem;
#endif

namespace GPUInstancerPro
{
    public abstract class GPUIInputHandler : MonoBehaviour
    {
#if ENABLE_INPUT_SYSTEM && GPUI_INPUTSYSTEM
        private Mouse _mouse;
        private Keyboard _keyboard;
#endif
        public Vector3 MousePosition
        {
            get
            {
#if ENABLE_LEGACY_INPUT_MANAGER
                return Input.mousePosition;
#elif ENABLE_INPUT_SYSTEM && GPUI_INPUTSYSTEM
                return _mouse == null ? Vector3.zero : _mouse.position.value;
#else
                return Vector3.zero;
#endif
            }
        }

        protected virtual void Start()
        {
#if ENABLE_INPUT_SYSTEM && GPUI_INPUTSYSTEM
            _mouse = Mouse.current;
            _keyboard = Keyboard.current;
#endif
        }

        public bool GetMouseButton(int button)
        {
#if ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetMouseButton(button);
#elif ENABLE_INPUT_SYSTEM && GPUI_INPUTSYSTEM
            if (_mouse == null)
                return false;
            if (button == 0)
                return _mouse.leftButton.isPressed;
            else if (button == 1)
                return _mouse.rightButton.isPressed;
            return false;
#else
            return false;
#endif
        }

        public bool GetMouseButtonUp(int button)
        {
#if ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetMouseButtonUp(button);
#elif ENABLE_INPUT_SYSTEM && GPUI_INPUTSYSTEM
            if (_mouse == null)
                return false;
            if (button == 0)
                return _mouse.leftButton.wasReleasedThisFrame;
            else if (button == 1)
                return _mouse.rightButton.wasReleasedThisFrame;
            return false;
#else
            return false;
#endif
        }

        public float GetAxis(string axisName)
        {
            if (string.IsNullOrEmpty(axisName))
                return 0;

#if ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetAxis(axisName);
#elif ENABLE_INPUT_SYSTEM && GPUI_INPUTSYSTEM
            if (_mouse != null)
            {
                if (axisName == "Mouse X")
                    return _mouse.delta.ReadValue().x / 10f;
                else if (axisName == "Mouse Y")
                    return _mouse.delta.ReadValue().y / 10f;
            }

            if (_keyboard != null)
            {
                if (axisName == "Horizontal")
                    return _keyboard.aKey.isPressed ? -1f : _keyboard.dKey.isPressed ? 1f : 0f;
                else if (axisName == "Vertical")
                    return _keyboard.sKey.isPressed ? -1f : _keyboard.wKey.isPressed ? 1f : 0f;
                else if (axisName == "Jump")
                    return _keyboard.spaceKey.isPressed ? 1f : 0f;
            }

            return 0;
#else
            return 0;
#endif
        }

        public bool GetKey(KeyCode key)
        {
#if ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKey(key);
#elif ENABLE_INPUT_SYSTEM && GPUI_INPUTSYSTEM
            if (_keyboard == null)
                return false;
            return ((UnityEngine.InputSystem.Controls.KeyControl)_keyboard[GetKeyString(key)]).isPressed;
#else
            return false;
#endif
        }

        public bool GetKeyUp(KeyCode key)
        {
#if ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKeyUp(key);
#elif ENABLE_INPUT_SYSTEM && GPUI_INPUTSYSTEM
            if (_keyboard == null)
                return false;
            return ((UnityEngine.InputSystem.Controls.KeyControl)_keyboard[GetKeyString(key)]).wasReleasedThisFrame;
#else
            return false;
#endif
        }

        public bool GetKeyDown(KeyCode key)
        {
#if ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKeyDown(key);
#elif ENABLE_INPUT_SYSTEM && GPUI_INPUTSYSTEM
            if (_keyboard == null)
                return false;
            return ((UnityEngine.InputSystem.Controls.KeyControl)_keyboard[GetKeyString(key)]).wasPressedThisFrame;
#else
            return false;
#endif
        }

#if ENABLE_INPUT_SYSTEM && GPUI_INPUTSYSTEM
        private string GetKeyString(KeyCode key)
        {
            switch (key) // Commonly used ones are hardcoded to reduce memory allocations
            {
                case KeyCode.W:
                    return "W";
                case KeyCode.S:
                    return "S";
                case KeyCode.A:
                    return "A";
                case KeyCode.D:
                    return "D";
                case KeyCode.Q:
                    return "Q";
                case KeyCode.E:
                    return "E";
                case KeyCode.LeftShift:
                    return "LeftShift";
                case KeyCode.Space:
                    return "Space";
                case KeyCode.Alpha0:
                    return "0";
                case KeyCode.Alpha1:
                    return "1";
                case KeyCode.Alpha2:
                    return "2";
                case KeyCode.Alpha3:
                    return "3";
            }
            string keyString = key.ToString();
            if (keyString.StartsWith("Alpha"))
                keyString = keyString.Substring(5);
            return keyString;
        }
#endif
    }
}
