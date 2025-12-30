using UnityEngine;
using System.Collections.Generic;

namespace Lean.Common
{
	/// <summary>This component checks if the specified <b>LeanCheck</b> components are matched, allowing you to perform a custom action.</summary>
	[HelpURL(LeanCommon.PlusHelpUrlPrefix + "LeanCheckMultiple")]
	[AddComponentMenu(LeanCommon.ComponentPathPrefix + "Check Multiple")]
	public class LeanCheckMultiple : LeanCheck
	{
		/// <summary>The components we will check.</summary>
		public List<LeanCheck> Targets { get { if (targets == null) targets = new List<LeanCheck>(); return targets; } set { targets = value; } } [SerializeField] private List<LeanCheck> targets;

		/// <summary>The minimum amount of targets that must be matched.
		/// -1 = Max.</summary>
		public int MatchMin { set { matchMin = value; } get { return matchMin; } } [SerializeField] private int matchMin = -1;

		/// <summary>The maximum amount of targets that must be matched.
		/// -1 = Max.</summary>
		public int MatchMax { set { matchMax = value; } get { return matchMax; } } [SerializeField] private int matchMax = -1;

		public int MatchedCount
		{
			get
			{
				var total = 0;

				foreach (var check in targets)
				{
					if (check != null && check.Matched == true)
					{
						total += 1;
					}
				}

				return total;
			}
		}

		[ContextMenu("Update Check")]
		public override void UpdateCheck()
		{
			if (targets != null && targets.Count > 0)
			{
				var min = matchMin >= 0 ? matchMin : Targets.Count;
				var max = matchMax >= 0 ? matchMax : Targets.Count;
				var raw = MatchedCount;

				SetMatched(raw >= min && raw <= max);
			}
		}
	}
}

#if UNITY_EDITOR
namespace Lean.Common.Editor
{
	using UnityEditor;
	using TARGET = LeanCheckMultiple;

	[CanEditMultipleObjects]
	[CustomEditor(typeof(TARGET))]
	public class LeanCheckMultiple_Editor : LeanCheck_Editor
	{
		protected override void OnInspector()
		{
			TARGET tgt; TARGET[] tgts; GetTargets(out tgt, out tgts);

			BeginError(Any(tgts, t => t.Targets.Count == 0));
				Draw("targets", "The components we will check.");
			EndError();
			Draw("matchMin", "The minimum amount of targets that must be matched.\n\n-1 = Max.");
			Draw("matchMax", "The maximum amount of targets that must be matched.\n\n-1 = Max.");

			base.OnInspector();
		}
	}
}
#endif