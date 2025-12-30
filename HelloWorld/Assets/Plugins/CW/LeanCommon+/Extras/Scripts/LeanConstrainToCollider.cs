using UnityEngine;
using CW.Common;

namespace Lean.Common
{
	/// <summary>This component will constrain the current <b>transform.position</b> to the specified collider.
	/// NOTE: If you're using a MeshCollider then it must be marked as <b>convex</b>.</summary>
	[DefaultExecutionOrder(200)]
	[HelpURL(LeanCommon.PlusHelpUrlPrefix + "LeanConstrainToCollider")]
	[AddComponentMenu(LeanCommon.ComponentPathPrefix + "Constrain To Collider")]
	public class LeanConstrainToCollider : MonoBehaviour
	{
		/// <summary>The collider this transform will be constrained to.</summary>
		public Collider Collider { set { _collider = value; } get { return _collider; } } [SerializeField] private Collider _collider;

		protected virtual void LateUpdate()
		{
			if (_collider != null)
			{
				var oldPosition = transform.position;
				var newPosition = _collider.ClosestPoint(oldPosition);

				if (Mathf.Approximately(oldPosition.x, newPosition.x) == false ||
					Mathf.Approximately(oldPosition.y, newPosition.y) == false ||
					Mathf.Approximately(oldPosition.z, newPosition.z) == false)
				{
					transform.position = newPosition;
				}
			}
		}
	}
}

#if UNITY_EDITOR
namespace Lean.Common.Editor
{
	using UnityEditor;
	using TARGET = LeanConstrainToCollider;

	[CanEditMultipleObjects]
	[CustomEditor(typeof(TARGET))]
	public class LeanConstrainToCollider_Editor : CwEditor
	{
		protected override void OnInspector()
		{
			TARGET tgt; TARGET[] tgts; GetTargets(out tgt, out tgts);

			BeginError(Any(tgts, t => t.Collider == null));
				Draw("_collider", "The collider this transform will be constrained to.");
			EndError();
		}
	}
}
#endif