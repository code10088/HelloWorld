using UnityEngine;
using UnityEngine.Events;
using CW.Common;

namespace Lean.Common
{
	/// <summary>This component allows you to store 1, 2, or 3 values. This is done by calling one of the <b>SetX/Y/Z</b> or <b>IncX/Y/Z</b> methods, and then sending it out using the <b>OnValueX/Y/Z</b> events.</summary>
	[HelpURL(LeanCommon.PlusHelpUrlPrefix + "LeanValue")]
	[AddComponentMenu(LeanCommon.ComponentPathPrefix + "Value")]
	public class LeanValue : MonoBehaviour
	{
		[System.Serializable] public class FloatEvent : UnityEvent<float> {}
		[System.Serializable] public class Vector2Event : UnityEvent<Vector2> {}
		[System.Serializable] public class Vector3Event : UnityEvent<Vector3> {}

		/// <summary>When the <b>Current</b> value is set, this event will be invoked with the <b>Current.x</b> value.</summary>
		public FloatEvent OnValueX { get { if (onValueX == null) onValueX = new FloatEvent(); return onValueX; } } [SerializeField] private FloatEvent onValueX;

		/// <summary>When the <b>Current</b> value is set, this event will be invoked with the <b>Current.y</b> value.</summary>
		public FloatEvent OnValueY { get { if (onValueY == null) onValueY = new FloatEvent(); return onValueY; } } [SerializeField] private FloatEvent onValueY;

		/// <summary>When the <b>Current</b> value is set, this event will be invoked with the <b>Current.z</b> value.</summary>
		public FloatEvent OnValueZ { get { if (onValueZ == null) onValueZ = new FloatEvent(); return onValueZ; } } [SerializeField] private FloatEvent onValueZ;

		/// <summary>When the <b>Current</b> value is set, this event will be invoked with the <b>Current.xy</b> value.</summary>
		public Vector2Event OnValueXY { get { if (onValueXY == null) onValueXY = new Vector2Event(); return onValueXY; } } [SerializeField] private Vector2Event onValueXY;

		/// <summary>When the <b>Current</b> value is set, this event will be invoked with the <b>Current.xyz</b> value.</summary>
		public Vector3Event OnValueXYZ { get { if (onValueXYZ == null) onValueXYZ = new Vector3Event(); return onValueXYZ; } } [SerializeField] private Vector3Event onValueXYZ;

		/// <summary>The current value.</summary>
		public Vector3 Current { set { SetXYZ(value); } get { return current; } } [SerializeField] private Vector3 current;

		public void SetX(float x) { current.x = x; Submit(); }
		public void SetY(float y) { current.y = y; Submit(); }
		public void SetZ(float z) { current.z = z; Submit(); }
		public void SetXY(Vector2 xy) { current.x = xy.x; current.y = xy.y; Submit(); }
		public void SetXYZ(Vector3 xyz) { current = xyz; Submit(); }

		public void SetX(int x) { SetX(x); }
		public void SetY(int y) { SetY(y); }
		public void SetZ(int z) { SetZ(z); }
		public void SetXY(Vector2Int xy) { SetXY(xy); }
		public void SetXYZ(Vector3Int xyz) { SetXYZ(xyz); }

		public void IncX(float v) { SetX(current.x + v); }
		public void IncY(float v) { SetY(current.y + v); }
		public void IncZ(float v) { SetZ(current.z + v); }
		public void IncXY(Vector2 v) { SetXY((Vector2)current + v); }
		public void IncXYZ(Vector3 v) { SetXY(current + v); }

		public void IncX(int v) { SetX(current.x + v); }
		public void IncY(int v) { SetY(current.x + v); }
		public void IncZ(int v) { SetZ(current.x + v); }
		public void IncXY(Vector2Int v) { SetXY((Vector2)current + v); }
		public void IncXYZ(Vector3Int v) { SetXY(current + v); }

		/// <summary>This method will invoke all events with the current value.
		/// NOTE: This will automatically be called every time you assign <b>Current</b>, or call any of the Set/Inc methods.</summary>
		[ContextMenu("Submit")]
		public void Submit()
		{
			if (onValueX != null)
			{
				onValueX.Invoke(current.x);
			}

			if (onValueY != null)
			{
				onValueY.Invoke(current.y);
			}

			if (onValueZ != null)
			{
				onValueZ.Invoke(current.z);
			}

			if (onValueXY != null)
			{
				onValueXY.Invoke(current);
			}

			if (onValueXYZ != null)
			{
				onValueXYZ.Invoke(current);
			}
		}
	}
}

#if UNITY_EDITOR
namespace Lean.Common.Editor
{
	using UnityEditor;
	using TARGET = LeanValue;

	[CanEditMultipleObjects]
	[CustomEditor(typeof(TARGET))]
	public class LeanValue_Editor : CwEditor
	{
		protected override void OnInspector()
		{
			TARGET tgt; TARGET[] tgts; GetTargets(out tgt, out tgts);

			if (Draw("current", "The current value.") == true)
			{
				Each(tgts, t => t.Current = serializedObject.FindProperty("current").vector3Value, true);
			}

			Separator();

			var usedA = Any(tgts, t => t.OnValueX.GetPersistentEventCount() > 0);
			var usedB = Any(tgts, t => t.OnValueY.GetPersistentEventCount() > 0);
			var usedC = Any(tgts, t => t.OnValueZ.GetPersistentEventCount() > 0);
			var usedD = Any(tgts, t => t.OnValueXY.GetPersistentEventCount() > 0);
			var usedE = Any(tgts, t => t.OnValueXYZ.GetPersistentEventCount() > 0);

			var showUnusedEvents = DrawFoldout("Show Unused Events", "Show all events?");

			if (usedA == true || showUnusedEvents == true)
			{
				Draw("onValueX");
			}

			if (usedB == true || showUnusedEvents == true)
			{
				Draw("onValueY");
			}

			if (usedC == true || showUnusedEvents == true)
			{
				Draw("onValueZ");
			}

			if (usedD == true || showUnusedEvents == true)
			{
				Draw("onValueXY");
			}

			if (usedE == true || showUnusedEvents == true)
			{
				Draw("onValueXYZ");
			}
		}
	}
}
#endif