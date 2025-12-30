using UnityEngine;
using CW.Common;

namespace Lean.Common
{
	/// <summary>This component allows you to spawn a prefab at a point, and have it thrown toward the target.</summary>
	[HelpURL(LeanCommon.PlusHelpUrlPrefix + "LeanSpawnBetween")]
	[AddComponentMenu(LeanCommon.ComponentPathPrefix + "Spawn Between")]
	public class LeanSpawnBetween : MonoBehaviour
	{
		/// <summary>The prefab that gets spawned.</summary>
		public Transform Prefab { set { prefab = value; } get { return prefab; } } [SerializeField] private Transform prefab;

		/// <summary>When calling Spawn, this allows you to specify the spawned velocity.</summary>
		public float VelocityMultiplier { set { velocityMultiplier = value; } get { return velocityMultiplier; } } [SerializeField] private float velocityMultiplier = 1.0f;

		public float VelocityMin { set { velocityMin = value; } get { return velocityMin; } } [SerializeField] private float velocityMin = -1.0f;

		public float VelocityMax { set { velocityMax = value; } get { return velocityMax; } } [SerializeField] private float velocityMax = -1.0f;

		public void Spawn(Vector3 start, Vector3 end)
		{
			if (prefab != null)
			{
				// Vector between points
				var direction = Vector3.Normalize(end - start);

				// Angle between points
				var angle = Mathf.Atan2(direction.x, direction.y) * Mathf.Rad2Deg;

				// Instance the prefab, position it at the start point, and rotate it to the vector
				var instance = Instantiate(prefab);

				instance.position = start;
				instance.rotation = Quaternion.Euler(0.0f, 0.0f, -angle);

				instance.gameObject.SetActive(true);

				// Calculate force
				var force = Vector3.Distance(start, end) * velocityMultiplier;

				if (velocityMin >= 0.0f)
				{
					force = Mathf.Max(force, velocityMin);
				}

				if (velocityMax >= 0.0f)
				{
					force = Mathf.Min(force, velocityMax);
				}

				// Apply 3D force?
				var rigidbody3D = instance.GetComponent<Rigidbody>();

				if (rigidbody3D != null)
				{
					#if UNITY_6000_0_OR_NEWER
						rigidbody3D.linearVelocity = direction * force;
					#else
						rigidbody3D.velocity = direction * force;
					#endif
				}

				// Apply 2D force?
				var rigidbody2D = instance.GetComponent<Rigidbody2D>();

				if (rigidbody2D != null)
				{
					#if UNITY_6000_0_OR_NEWER
						rigidbody2D.linearVelocity = direction * force;
					#else
						rigidbody2D.velocity = direction * force;
					#endif
				}
			}
		}
	}
}

#if UNITY_EDITOR
namespace Lean.Common.Editor
{
	using UnityEditor;
	using TARGET = LeanSpawnBetween;

	[CanEditMultipleObjects]
	[CustomEditor(typeof(TARGET))]
	public class LeanSpawnBetween_Editor : CwEditor
	{
		protected override void OnInspector()
		{
			TARGET tgt; TARGET[] tgts; GetTargets(out tgt, out tgts);

			Draw("prefab", "The prefab that gets spawned.");
			Draw("velocityMultiplier", "When calling Spawn, this allows you to specify the spawned velocity.");
			Draw("velocityMin");
			Draw("velocityMax");
		}
	}
}
#endif