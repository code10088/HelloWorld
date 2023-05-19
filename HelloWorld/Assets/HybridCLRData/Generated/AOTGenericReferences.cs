public class AOTGenericReferences : UnityEngine.MonoBehaviour
{

	// {{ AOT assemblies
	// Assembly-CSharp.dll
	// UnityEngine.CoreModule.dll
	// mscorlib.dll
	// protobuf-net.dll
	// }}

	// {{ constraint implement type
	// }} 

	// {{ AOT generic types
	// Singletion<object>
	// System.Action<object>
	// System.Action<ushort,object>
	// System.Action<int,object>
	// System.Collections.Generic.Dictionary<int,int>
	// System.Collections.Generic.List<object>
	// System.Collections.Generic.Queue<object>
	// System.Func<object>
	// System.Predicate<object>
	// }}

	public void RefMethods()
	{
		// int AssetManager.Load<object>(string,System.Action<int,UnityEngine.Object>)
		// object[] BytesDecode.ToBDIArray<object>(System.Func<object>)
		// System.Void BytesDecode.ToBytes<object>(object[])
		// object ProtoBuf.Serializer.Deserialize<object>(System.IO.Stream)
		// object[] System.Array.Empty<object>()
		// int System.Array.FindIndex<object>(object[],System.Predicate<object>)
		// object System.Threading.Interlocked.CompareExchange<object>(object&,object,object)
		// object UnityEngine.GameObject.GetComponent<object>()
		// object[] UnityEngine.GameObject.GetComponentsInChildren<object>(bool)
	}
}