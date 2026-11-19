using UnityEngine;
using CW.Common;

namespace Lean.Common
{
	/// <summary>This component allows you to add force to the current GameObject using events.</summary>
	[HelpURL(LeanCommon.PlusHelpUrlPrefix + "LeanManualVelocity2D")]
	[AddComponentMenu(LeanCommon.ComponentPathPrefix + "Manual Velocity 2D")]
	public class LeanManualVelocity2D : MonoBehaviour
	{
		/// <summary>If your Rigidbody2D is on a different GameObject, set it here.</summary>
		public GameObject Target { set { target = value; } get { return target; } } [SerializeField] private GameObject target;

		/// <summary>The force mode.</summary>
		public ForceMode2D Mode { set { mode = value; } get { return mode; } } [SerializeField] private ForceMode2D mode;

		/// <summary>The applied velocity will be multiplied by this.</summary>
		public float Multiplier { set { multiplier = value; } get { return multiplier; } } [SerializeField] private float multiplier = 1.0f;

		/// <summary>The velocity space.</summary>
		public Space Space { set { space = value; } get { return space; } } [SerializeField] private Space space = Space.World;

		/// <summary>The first force direction.</summary>
		public Vector2 DirectionA { set { directionA = value; } get { return directionA; } } [SerializeField] private Vector2 directionA = Vector2.right;

		/// <summary>The second force direction.</summary>
		public Vector2 DirectionB { set { directionB = value; } get { return directionB; } } [SerializeField] private Vector2 directionB = Vector2.up;

		/// <summary>If you want this component to translate an object relative to a camera, enable this setting.</summary>
		public bool RotateDirectionsToCamera { set { rotateDirectionsToCamera = value; } get { return rotateDirectionsToCamera; } } [SerializeField] private bool rotateDirectionsToCamera;

		/// <summary>The camera used by the <b>RotateAxesToCamera</b> setting.
		/// None/null = MainCamera.</summary>
		public Camera Camera { set { _camera = value; } get { return _camera; } } [SerializeField] private Camera _camera;

		public void AddForceA(float delta)
		{
			AddForce(directionA * delta);
		}

		public void AddForceB(float delta)
		{
			AddForce(directionB * delta);
		}

		public void AddForceAB(Vector2 delta)
		{
			AddForce(directionA * delta.x + directionB * delta.y);
		}

		public void AddForceFromTo(Vector3 from, Vector3 to)
		{
			AddForce(to - from);
		}

		public void AddForce(Vector3 delta)
		{
			var finalGameObject = target != null ? target : gameObject;
			var rigidbody       = finalGameObject.GetComponent<Rigidbody2D>();

			if (rigidbody != null)
			{
				var force = delta * multiplier;

				if (rotateDirectionsToCamera == true)
				{
					var finalCamera = CwHelper.GetCamera(_camera);

					if (finalCamera != null)
					{
						force = finalCamera.transform.TransformVector(force);
					}
				}
				else if (space == Space.Self)
				{
					force = rigidbody.transform.rotation * force;
				}

				rigidbody.AddForce(force, mode);
			}
		}
	}
}

#if UNITY_EDITOR
namespace Lean.Common.Editor
{
	using UnityEditor;
	using TARGET = LeanManualVelocity2D;

	[CanEditMultipleObjects]
	[CustomEditor(typeof(TARGET), true)]
	public class LeanManualVelocity2D_Editor : CwEditor
	{
		protected override void OnInspector()
		{
			TARGET tgt; TARGET[] tgts; GetTargets(out tgt, out tgts);

			Draw("target", "If your Rigidbody2D is on a different GameObject, set it here.");
			Draw("mode", "The force mode.");
			Draw("multiplier", "The applied velocity will be multiplied by this.");
			Draw("space", "The velocity space.");
			Draw("directionA", "The first force direction.");
			Draw("directionB", "The second force direction.");
			Draw("rotateDirectionsToCamera", "If you want this component to translate an object relative to a camera, enable this setting.");
			if (Any(tgts, t => t.RotateDirectionsToCamera == true))
			{
				BeginIndent();
					Draw("_camera", "The camera used by the <b>RotateDirectionsToCamera</b> setting.\n\nNone/null = MainCamera.");
				EndIndent();
			}
		}
	}
}
#endif