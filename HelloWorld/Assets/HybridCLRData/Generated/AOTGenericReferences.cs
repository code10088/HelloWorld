using System.Collections.Generic;
public class AOTGenericReferences : UnityEngine.MonoBehaviour
{

	// {{ AOT assemblies
	public static readonly IReadOnlyList<string> PatchedAOTAssemblyList = new List<string>
	{
		"Assembly-CSharp.dll",
		"UniTask.dll",
		"UnityEngine.CoreModule.dll",
		"mscorlib.dll",
		"protobuf-net.dll",
	};
	// }}

	// {{ constraint implement type
	// }} 

	// {{ AOT generic types
	// Singletion<object>
	// System.Action<byte>
	// System.Action<float>
	// System.Action<int,object>
	// System.Action<object>
	// System.Collections.Generic.Dictionary<cfg.UIType,object>
	// System.Collections.Generic.Dictionary<int,object>
	// System.Collections.Generic.Dictionary<object,object>
	// System.Collections.Generic.List.Enumerator<object>
	// System.Collections.Generic.List<object>
	// System.Collections.Generic.Queue<object>
	// System.Predicate<object>
	// UnityEngine.UI.CustomLoopScroll<object>
	// }}

	public void RefMethods()
	{
		// int AssetManager.Load<object>(string,System.Action<int,UnityEngine.Object>)
		// System.Void Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskMethodBuilder.AwaitUnsafeOnCompleted<Cysharp.Threading.Tasks.UniTask.Awaiter,object>(Cysharp.Threading.Tasks.UniTask.Awaiter&,object&)
		// System.Void Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskMethodBuilder.AwaitUnsafeOnCompleted<Cysharp.Threading.Tasks.YieldAwaitable.Awaiter,object>(Cysharp.Threading.Tasks.YieldAwaitable.Awaiter&,object&)
		// System.Void Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskMethodBuilder.Start<object>(object&)
		// object ProtoBuf.Serializer.Deserialize<object>(System.IO.Stream)
		// object[] System.Array.Empty<object>()
		// int System.Array.FindIndex<object>(object[],System.Predicate<object>)
		// object System.Threading.Interlocked.CompareExchange<object>(object&,object,object)
		// object UnityEngine.Component.GetComponentInParent<object>()
		// object UnityEngine.GameObject.GetComponent<object>()
		// object[] UnityEngine.GameObject.GetComponentsInChildren<object>(bool)
	}
}