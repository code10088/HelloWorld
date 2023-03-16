public class AOTGenericReferences : UnityEngine.MonoBehaviour
{

	// {{ constraint implement type
	// }} 

	// {{ AOT generic type
	//MainAssembly.MonoSingletion`1<System.Object>
	//MainAssembly.Singletion`1<System.Object>
	//System.Action`1<System.Object>
	//System.Action`3<System.Int32,System.Object,System.Object>
	//System.Action`3<System.Object,System.Object,System.Object>
	//System.Collections.Generic.Dictionary`2<HotAssembly.EventType,System.Object>
	//System.Collections.Generic.Dictionary`2<System.Int32,System.Int32>
	//System.Collections.Generic.List`1<System.Object>
	//System.Func`1<System.Object>
	//System.Func`3<System.Object,System.Object,System.Object>
	//System.Func`3<System.Object,System.Object,System.Byte>
	//System.Func`4<System.Object,System.Object,System.Object,System.Object>
	//System.Predicate`1<System.Object>
	//System.Runtime.CompilerServices.CallSite`1<System.Object>
	// }}

	public void RefMethods()
	{
		// System.Object[] BytesDecode::ToBDIArray<System.Object>(System.Func`1<System.Object>)
		// System.Void BytesDecode::ToBytes<System.Object>(System.Object[])
		// System.Int32 MainAssembly.AssetManager::Load<System.Object>(System.String,System.Action`3<System.Int32,System.Object,System.Object>,System.Object)
		// System.Object[] System.Array::Empty<System.Object>()
		// System.Object System.Threading.Interlocked::CompareExchange<System.Object>(System.Object&,System.Object,System.Object)
		// System.Object UnityEngine.GameObject::GetComponent<System.Object>()
		// System.Object[] UnityEngine.GameObject::GetComponentsInChildren<System.Object>(System.Boolean)
	}
}