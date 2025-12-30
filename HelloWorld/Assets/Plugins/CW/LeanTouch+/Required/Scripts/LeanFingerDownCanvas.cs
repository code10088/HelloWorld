using UnityEngine;

namespace Lean.Touch
{
	/// <summary>This component works like <b>LeanFingerDown</b>, but only for the UI element this component is attached to.
	/// NOTE: This requires you to enable the RaycastTarget setting on your UI graphic.</summary>
	[RequireComponent(typeof(RectTransform))]
	[HelpURL(LeanTouch.PlusHelpUrlPrefix + "LeanFingerDownCanvas")]
	[AddComponentMenu(LeanTouch.ComponentPathPrefix + "Finger Down Canvas")]
	public class LeanFingerDownCanvas : LeanFingerDown
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
	using TARGET = LeanFingerDownCanvas;

	[CanEditMultipleObjects]
	[CustomEditor(typeof(TARGET))]
	public class LeanFingerDownCanvas_Editor : LeanFingerDown_Editor
	{
		protected override void OnInspector()
		{
			TARGET tgt; TARGET[] tgts; GetTargets(out tgt, out tgts);

			base.OnInspector();
		}
	}
}
#endif