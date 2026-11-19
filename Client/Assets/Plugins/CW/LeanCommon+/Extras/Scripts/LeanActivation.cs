using UnityEngine;
using UnityEngine.Events;
using CW.Common;

namespace Lean.Common
{
	/// <summary>This component lets you know when this component is enabled or disabled.</summary>
	[HelpURL(LeanCommon.PlusHelpUrlPrefix + "LeanActivation")]
	[AddComponentMenu(LeanCommon.ComponentPathPrefix + "Activation")]
	public class LeanActivation : MonoBehaviour
	{
		/// <summary>This event will be invoked when this component is enabled.</summary>
		public UnityEvent OnEnabled { get { if (onEnabled == null) onEnabled = new UnityEvent(); return onEnabled; } } [SerializeField] private UnityEvent onEnabled;

		/// <summary>This event will be invoked when this component is disabled.</summary>
		public UnityEvent OnDisabled { get { if (onDisabled == null) onDisabled = new UnityEvent(); return onDisabled; } } [SerializeField] private UnityEvent onDisabled;

		protected virtual void OnEnable()
		{
			if (onEnabled != null)
			{
				onEnabled.Invoke();
			}
		}

		protected virtual void OnDisable()
		{
			if (onDisabled != null)
			{
				onDisabled.Invoke();
			}
		}
	}
}

#if UNITY_EDITOR
namespace Lean.Common.Editor
{
	using UnityEditor;
	using TARGET = LeanActivation;

	[CanEditMultipleObjects]
	[CustomEditor(typeof(TARGET))]
	public class LeanActivation_Editor : CwEditor
	{
		protected override void OnInspector()
		{
			TARGET tgt; TARGET[] tgts; GetTargets(out tgt, out tgts);

			Draw("onEnabled", "This event will be invoked when this component is enabled.");
			Draw("onDisabled", "This event will be invoked when this component is disabled.");
		}
	}
}
#endif