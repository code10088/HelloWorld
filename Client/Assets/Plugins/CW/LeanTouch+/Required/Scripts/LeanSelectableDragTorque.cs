using UnityEngine;
using CW.Common;
using Lean.Common;

namespace Lean.Touch
{
	/// <summary>This script will add torque to the attached Rigidbody based on finger spin gestures.</summary>
	[RequireComponent(typeof(Rigidbody))]
	[HelpURL(LeanTouch.PlusHelpUrlPrefix + "LeanSelectableDragTorque")]
	[AddComponentMenu(LeanTouch.ComponentPathPrefix + "Selectable Drag Torque")]
	public class LeanSelectableDragTorque : LeanSelectableByFingerBehaviour
	{
		/// <summary>The camera this component will calculate using.
		/// None/null = MainCamera.</summary>
		public Camera Camera { set { _camera = value; } get { return _camera; } } [SerializeField] private Camera _camera;

		/// <summary>The torque force multiplier.</summary>
		public float Force { set { force = value; } get { return force; } } [SerializeField] private float force = 0.1f;

		// The previous finger.ScaledDelta
		[System.NonSerialized]
		private Vector2 oldScaledDelta;

		// The cached rigidbody attached to this GameObject
		[System.NonSerialized]
		private Rigidbody cachedRigidbody;

		protected override void OnSelected(LeanSelect select)
		{
			oldScaledDelta = Vector3.zero;
		}

		protected virtual void Update()
		{
			// Is this GameObject selected?
			if (Selectable != null && Selectable.IsSelected == true)
			{
				// Does it have a selected finger?
				var finger = Selectable.SelectingFinger;

				if (finger != null)
				{
					// Make sure the camera exists
					var camera = CwHelper.GetCamera(_camera, gameObject);

					if (camera != null)
					{
						var newScaledDelta = finger.ScaledDelta;

						if (oldScaledDelta != Vector2.zero && newScaledDelta != Vector2.zero)
						{
							var angleA = Mathf.Atan2(oldScaledDelta.y, oldScaledDelta.x) * Mathf.Rad2Deg;
							var angleB = Mathf.Atan2(newScaledDelta.y, newScaledDelta.x) * Mathf.Rad2Deg;
							var torque = Mathf.DeltaAngle(angleA, angleB) * (oldScaledDelta.magnitude + newScaledDelta.magnitude);

							if (cachedRigidbody == null) cachedRigidbody = GetComponent<Rigidbody>();

							cachedRigidbody.AddTorque(camera.transform.forward * torque * force, ForceMode.Acceleration);
						}

						oldScaledDelta = newScaledDelta;
					}
					else
					{
						Debug.LogError("Failed to find camera. Either tag your cameras MainCamera, or set one in this component.", this);
					}
				}
			}
		}
	}
}

#if UNITY_EDITOR
namespace Lean.Touch.Editor
{
	using UnityEditor;
	using TARGET = LeanSelectableDragTorque;

	[CanEditMultipleObjects]
	[CustomEditor(typeof(TARGET))]
	public class LeanSelectableDragTorque_Editor : CwEditor
	{
		protected override void OnInspector()
		{
			TARGET tgt; TARGET[] tgts; GetTargets(out tgt, out tgts);

			Draw("_camera", "The camera this component will calculate using.\n\nNone/null = MainCamera.");
			Draw("force", "The torque force multiplier.");
		}
	}
}
#endif