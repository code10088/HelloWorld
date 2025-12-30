using UnityEngine;

namespace Lean.Touch
{
	/// <summary>This component works like <b>LeanFingerHeld</b>, but only for the UI element this component is attached to.</summary>
	[HelpURL(LeanTouch.HelpUrlPrefix + "LeanFingerHeldCanvas")]
	[AddComponentMenu(LeanTouch.ComponentPathPrefix + "Finger Held Canvas")]
	public class LeanFingerHeldCanvas : LeanFingerHeld
	{
		protected override void HandleFingerDown(LeanFinger finger)
		{
			if (LeanTouch.ElementOverlapped(gameObject, finger.ScreenPosition) == true)
			{
				base.HandleFingerDown(finger);
			}
		}

#if UNITY_EDITOR
		protected override void Reset()
		{
			IgnoreStartedOverGui = false;
		}
#endif
	}
}

#if UNITY_EDITOR
namespace Lean.Touch.Editor
{
	using UnityEditor;
	using TARGET = LeanFingerHeldCanvas;

	[CanEditMultipleObjects]
	[CustomEditor(typeof(TARGET))]
	public class LeanFingerHeldCanvas_Editor : LeanFingerHeld_Editor
	{
		protected override void OnInspector()
		{
			base.OnInspector();
		}
	}
}
#endif