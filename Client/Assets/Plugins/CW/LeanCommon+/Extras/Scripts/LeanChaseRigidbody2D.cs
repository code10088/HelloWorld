using UnityEngine;
using CW.Common;

namespace Lean.Common
{
	/// <summary>This script allows you to drag this Rigidbody2D in a way that causes it to chase the specified position.</summary>
	[RequireComponent(typeof(Rigidbody2D))]
	[HelpURL(LeanCommon.PlusHelpUrlPrefix + "LeanChaseRigidbody2D")]
	[AddComponentMenu(LeanCommon.ComponentPathPrefix + "Chase Rigidbody2D")]
	public class LeanChaseRigidbody2D : LeanChase
	{
		public enum AxisType
		{
			HorizontalAndVertical,
			Horizontal,
			Vertical
		}

		/// <summary>This allows you to control which axes the velocity can apply to.</summary>
		public AxisType Axis { set { axis = value; } get { return axis; } } [SerializeField] private AxisType axis;

		[System.NonSerialized]
		private Rigidbody2D cachedRigidbody;

		[System.NonSerialized]
		protected bool fixedUpdateCalled;

		/// <summary>This method will override the Position value based on the specified value.</summary>
		public override void SetPosition(Vector3 newPosition)
		{
			base.SetPosition(newPosition);

			fixedUpdateCalled = false;
		}

		protected virtual void OnEnable()
		{
			cachedRigidbody = GetComponent<Rigidbody2D>();
		}

		protected override void UpdatePosition(float damping, float linear)
		{
			if (positionSet == true || continuous == true)
			{
				if (destination != null)
				{
					position = destination.TransformPoint(destinationOffset);
				}

				var currentPosition = (Vector2)(transform.position);
				var targetPosition  = (Vector2)(position + offset);

				var direction = targetPosition - currentPosition;
				var velocity  = direction / Time.fixedDeltaTime;

				// Apply the velocity
				velocity *= CwHelper.DampenFactor(damping, Time.fixedDeltaTime);
				velocity  = Vector3.MoveTowards(velocity, Vector3.zero, linear * Time.fixedDeltaTime);

				#if UNITY_6000_0_OR_NEWER
					if (axis == AxisType.Horizontal)
					{
						velocity.y = cachedRigidbody.linearVelocity.y;
					}
					else if (axis == AxisType.Vertical)
					{
						velocity.x = cachedRigidbody.linearVelocity.x;
					}

					cachedRigidbody.linearVelocity = velocity;
				#else
					if (axis == AxisType.Horizontal)
					{
						velocity.y = cachedRigidbody.velocity.y;
					}
					else if (axis == AxisType.Vertical)
					{
						velocity.x = cachedRigidbody.velocity.x;
					}

					cachedRigidbody.velocity = velocity;
				#endif

				fixedUpdateCalled = true;
			}
		}

		protected virtual void LateUpdate()
		{
			if (fixedUpdateCalled == true)
			{
				positionSet       = false;
				fixedUpdateCalled = false;
			}
		}
	}
}

#if UNITY_EDITOR
namespace Lean.Common.Editor
{
	using UnityEditor;
	using TARGET = LeanChaseRigidbody2D;

	[CanEditMultipleObjects]
	[CustomEditor(typeof(TARGET))]
	public class LeanChaseRigidbody2D_Editor : LeanChase_Editor
	{
		protected override void OnInspector()
		{
			TARGET tgt; TARGET[] tgts; GetTargets(out tgt, out tgts);

			base.OnInspector();

			Separator();

			Draw("axis", "This allows you to control which axes the velocity can apply to.");
		}
	}
}
#endif