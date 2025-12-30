using UnityEngine;
using CW.Common;

namespace Lean.Common
{
	/// <summary>This component allows you to rotate the current GameObject using events.</summary>
	[HelpURL(LeanCommon.PlusHelpUrlPrefix + "LeanManualRotate")]
	[AddComponentMenu(LeanCommon.ComponentPathPrefix + "Manual Rotate")]
	public class LeanManualRotate : MonoBehaviour
	{
		/// <summary>If you want this component to work on a different GameObject, then specify it here. This can be used to improve organization if your GameObject already has many components.</summary>
		public GameObject Target { set { target = value; } get { return target; } } [SerializeField] private GameObject target;

		/// <summary>This allows you to set the coordinate space the rotation will use.</summary>
		public Space Space { set { space = value; } get { return space; } } [SerializeField] private Space space;

		/// <summary>The first rotation axis, used when calling RotateA or RotateAB.</summary>
		public Vector3 AxisA { set { axisA = value; } get { return axisA; } } [SerializeField] private Vector3 axisA = Vector3.down;

		/// <summary>The second rotation axis, used when calling RotateB or RotateAB.</summary>
		public Vector3 AxisB { set { axisB = value; } get { return axisB; } } [SerializeField] private Vector3 axisB = Vector3.right;

		/// <summary>If you want this component to rotate an object relative to a camera, enable this setting.</summary>
		public bool RotateAxesToCamera { set { rotateAxesToCamera = value; } get { return rotateAxesToCamera; } } [SerializeField] private bool rotateAxesToCamera;

		/// <summary>The camera used by the <b>RotateAxesToCamera</b> setting.
		/// None/null = MainCamera.</summary>
		public Camera Camera { set { _camera = value; } get { return _camera; } } [SerializeField] private Camera _camera;

		/// <summary>The rotation angle is multiplied by this.
		/// 1 = Normal rotation.
		/// 2 = Double rotation.</summary>
		public float Multiplier { set { multiplier = value; } get { return multiplier; } } [SerializeField] private float multiplier = 1.0f;

		/// <summary>If you want this component to change smoothly over time, then this allows you to control how quick the changes reach their target value.
		/// -1 = Instantly change.
		/// 1 = Slowly change.
		/// 10 = Quickly change.</summary>
		public float Damping { set { damping = value; } get { return damping; } } [SerializeField] private float damping = 10.0f;

		/// <summary>If you enable this then the rotation will be multiplied by Time.deltaTime. This allows you to maintain frame rate independent rotation.</summary>
		public bool ScaleByTime { set { scaleByTime = value; } get { return scaleByTime; } } [SerializeField] private bool scaleByTime;

		/// <summary>If you call the ResetRotation method, the rotation will be set to this Euler rotation.</summary>
		public Vector3 DefaultRotation { set { defaultRotation = value; } get { return defaultRotation; } } [SerializeField] private Vector3 defaultRotation;

		[SerializeField]
		private Quaternion remainingDelta = Quaternion.identity;

		/// <summary>This method will reset the rotation to the specified DefaultRotation value.</summary>
		[ContextMenu("Reset Rotation")]
		public void ResetRotation()
		{
			var finalTransform = target != null ? target.transform : transform;
			var oldRotation    = finalTransform.localRotation;

			if (space == Space.Self)
			{
				finalTransform.localRotation = Quaternion.Euler(defaultRotation);
			}
			else
			{
				finalTransform.rotation = Quaternion.Euler(defaultRotation);
			}

			remainingDelta *= Quaternion.Inverse(oldRotation) * finalTransform.localRotation;

			// Revert
			finalTransform.localRotation = oldRotation;
		}

		/// <summary>This method will cause the rotation to immediately snap to its final value.</summary>
		[ContextMenu("Snap To Target")]
		public void SnapToTarget()
		{
			UpdateRotation(1.0f);
		}

		/// <summary>This method will clear the target rotation value, causing the rotation to stop.</summary>
		[ContextMenu("Stop Rotation")]
		public void StopRotation()
		{
			remainingDelta = Quaternion.identity;
		}

