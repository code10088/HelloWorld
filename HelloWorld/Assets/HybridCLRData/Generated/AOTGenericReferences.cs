public class AOTGenericReferences : UnityEngine.MonoBehaviour
{

	// {{ AOT assemblies
	// Assembly-CSharp.dll
	// UnityEngine.CoreModule.dll
	// mscorlib.dll
	// }}

	// {{ constraint implement type
	// }} 

	// {{ AOT generic types
	// System.Action<object>
	// System.Action<int,object,object>
	// System.Collections.Generic.Dictionary<HotAssembly.EventType,object>
	// System.Collections.Generic.Dictionary<int,int>
	// System.Collections.Generic.List<object>
	// System.Func<object>
	// System.Predicate<object>
	// }}

	public void RefMethods()
	{
		// object[] BytesDecode.ToBDIArray<object>(System.Func<object>)
		// System.Void BytesDecode.ToBytes<object>(object[])
		// object[] System.Array.Empty<object>()
		// object System.Threading.Interlocked.CompareExchange<object>(object&,object,object)
		// object UnityEngine.GameObject.GetComponent<object>()
		// object[] UnityEngine.GameObject.GetComponentsInChildren<object>(bool)
	}
}