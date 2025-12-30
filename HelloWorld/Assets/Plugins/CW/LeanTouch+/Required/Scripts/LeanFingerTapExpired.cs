using UnityEngine;

namespace Lean.Touch
{
	/// <summary>This component works like <b>OnFingerTap</b>, but it will only call events when a finger you've tapped has 'expired'. This allows you to discern between single/double/etc taps.</summary>
	[HelpURL(LeanTouch.PlusHelpUrlPrefix + "LeanFingerTapExpired")]
	[AddComponentMenu(LeanTouch.ComponentPathPrefix + "Finger Tap Expired")]
	public class LeanFingerTapExpired : LeanFingerTap
	{
		protected override void OnEnable()
		{
			LeanTouch.OnFingerExpired += HandleFingerExpired;
		}

		protected override void OnDisable()
		{
			LeanTouch.OnFingerExpired -= HandleFingerExpired;
		}

		private void HandleFingerExpired(LeanFinger finger)
		{
			HandleFingerTap(finger);
		}
	}
}

#if UNITY_EDITOR
namespace Lean.Touch.Editor
{
	using UnityEditor;
	using TARGET = LeanFingerTapExpired;

	[CanEditMultipleObjects]
	[CustomEditor(typeof(TARGET))]
	public class LeanFingerTapExpired_Editor : LeanFingerTap_Editor
	{
		protected override void OnInspector()
		{
			base.OnInspector();
		}
	}
}
#endif