		/// <summary>This method allows you to rotate around AxisA, with the specified angle in degrees.</summary>
		public void RotateA(float delta)
		{
			RotateAB(new Vector2(delta, 0.0f));
		}

		/// <summary>This method allows you to rotate around AxisB, with the specified angle in degrees.</summary>
		public void RotateB(float delta)
		{
			RotateAB(new Vector2(0.0f, delta));
		}

		/// <summary>This method allows you to rotate around AxisA and AxisB, with the specified angles in degrees.</summary>
		public void RotateAB(Vector2 delta)
		{
			var finalTransform = target != null ? target.transform : transform;
			var oldRotation    = finalTransform.localRotation;

			if (scaleByTime == true)
			{
				delta *= Time.deltaTime;
			}

			if (rotateAxesToCamera == true)
			{
				var finalCamera = CwHelper.GetCamera(_camera);

				if (finalCamera != null)
				{
					var finalAxisA = finalCamera.transform.TransformDirection(axisA);
					var finalAxisB = finalCamera.transform.TransformDirection(axisB);

					finalTransform.Rotate(finalAxisA, delta.x * multiplier, Space.World);
					finalTransform.Rotate(finalAxisB, delta.y * multiplier, Space.World);
				}
			}
			else
			{
				finalTransform.Rotate(axisA, delta.x * multiplier, space);
				finalTransform.Rotate(axisB, delta.y * multiplier, space);
			}

			remainingDelta *= Quaternion.Inverse(oldRotation) * finalTransform.localRotation;

			// Revert
			finalTransform.localRotation = oldRotation;
		}

		protected virtual void Update()
		{
			var factor = CwHelper.DampenFactor(damping, Time.deltaTime);

			UpdateRotation(factor);
		}

		private void UpdateRotation(float factor)
		{
			var finalTransform = target != null ? target.transform : transform;
			var newDelta       = Quaternion.Slerp(remainingDelta, Quaternion.identity, factor);

			finalTransform.localRotation = finalTransform.localRotation * Quaternion.Inverse(newDelta) * remainingDelta;

			remainingDelta = newDelta;
		}
	}
}

#if UNITY_EDITOR
namespace Lean.Common.Editor
{
	using UnityEditor;
	using TARGET = LeanManualRotate;

	[CanEditMultipleObjects]
	[CustomEditor(typeof(TARGET))]
	public class LeanManualRotate_Editor : CwEditor
	{
		protected override void OnInspector()
		{
			TARGET tgt; TARGET[] tgts; GetTargets(out tgt, out tgts);

			Draw("target", "If you want this component to work on a different GameObject, then specify it here. This can be used to improve organization if your GameObject already has many components.");
			Draw("space", "This allows you to set the coordinate space the rotation will use.");
			Draw("axisA", "The first rotation axis, used when calling RotateA or RotateAB.");
			Draw("axisB", "The second rotation axis, used when calling RotateB or RotateAB.");
			Draw("rotateAxesToCamera", "If you want this component to rotate an object relative to a camera, enable this setting.");
			if (Any(tgts, t => t.RotateAxesToCamera == true))
			{
				BeginIndent();
					Draw("_camera", "The camera used by the <b>RotateAxesToCamera</b> setting.\n\nNone/null = MainCamera.");
				EndIndent();
			}

			Separator();

			Draw("multiplier", "The rotation angle is multiplied by this.\n\n1 = Normal rotation.\n\n2 = Double rotation.");
			Draw("scaleByTime", "If you enable this then the rotation will be multiplied by Time.deltaTime. This allows you to maintain frame rate independent rotation.");
			Draw("damping", "If you want this component to change smoothly over time, then this allows you to control how quick the changes reach their target value.\n\n-1 = Instantly change.\n\n1 = Slowly change.\n\n10 = Quickly change.");
			Draw("defaultRotation", "If you call the ResetRotation method, the rotation will be set to this Euler rotation.");
		}
	}
}
#endif