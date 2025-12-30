using UnityEngine;

namespace Lean.Touch
{
	/// <summary>This component allows you to detect when the first finger begins touching the current UI element.
	/// NOTE: This requires you to enable the RaycastTarget setting on your UI graphic.</summary>
	[HelpURL(LeanTouch.PlusHelpUrlPrefix + "LeanFirstDownCanvas")]
	[AddComponentMenu(LeanTouch.ComponentPathPrefix + "First Down Canvas")]
	public class LeanFirstDownCanvas : LeanFirstDown
	{
		protected override bool UseFinger(LeanFinger finger)
		{
			return UseFinger(finger) == true && LeanTouch.ElementOverlapped(gameObject, finger.ScreenPosition) == true;
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
	using TARGET = LeanFirstDownCanvas;

	[CanEditMultipleObjects]
	[CustomEditor(typeof(TARGET))]
	public class LeanFirstDownCanvas_Editor : LeanFirstDown_Editor
	{
		protected override void OnInspector()
		{
			TARGET tgt; TARGET[] tgts; GetTargets(out tgt, out tgts);

			base.OnInspector();
		}
	}
}
#endif