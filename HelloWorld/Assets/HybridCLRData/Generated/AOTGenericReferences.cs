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
		"protobuf-net.Core.dll",
		"protobuf-net.dll",
	};
	// }}

	// {{ constraint implement type
	// }} 

	// {{ AOT generic types
	// Cysharp.Threading.Tasks.CompilerServices.AsyncUniTask.<>c<object>
	// Cysharp.Threading.Tasks.CompilerServices.AsyncUniTask<object>
	// Cysharp.Threading.Tasks.ITaskPoolNode<object>
	// Cysharp.Threading.Tasks.TaskPool<object>
	// Cysharp.Threading.Tasks.UniTaskCompletionSourceCore<Cysharp.Threading.Tasks.AsyncUnit>
	// ProtoBuf.Internal.IValueChecker<object>
	// ProtoBuf.Serializers.IFactory<object>
	// ProtoBuf.Serializers.IRepeatedSerializer<object>
	// ProtoBuf.Serializers.ISerializer<object>
	// Singletion<object>
	// System.Action<byte>
	// System.Action<float>
	// System.Action<int,object>
	// System.Action<object>
	// System.Collections.Generic.ArraySortHelper<object>
	// System.Collections.Generic.Comparer<object>
	// System.Collections.Generic.Dictionary.Enumerator<int,object>
	// System.Collections.Generic.Dictionary.Enumerator<object,object>
	// System.Collections.Generic.Dictionary.KeyCollection.Enumerator<int,object>
	// System.Collections.Generic.Dictionary.KeyCollection.Enumerator<object,object>
	// System.Collections.Generic.Dictionary.KeyCollection<int,object>
	// System.Collections.Generic.Dictionary.KeyCollection<object,object>
	// System.Collections.Generic.Dictionary.ValueCollection.Enumerator<int,object>
	// System.Collections.Generic.Dictionary.ValueCollection.Enumerator<object,object>
	// System.Collections.Generic.Dictionary.ValueCollection<int,object>
	// System.Collections.Generic.Dictionary.ValueCollection<object,object>
	// System.Collections.Generic.Dictionary<int,object>
	// System.Collections.Generic.Dictionary<object,object>
	// System.Collections.Generic.EqualityComparer<int>
	// System.Collections.Generic.EqualityComparer<object>
	// System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<int,object>>
	// System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<object,object>>
	// System.Collections.Generic.ICollection<object>
	// System.Collections.Generic.IComparer<object>
	// System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<int,object>>
	// System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<object,object>>
	// System.Collections.Generic.IEnumerable<object>
	// System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<int,object>>
	// System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<object,object>>
	// System.Collections.Generic.IEnumerator<object>
	// System.Collections.Generic.IEqualityComparer<int>
	// System.Collections.Generic.IEqualityComparer<object>
	// System.Collections.Generic.IList<object>
	// System.Collections.Generic.KeyValuePair<int,object>
	// System.Collections.Generic.KeyValuePair<object,object>
	// System.Collections.Generic.List.Enumerator<object>
	// System.Collections.Generic.List<object>
	// System.Collections.Generic.ObjectComparer<object>
	// System.Collections.Generic.ObjectEqualityComparer<int>
	// System.Collections.Generic.ObjectEqualityComparer<object>
	// System.Collections.Generic.Queue.Enumerator<object>
	// System.Collections.Generic.Queue<object>
	// System.Collections.ObjectModel.ReadOnlyCollection<object>
	// System.Comparison<object>
	// System.Func<int>
	// System.Predicate<object>
	// System.Runtime.CompilerServices.ConditionalWeakTable.CreateValueCallback<object,object>
	// System.Runtime.CompilerServices.ConditionalWeakTable.Enumerator<object,object>
	// System.Runtime.CompilerServices.ConditionalWeakTable<object,object>
	// }}

	public void RefMethods()
	{
		// System.Void Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskMethodBuilder.AwaitUnsafeOnCompleted<Cysharp.Threading.Tasks.UniTask.Awaiter,object>(Cysharp.Threading.Tasks.UniTask.Awaiter&,object&)
		// System.Void Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskMethodBuilder.AwaitUnsafeOnCompleted<Cysharp.Threading.Tasks.YieldAwaitable.Awaiter,object>(Cysharp.Threading.Tasks.YieldAwaitable.Awaiter&,object&)
		// System.Void Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskMethodBuilder.Start<object>(object&)
		// object ProtoBuf.Meta.TypeModel.ActivatorCreate<object>()
		// object ProtoBuf.Meta.TypeModel.CreateInstance<object>(ProtoBuf.ISerializationContext,ProtoBuf.Serializers.ISerializer<object>)
		// ProtoBuf.Serializers.ISerializer<object> ProtoBuf.Meta.TypeModel.GetSerializer<object>()
		// ProtoBuf.Serializers.ISerializer<object> ProtoBuf.Meta.TypeModel.GetSerializer<object>(ProtoBuf.Meta.TypeModel,ProtoBuf.CompatibilityLevel)
		// ProtoBuf.Serializers.ISerializer<object> ProtoBuf.Meta.TypeModel.GetSerializerCore<object>(ProtoBuf.CompatibilityLevel)
		// ProtoBuf.Serializers.ISerializer<object> ProtoBuf.Meta.TypeModel.NoSerializer<object>(ProtoBuf.Meta.TypeModel)
		// ProtoBuf.Serializers.ISerializer<object> ProtoBuf.Meta.TypeModel.TryGetSerializer<object>(ProtoBuf.Meta.TypeModel)
		// object ProtoBuf.ProtoReader.State.<ReadAsRoot>g__ReadFieldOne|110_0<object>(ProtoBuf.ProtoReader.State&,ProtoBuf.Serializers.SerializerFeatures,object,ProtoBuf.Serializers.ISerializer<object>)
		// object ProtoBuf.ProtoReader.State.CreateInstance<object>(ProtoBuf.Serializers.ISerializer<object>)
		// object ProtoBuf.ProtoReader.State.DeserializeRoot<object>(object,ProtoBuf.Serializers.ISerializer<object>)
		// object ProtoBuf.ProtoReader.State.DeserializeRootImpl<object>(object)
		// object ProtoBuf.ProtoReader.State.ReadAny<object>(ProtoBuf.Serializers.SerializerFeatures,object,ProtoBuf.Serializers.ISerializer<object>)
		// object ProtoBuf.ProtoReader.State.ReadAsRoot<object>(object,ProtoBuf.Serializers.ISerializer<object>)
		// object ProtoBuf.ProtoReader.State.ReadMessage<object,object>(ProtoBuf.Serializers.SerializerFeatures,object,object&)
		// object ProtoBuf.ProtoReader.State.ReadMessage<object>(ProtoBuf.Serializers.SerializerFeatures,object,ProtoBuf.Serializers.ISerializer<object>)
		// object ProtoBuf.ProtoReader.State.ReadWrapped<object>(ProtoBuf.Serializers.SerializerFeatures,object,ProtoBuf.Serializers.ISerializer<object>)
		// object ProtoBuf.Serializer.Deserialize<object>(System.IO.Stream)
		// object[] System.Array.Empty<object>()
		// int System.Array.FindIndex<object>(object[],System.Predicate<object>)
		// int System.Array.FindIndex<object>(object[],int,int,System.Predicate<object>)
		// object& System.Runtime.CompilerServices.Unsafe.As<object,object>(object&)
		// System.Void* System.Runtime.CompilerServices.Unsafe.AsPointer<object>(object&)
		// object System.Threading.Interlocked.CompareExchange<object>(object&,object,object)
		// object UnityEngine.Component.GetComponentInParent<object>()
		// object UnityEngine.GameObject.GetComponent<object>()
		// object[] UnityEngine.GameObject.GetComponentsInChildren<object>(bool)
	}
}