using UnityEngine;
using UnityEngine.Events;
using CW.Common;

namespace Lean.Common
{
	/// <summary>This component allows you to detect when any selectable object in the scene has been deselected.</summary>
	[HelpURL(LeanCommon.PlusHelpUrlPrefix + "LeanDeselected")]
	[AddComponentMenu(LeanCommon.ComponentPathPrefix + "Deselected")]
	public class LeanDeselected : MonoBehaviour
	{
		[System.Serializable] public class LeanSelectableEvent : UnityEvent<LeanSelectable> {}

		public LeanSelectableEvent OnSelectable { get { if (onSelectable == null) onSelectable = new LeanSelectableEvent(); return onSelectable; } } [SerializeField] private LeanSelectableEvent onSelectable;

		protected virtual void OnEnable()
		{
			LeanSelect.OnAnyDeselected += HandleAnyDeselected;
		}
		protected virtual void OnDisable()
		{
			LeanSelect.OnAnyDeselected -= HandleAnyDeselected;
		}

		private void HandleAnyDeselected(LeanSelect select, LeanSelectable selectable)
		{
			if (onSelectable != null)
			{
				onSelectable.Invoke(selectable);
			}
		}
	}
}

#if UNITY_EDITOR
namespace Lean.Common.Editor
{
	using UnityEditor;
	using TARGET = LeanDeselected;

	[CanEditMultipleObjects]
	[CustomEditor(typeof(TARGET), true)]
	public class LeanDeselected_Editor : CwEditor
	{
		protected override void OnInspector()
		{
			TARGET tgt; TARGET[] tgts; GetTargets(out tgt, out tgts);

			Draw("onSelectable");
		}
	}
}
#endif