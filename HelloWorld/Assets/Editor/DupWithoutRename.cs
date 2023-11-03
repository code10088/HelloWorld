using UnityEngine;
using UnityEditor;

public class DupWithoutRename 
{
	[MenuItem("GameObject/Tools/CopyNoSuffix &d")]
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
	[MenuItem("GameObject/Tools/Rename &r")]
	public static void Rename()
	{
		var array = Selection.GetTransforms(SelectionMode.TopLevel | SelectionMode.ExcludePrefab | SelectionMode.Editable);
		if (array.Length > 1)
        {
			System.Array.Sort(array, (a, b) => a.GetSiblingIndex() - b.GetSiblingIndex());
			string name = array[0].name;
            for (int i = 0; i < array.Length; i++)
            {
				array[i].name = name + (i + 1);
				Undo.RegisterCompleteObjectUndo(array[i], "Rename");
			}
		}
	}
}
