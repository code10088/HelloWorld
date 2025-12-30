using UnityEngine;
using CW.Common;

namespace Lean.Touch
{
	/// <summary>This component works like <b>LeanDragTranslateAlong</b>, but is specifically designed to work with a <b>Rigidbody</b>.</summary>
	[HelpURL(LeanTouch.PlusHelpUrlPrefix + "LeanDragTranslateAlongRigidbody")]
	[AddComponentMenu(LeanTouch.ComponentPathPrefix + "Drag Translate Along Rigidbody")]
	public class LeanDragTranslateAlongRigidbody : LeanDragTranslateAlong
	{
		protected override void SnapPosition()
		{
			var finalTransform = Target != null ? Target : transform;
			var rigidbody      = default(Rigidbody);

			if (finalTransform.TryGetComponent(out rigidbody) == true)
			{
				var worldPosition = rigidbody.position;

				if (TryGetSnappedWorldPosition(ref worldPosition) == true)
				{
					rigidbody.MovePosition(worldPosition);
				}
			}
		}

		protected virtual void FixedUpdate()
		{
			SnapPosition();

			// Only move this object if we're dragging it
			if (targetSet == true)
			{
				var finalTransform = Target != null ? Target.transform : transform;
				var rigidbody      = default(Rigidbody);

				if (finalTransform.TryGetComponent(out rigidbody) == true)
				{
					var oldPosition = finalTransform.position;
					var newPosition = oldPosition;

					if (ScreenDepth.TryConvert(ref newPosition, targetScreenPoint, gameObject) == true)
					{
						var velocity = (newPosition - oldPosition) / Time.fixedDeltaTime;
						var factor   = CwHelper.DampenFactor(Damping, Time.fixedDeltaTime);

						// Apply the velocity
						#if UNITY_6000_0_OR_NEWER
							rigidbody.linearVelocity = velocity * factor;
						#else
							rigidbody.velocity = velocity * factor;
						#endif
					}
				}
			}
		}

		protected override void Update()
		{
			UpdateTarget();
		}
	}
}

#if UNITY_EDITOR
namespace Lean.Touch.Editor
{
	using UnityEditor;
	using TARGET = LeanDragTranslateAlongRigidbody;

	[CanEditMultipleObjects]
	[CustomEditor(typeof(TARGET))]
	public class LeanDragTranslateAlongRigidbody_Editor : LeanDragTranslateAlong_Editor
	{
		protected override void OnInspector()
		{
			TARGET tgt; TARGET[] tgts; GetTargets(out tgt, out tgts);

			base.OnInspector();
		}
	}
}
#endif