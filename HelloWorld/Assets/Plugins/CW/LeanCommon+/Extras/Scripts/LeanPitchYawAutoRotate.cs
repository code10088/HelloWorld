using UnityEngine;
using CW.Common;

namespace Lean.Common
{
	/// <summary>This component adds auto Yaw rotation to the attached LeanPitchYaw component.</summary>
	[RequireComponent(typeof(LeanPitchYaw))]
	[HelpURL(LeanCommon.PlusHelpUrlPrefix + "LeanPitchYawAutoRotate")]
	[AddComponentMenu(LeanCommon.ComponentPathPrefix + "Pitch Yaw Auto Rotate")]
	public class LeanPitchYawAutoRotate : MonoBehaviour
	{
		/// <summary>The amount of seconds until auto rotation begins after no touches.</summary>
		public float Delay { set { delay = value; } get { return delay; } } [SerializeField] private float delay = 5.0f;

		/// <summary>The speed of the yaw changes.</summary>
		public float Speed { set { speed = value; } get { return speed; } } [SerializeField] private float speed = 5.0f;

		/// <summary>The speed the auto rotation goes from 0% to 100%.</summary>
		public float Acceleration { set { acceleration = value; } get { return acceleration; } } [SerializeField] private float acceleration = 1.0f;

		[SerializeField]
		private float idleTime;

		[SerializeField]
		private float strength;

		[SerializeField]
		private float expectedPitch;

		[SerializeField]
		private float expectedYaw;

		[System.NonSerialized]
		private LeanPitchYaw cachedPitchYaw;

		protected virtual void OnEnable()
		{
			cachedPitchYaw = GetComponent<LeanPitchYaw>();
		}

		protected virtual void LateUpdate()
		{
			if (cachedPitchYaw.Pitch == expectedPitch && cachedPitchYaw.Yaw == expectedYaw)
			{
				idleTime += Time.deltaTime;

				if (idleTime >= delay)
				{
					strength += acceleration * Time.deltaTime;

					cachedPitchYaw.Yaw += Mathf.Clamp01(strength) * speed * Time.deltaTime;

					//cachedPitchYaw.UpdateRotation();
				}
			}
			else
			{
				idleTime = 0.0f;
				strength = 0.0f;
			}

			expectedPitch = cachedPitchYaw.Pitch;
			expectedYaw   = cachedPitchYaw.Yaw;
		}
	}
}

#if UNITY_EDITOR
namespace Lean.Common.Editor
{
	using UnityEditor;
	using TARGET = LeanPitchYawAutoRotate;

	[CanEditMultipleObjects]
	[CustomEditor(typeof(TARGET))]
	public class LeanPitchYawAutoRotate_Editor : CwEditor
	{
		protected override void OnInspector()
		{
			TARGET tgt; TARGET[] tgts; GetTargets(out tgt, out tgts);

			Draw("delay", "The amount of seconds until auto rotation begins after no touches.");
			Draw("speed", "The speed of the yaw changes.");
			Draw("acceleration", "The speed the auto rotation goes from 0% to 100%.");
		}
	}
}
#endif