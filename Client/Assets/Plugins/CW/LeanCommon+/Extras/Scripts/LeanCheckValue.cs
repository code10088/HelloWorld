using UnityEngine;
using CW.Common;

namespace Lean.Common
{
	/// <summary>This component checks if the specified <b>LeanValue</b> component's <b>X</b>, <b>Y</b>, or <b>Z</b> value is within the specified range. If so, it will invoke an event, allowing you to perform a custom action.</summary>
	[HelpURL(LeanCommon.PlusHelpUrlPrefix + "LeanCheckValue")]
	[AddComponentMenu(LeanCommon.ComponentPathPrefix + "Check Value")]
	public class LeanCheckValue : LeanCheck
	{
		/// <summary>The value we will check.</summary>
		public LeanValue Target { get { return target; } set { target = value; } } [SerializeField] private LeanValue target;

		/// <summary>Check the X axis?</summary>
		public bool X { set { x = value; } get { return x; } } [SerializeField] private bool x;

		public float XMin { set { xMin = value; } get { return xMin; } } [SerializeField] private float xMin = -1.0f;

		public float XMax { set { xMax = value; } get { return xMax; } } [SerializeField] private float xMax =  1.0f;

		/// <summary>Check the Y axis?</summary>
		public bool Y { set { y = value; } get { return y; } } [SerializeField] private bool y;

		public float YMin { set { yMin = value; } get { return yMin; } } [SerializeField] private float yMin = -1.0f;

		public float YMax { set { yMax = value; } get { return yMax; } } [SerializeField] private float yMax =  1.0f;

		/// <summary>Check the Z axis?</summary>
		public bool Z { set { z = value; } get { return z; } } [SerializeField] private bool z;

		public float ZMin { set { zMin = value; } get { return zMin; } } [SerializeField] private float zMin = -1.0f;

		public float ZMax { set { zMax = value; } get { return zMax; } } [SerializeField] private float zMax =  1.0f;

		[ContextMenu("Update Check")]
		public override void UpdateCheck()
		{
			if (target != null && (x == true || y == true || z == true))
			{
				var newMatched = true;

				if (x == true && (target.Current.x < xMin || target.Current.x > xMax))
				{
					newMatched = false;
				}

				if (y == true && (target.Current.y < yMin || target.Current.y > yMax))
				{
					newMatched = false;
				}

				if (z == true && (target.Current.z < zMin || target.Current.z > zMax))
				{
					newMatched = false;
				}

				SetMatched(newMatched);
			}
		}
	}
}

#if UNITY_EDITOR
namespace Lean.Common.Editor
{
	using UnityEditor;
	using TARGET = LeanCheckValue;

	[CanEditMultipleObjects]
	[CustomEditor(typeof(TARGET))]
	public class LeanCheckValue_Editor : LeanCheck_Editor
	{
		protected override void OnInspector()
		{
			TARGET tgt; TARGET[] tgts; GetTargets(out tgt, out tgts);

			BeginError(Any(tgts, t => t.Target == null));
				Draw("target", "The value we will check.");
			EndError();

			BeginError(Any(tgts, t => t.X == false && t.Y == false && t.Z == false));
				Draw("x", "Clamp the X axis?");
				if (Any(tgts, t => t.X == true))
				{
					BeginIndent();
						Draw("xMin", "", "Min");
						Draw("xMax", "", "Max");
					EndIndent();
				}

				Draw("y", "Clamp the Y axis?");
				if (Any(tgts, t => t.Y == true))
				{
					BeginIndent();
						Draw("yMin", "", "Min");
						Draw("yMax", "", "Max");
					EndIndent();
				}

				Draw("z", "Clamp the Z axis?");
				if (Any(tgts, t => t.Z == true))
				{
					BeginIndent();
						Draw("zMin", "", "Min");
						Draw("zMax", "", "Max");
					EndIndent();
				}
			EndError();

			base.OnInspector();
		}
	}
}
#endif