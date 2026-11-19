using UnityEngine;
using System.Collections.Generic;
using CW.Common;

namespace Lean.Common
{
	/// <summary>This script will constrain the current transform.position to the specified colliders.
	/// NOTE: If you're using a MeshCollider then it must be marked as <b>convex</b>.</summary>
	[DefaultExecutionOrder(200)]
	[HelpURL(LeanCommon.PlusHelpUrlPrefix + "LeanConstrainToColliders")]
	[AddComponentMenu(LeanCommon.ComponentPathPrefix + "Constrain To Colliders")]
	public class LeanConstrainToColliders : MonoBehaviour
	{
		/// <summary>The colliders this transform will be constrained to.</summary>
		public List<Collider> Colliders { get { if (colliders == null) colliders = new List<Collider>(); return colliders; } } [SerializeField] private List<Collider> colliders;

		protected virtual void LateUpdate()
		{
			if (colliders != null)
			{
				var oldPosition = transform.position;
				var newPosition = default(Vector3);
				var distance    = float.PositiveInfinity;
				var count       = 0;
				var moved       = 0;

				for (var i = colliders.Count - 1; i >= 0; i--)
				{
					var collider = colliders[i];

					if (collider != null)
					{
						var testPosition = collider.ClosestPoint(oldPosition);

						if (testPosition != oldPosition)
						{
							moved++;

							var testDistance = Vector3.SqrMagnitude(testPosition - oldPosition);
							
							if (testDistance < distance)
							{
								distance    = testDistance;
								newPosition = testPosition;
							}
						}

						count++;
					}
				}

				if (count > 0 && count == moved)
				{
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
}

#if UNITY_EDITOR
namespace Lean.Common.Editor
{
	using UnityEditor;
	using TARGET = LeanConstrainToColliders;

	[CanEditMultipleObjects]
	[CustomEditor(typeof(TARGET))]
	public class LeanConstrainToColliders_Editor : CwEditor
	{
		protected override void OnInspector()
		{
			TARGET tgt; TARGET[] tgts; GetTargets(out tgt, out tgts);

			Draw("colliders", "The colliders this transform will be constrained to.");
		}
	}
}
#endif