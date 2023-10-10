using UnityEngine;
using UnityEditor;

public class DupWithoutRename 
{
	[MenuItem("GameObject/CopyNoSuffix %#d")]
	public static void CopyNoSuffix()
	{
		foreach (Transform t in Selection.GetTransforms(SelectionMode.TopLevel | SelectionMode.ExcludePrefab | SelectionMode.Editable))
		{
			GameObject newObject;
			PrefabAssetType pt = PrefabUtility.GetPrefabAssetType(t.gameObject);
			if (pt == PrefabAssetType.NotAPrefab)
			{
				newObject = Object.Instantiate(t.gameObject);
				newObject.name = t.name;
			}
			else
			{
				Object prefab = PrefabUtility.GetCorrespondingObjectFromSource(t.gameObject);
				newObject = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
				PropertyModification[] overrides = PrefabUtility.GetPropertyModifications(t.gameObject);
				PrefabUtility.SetPropertyModifications(newObject, overrides);
			}
			newObject.transform.parent = t.parent;
			newObject.transform.position = t.position;
			newObject.transform.rotation = t.rotation;
			Undo.RegisterCreatedObjectUndo(newObject, "CopyNoSuffix");
		}
	}
}
