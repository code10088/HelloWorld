#region copyright
// --------------------------------------------------------------
//  Copyright (C) Dmitriy Yukhanov - focus [http://codestage.net]
// --------------------------------------------------------------
#endregion

#if UNITY_2019_1_OR_NEWER && ENABLE_INPUT_SYSTEM && AFPS_INPUT_SYSTEM
#define USING_INPUT_SYSTEM
#elif UNITY_2019_1_OR_NEWER && ENABLE_INPUT_SYSTEM
#define USING_INPUT_SYSTEM_NO_INPUT_SYSTEM_PACKAGE
#endif

namespace CodeStage.AdvancedFPSCounter
{
	using System;
	using UnityEngine;
#if USING_INPUT_SYSTEM
	using UnityEngine.InputSystem;
#endif
	
	public static class AFPSInputProxy
	{
		
#if USING_INPUT_SYSTEM
		private static Key cachedHotKey;
		private static KeyCode lastHotKeyLegacy;
#endif
		
		public static bool GetHotKeyDown(KeyCode key)
		{
			if (key == KeyCode.None)
				return false;
			
			var result = false;
#if USING_INPUT_SYSTEM
			if (lastHotKeyLegacy != key)
			{
				cachedHotKey = ConvertLegacyKeyCode(key);
				lastHotKeyLegacy = key;
			}
			
			result = Keyboard.current[cachedHotKey].wasPressedThisFrame;
#elif USING_INPUT_SYSTEM_NO_INPUT_SYSTEM_PACKAGE
			Debug.LogError("Looks like you have Input System enabled but no Input System package installed!");
#elif ENABLE_LEGACY_INPUT_MANAGER
			result = Input.GetKeyDown(key);
#else
			result = Input.GetKeyDown(key);
#endif
			return result;
		}
		
		public static bool GetControlKey()
		{
#if USING_INPUT_SYSTEM
			return Keyboard.current.leftCtrlKey.isPressed || Keyboard.current.rightCtrlKey.isPressed || Keyboard.current.leftCommandKey.isPressed || Keyboard.current.rightCommandKey.isPressed;
#else
			return Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl) || Input.GetKey(KeyCode.LeftCommand) || Input.GetKey(KeyCode.RightCommand);
#endif
		}
		
		public static bool GetAltKey()
		{
#if USING_INPUT_SYSTEM
			return Keyboard.current.leftAltKey.isPressed || Keyboard.current.rightAltKey.isPressed;
#else
			return Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt);
#endif
		}
		
		public static bool GetShiftKey()
		{
#if USING_INPUT_SYSTEM
			return Keyboard.current.leftShiftKey.isPressed || Keyboard.current.rightShiftKey.isPressed;
#else
			return Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
#endif
		}
		
#if USING_INPUT_SYSTEM
		private static Key ConvertLegacyKeyCode(KeyCode keyCode)
		{
			if (!Enum.TryParse(keyCode.ToString(), true, out Key result))
			{
				Debug.LogError("Couldn't convert legacy input KeyCode " + keyCode + " to the new Input System format!\nPlease report to https://codestage.net/contacts/");
			}
			return result;
		}
#endif
	}
}