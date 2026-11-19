using UnityEngine;
using Lean.Common;

namespace Lean.Touch
{
	/// <summary>This component allows you to detect when the last finger stops touching the current UI element.
	/// NOTE: This requires you to enable the RaycastTarget setting on your UI graphic.
	[HelpURL(LeanTouch.PlusHelpUrlPrefix + "LeanLastUpCanvas")]
	[AddComponentMenu(LeanTouch.ComponentPathPrefix + "Last Up Canvas")]
	public class LeanLastUpCanvas : LeanLastUp
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
	using TARGET = LeanLastUpCanvas;

	[CanEditMultipleObjects]
	[CustomEditor(typeof(TARGET))]
	public class LeanLastUpCanvas_Editor : LeanLastUp_Editor
	{
		protected override void OnInspector()
		{
			TARGET tgt; TARGET[] tgts; GetTargets(out tgt, out tgts);

			base.OnInspector();
		}
	}
}
#endif