using UnityEngine;
using CW.Common;

namespace Lean.Touch
{
	/// <summary>This component allows you to translate the current GameObject along the specified surface.</summary>
	[HelpURL(LeanTouch.PlusHelpUrlPrefix + "LeanDragTranslateAlong")]
	[AddComponentMenu(LeanTouch.ComponentPathPrefix + "Drag Translate Along")]
	public class LeanDragTranslateAlong : MonoBehaviour
	{
		/// <summary>If you want this component to work on a different <b>Transform</b>, then specify it here. This can be used to improve organization if your GameObject already has many components.
		/// None/null = Current Transform.</summary>
		public Transform Target { set { target = value; } get { return target; } } [SerializeField] private Transform target;

		/// <summary>The method used to find fingers to use with this component. See LeanFingerFilter documentation for more information.</summary>
		public LeanFingerFilter Use = new LeanFingerFilter(true);

		/// <summary>The method used to find world coordinates from a finger. See LeanScreenDepth documentation for more information.</summary>
		public LeanScreenDepth ScreenDepth = new LeanScreenDepth(LeanScreenDepth.ConversionType.DepthIntercept);

		/// <summary>If you want this component to change smoothly over time, then this allows you to control how quick the changes reach their target value.
		/// -1 = Instantly change.
		/// 1 = Slowly change.
		/// 10 = Quickly change.</summary>
		public float Damping { set { damping = value; } get { return damping; } } [SerializeField] private float damping = -1.0f;

		[System.NonSerialized]
		protected bool targetSet;

		[System.NonSerialized]
		protected Vector3 targetScreenPoint;

		[System.NonSerialized]
		private Vector3 remainingDelta;

		/// <summary>If you've set Use to ManuallyAddedFingers, then you can call this method to manually add a finger.</summary>
		public void AddFinger(LeanFinger finger)
		{
			Use.AddFinger(finger);
		}

		/// <summary>If you've set Use to ManuallyAddedFingers, then you can call this method to manually remove a finger.</summary>
		public void RemoveFinger(LeanFinger finger)
		{
			Use.RemoveFinger(finger);
		}

		/// <summary>If you've set Use to ManuallyAddedFingers, then you can call this method to manually remove all fingers.</summary>
		public void RemoveAllFingers()
		{
			Use.RemoveAllFingers();
		}

		[ContextMenu("Snap Position")]
		protected virtual void SnapPosition()
		{
			var finalTransform       = target != null ? target : transform;
			var snappedWorldPosition = finalTransform.position;

			if (TryGetSnappedWorldPosition(ref snappedWorldPosition) == true)
			{
				finalTransform.position = snappedWorldPosition;
			}
		}

#if UNITY_EDITOR
		protected virtual void Reset()
		{
			Use.UpdateRequiredSelectable(gameObject);
		}
#endif

		protected virtual void Awake()
		{
			Use.UpdateRequiredSelectable(gameObject);
		}

		protected virtual void Update()
		{
			SnapPosition();

			UpdateTarget();

			var finalTransform = target != null ? target : transform;
			var oldPosition    = finalTransform.position;
			var newPosition    = oldPosition;

			// If we're dragging this object, update the remainingDelta
			if (targetSet == true)
			{
				if (ScreenDepth.TryConvert(ref newPosition, targetScreenPoint, gameObject) == true)
				{
					remainingDelta = newPosition - oldPosition;
				}
			}

			// Dampen remainingDelta
			var factor   = CwHelper.DampenFactor(damping, Time.deltaTime);
			var newDelta = Vector3.Lerp(remainingDelta, Vector3.zero, factor);

			// Shift this position by the change in delta
			finalTransform.position = oldPosition + remainingDelta - newDelta;

			// Update remainingDelta with the dampened value
			remainingDelta = newDelta;
		}

		protected bool TryGetSnappedWorldPosition(ref Vector3 worldPosition)
		{
			var camera = CwHelper.GetCamera(ScreenDepth.Camera, gameObject);

			if (camera != null)
			{
				var screenPointA  = camera.WorldToScreenPoint(worldPosition);
				var screenPointB  = screenPointA + Vector3.right;
				var worldPositionA = worldPosition;
				var worldPositionB = worldPosition;

				if (ScreenDepth.TryConvert(ref worldPositionA, screenPointA, gameObject) == true && ScreenDepth.TryConvert(ref worldPositionB, screenPointB, gameObject) == true)
				{
					// Only snap if the snap distance is more than one pixel movement
					// This is to avoid floating point inaccuracy drift
					var snpPixelDelta = Vector3.Distance(worldPosition, worldPositionA);
					var onePixelDelta = Vector3.Distance(worldPositionA, worldPositionB);

					if (snpPixelDelta >= onePixelDelta * 0.5f)
					{
						worldPosition = worldPositionA;

						return true;
					}
				}
			}

			return false;
		}

		protected void UpdateTarget()
		{
			var fingers = Use.UpdateAndGetFingers();
			var camera  = CwHelper.GetCamera(ScreenDepth.Camera, gameObject);

			if (fingers.Count > 0 && camera != null)
			{
				var finalTransform = Target != null ? Target : transform;

				if (targetSet == false)
				{
					targetSet         = true;
					targetScreenPoint = camera.WorldToScreenPoint(finalTransform.position);
				}

				targetScreenPoint += (Vector3)LeanGesture.GetScreenDelta(fingers);
			}
			else
			{
				targetSet = false;
			}

			if (camera == null)
			{
				Debug.LogError("Failed to find camera. Either tag your cameras MainCamera, or set one in this component.", this);
			}
		}
	}
}

#if UNITY_EDITOR
namespace Lean.Touch.Editor
{
	using UnityEditor;
	using TARGET = LeanDragTranslateAlong;

	[CanEditMultipleObjects]
	[CustomEditor(typeof(TARGET))]
	public class LeanDragTranslateAlong_Editor : CwEditor
	{
		protected override void OnInspector()
		{
			TARGET tgt; TARGET[] tgts; GetTargets(out tgt, out tgts);

			Draw("target", "This allows you to control how quickly the target value is reached.");
			Draw("Use");
			Draw("ScreenDepth");
			Draw("damping", "If you want this component to change smoothly over time, then this allows you to control how quick the changes reach their target value.\n\n-1 = Instantly change.\n\n1 = Slowly change.\n\n10 = Quickly change.");
		}
	}
}
#endif