/// <summary>
/// DupWithoutRename is a utility class for Unity 5 that mimics the Unity 4 behavior of Ctrl+D (or Cmd+D on Mac): objects are duplicated 
/// without being renamed. In Unity 5 they are renamed automatically, and it sucks. Place this file in Assets/Editor so that it is 
/// detected as an editor script.
/// 
/// Note that unlike the real Ctrl+D command, this one only duplicates objects in the scene hierarchy.
/// It can't duplicate other stuff, like assets in your Project database, or array entries in the editor's stock array manager GUI. 
/// For this reason, it's mapped to Ctrl+Shift+D, so you still have access to Ctrl+D for those other tasks.
/// 
/// v1.01. 
/// Code by Eric Heimburg. 
/// Public domain. This is free and unencumbered software released into the public domain. See unlicense.org for details.
/// </summary>

// Known issue!!! While this function will duplicate selected prefabs correctly, if you duplicate a NON-prefab game object that has child objects
// that ARE prefabs, the duplicated children won't be correctly marked as prefabs. This scenario doesn't seem to occur in practice for me,
// which is good because I don't have time to try to fix it at the moment. If somebody fixes it, though, please email me! eric@heimburg.com

using UnityEngine;
using UnityEditor;

public class DupWithoutRename 
{
	[MenuItem("GameObject/Duplicate Without Renaming %#d")]
	public static void DuplicateWithoutRenaming()
	{
		foreach (Transform t in Selection.GetTransforms(SelectionMode.TopLevel | SelectionMode.ExcludePrefab | SelectionMode.Editable))
		{
			GameObject newObject = null;

			PrefabType pt = PrefabUtility.GetPrefabType(t.gameObject);
			if (pt == PrefabType.PrefabInstance || pt == PrefabType.ModelPrefabInstance)
			{
				// it's an instance of a prefab! Create a new instance of the same prefab!
				Object prefab = PrefabUtility.GetPrefabParent(t.gameObject);
				newObject = (GameObject)PrefabUtility.InstantiatePrefab(prefab);

				// we've got a brand new prefab instance, but it doesn't have the same overrides as our original. Fix that...
				PropertyModification[] overrides = PrefabUtility.GetPropertyModifications(t.gameObject);
				PrefabUtility.SetPropertyModifications(newObject, overrides);
			}
			else
			{
				// not a prefab... so just instantiate it!
				newObject = Object.Instantiate(t.gameObject);
				newObject.name = t.name;
			}

			// okay, prefab is instantiated (or if it's not a prefab, we've just cloned it in the scene)
			// Make sure it's got the same parent and position as the original!
			newObject.transform.parent = t.parent;
			newObject.transform.position = t.position;
			newObject.transform.rotation = t.rotation;

			// tell the Undo system we made this, so you can undo it if you did it by mistake
			Undo.RegisterCreatedObjectUndo(newObject, "Duplicate Without Renaming");
		}
	}
	
	
	[MenuItem("GameObject/Duplicate Without Renaming %#d", true)]
	public static bool ValidateDuplicateWithoutRenaming()
	{
		return Selection.GetFiltered(typeof(GameObject), SelectionMode.TopLevel | SelectionMode.ExcludePrefab | SelectionMode.Editable).Length > 0;
	}

}
