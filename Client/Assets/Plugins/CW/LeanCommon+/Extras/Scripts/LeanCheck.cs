using UnityEngine;
using UnityEngine.Events;
using CW.Common;

namespace Lean.Common
{
	/// <summary>The base class for all components that check the game state to see if a specific condition is met, so then a custom action can be performed.</summary>
	public abstract class LeanCheck : MonoBehaviour
	{
		/// <summary>Have the conditions of this component been met?</summary>
		public bool Matched { get { return matched; } set { SetMatched(value); } } [SerializeField] private bool matched;

		/// <summary>If the </summary>
		public UnityEvent OnMatched { get { if (onMatched == null) onMatched = new UnityEvent(); return onMatched; } } [SerializeField] private UnityEvent onMatched;

		/// <summary>This event will send any previously set values after the specified delay.</summary>
		public UnityEvent OnUnmatched { get { if (onUnmatched == null) onUnmatched = new UnityEvent(); return onUnmatched; } } [SerializeField] private UnityEvent onUnmatched;

		public void SetMatched(bool newMatched)
		{
			if (matched != newMatched)
			{
				matched = newMatched;

				if (matched == true)
				{
					if (onMatched != null)
					{
						onMatched.Invoke();
					}
				}
				else
				{
					if (onUnmatched != null)
					{
						onUnmatched.Invoke();
					}
				}
			}
		}

		[ContextMenu("Update Check")]
		public abstract void UpdateCheck();

		protected virtual void Update()
		{
			UpdateCheck();
		}
	}
}

#if UNITY_EDITOR
namespace Lean.Common.Editor
{
	using UnityEditor;
	using TARGET = LeanCheck;

	[CanEditMultipleObjects]
	[CustomEditor(typeof(TARGET))]
	public class LeanCheck_Editor : CwEditor
	{
		protected override void OnInspector()
		{
			TARGET tgt; TARGET[] tgts; GetTargets(out tgt, out tgts);

			Draw("matched", "Have the conditions of this component been met?");

			Separator();

			Draw("onMatched");
			Draw("onUnmatched");
		}
	}
}
#endif