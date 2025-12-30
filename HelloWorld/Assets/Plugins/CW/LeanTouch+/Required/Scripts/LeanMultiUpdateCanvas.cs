using UnityEngine;
using System.Collections.Generic;

namespace Lean.Touch
{
	/// <summary>This component allows you to perform events while fingers are on top of the current UI element.</summary>
	[HelpURL(LeanTouch.PlusHelpUrlPrefix + "LeanMultiUpdateCanvas")]
	[AddComponentMenu(LeanTouch.ComponentPathPrefix + "Multi Update Canvas")]
	public class LeanMultiUpdateCanvas : LeanMultiUpdate
	{
		/// <summary>If a finger is currently off the current UI element, ignore it?</summary>
		public bool IgnoreIfOff { set { ignoreIfOff = value; } get { return ignoreIfOff; } } [SerializeField] private bool ignoreIfOff = true;

		protected override List<LeanFinger> GetFingers()
		{
			var fingers = base.GetFingers();

			// Remove fingers that didn't begin on this UI element
			for (var i = fingers.Count - 1; i >= 0; i--)
			{
				var finger = fingers[i];

				if (LeanTouch.ElementOverlapped(gameObject, finger.ScreenPosition) == false)
				{
					fingers.RemoveAt(i);
				}
			}

			return fingers;
		}

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
			Use.IgnoreStartedOverGui = false;
		}
#endif
	}
}

#if UNITY_EDITOR
namespace Lean.Touch.Editor
{
	using UnityEditor;
	using TARGET = LeanMultiUpdateCanvas;

	[CanEditMultipleObjects]
	[CustomEditor(typeof(TARGET))]
	public class LeanMultiUpdateCanvas_Editor : LeanMultiUpdate_Editor
	{
		protected override void DrawIgnore()
		{
			base.DrawIgnore();

			Draw("ignoreIfOff", "If a finger is currently off the current UI element, ignore it?");
		}
	}
}
#endif