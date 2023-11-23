using UnityEngine;
using UnityEditor;

public class CopyGameObject
{
	[MenuItem("GameObject/Tools/CopyGameObjectAutoIndex &d")]
	public static void Copy()
	{
		foreach (Transform t in Selection.GetTransforms(SelectionMode.TopLevel | SelectionMode.ExcludePrefab | SelectionMode.Editable))
		{
			var parent = t.parent;
			if (parent == null) continue;
			GameObject newObject;
			PrefabAssetType pt = PrefabUtility.GetPrefabAssetType(t.gameObject);
			if (pt == PrefabAssetType.NotAPrefab)
			{
				newObject = Object.Instantiate(t.gameObject, parent);
			}
			else
			{
				Object prefab = PrefabUtility.GetCorrespondingObjectFromSource(t.gameObject);
				newObject = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
				PropertyModification[] overrides = PrefabUtility.GetPropertyModifications(t.gameObject);
				PrefabUtility.SetPropertyModifications(newObject, overrides);
			}
			var trimChars = new[] { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9' };
			string str = t.name.TrimEnd(trimChars);
			int index = 1, sibling = 0;
			var child = parent.Find(str + index);
			while (child)
            {
				sibling = child.GetSiblingIndex();
				index++;
				child = parent.Find(str + index);
			}
			newObject.name = str + index;
			newObject.transform.position = t.position;
			newObject.transform.rotation = t.rotation;
			newObject.transform.SetSiblingIndex(sibling+1);
			Undo.RegisterCreatedObjectUndo(newObject, "CopyGameObjectAutoIndex");
		}
	}
}
