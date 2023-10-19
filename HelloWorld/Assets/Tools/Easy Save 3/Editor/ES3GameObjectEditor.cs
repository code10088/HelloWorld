using UnityEditor;
using UnityEngine;

namespace ES3Internal
{
    [CustomEditor(typeof(ES3GameObject))]
    public class ES3GameObjectEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            if (target == null)
                return;

            var es3Go = (ES3GameObject)target;

            EditorGUILayout.HelpBox("This Component allows you to choose which Components are saved when this GameObject is saved using code.", MessageType.Info);

            if (es3Go.GetComponent<ES3AutoSave>() != null)
            {
                EditorGUILayout.HelpBox("This Component cannot be used on GameObjects which are already managed by Auto Save.", MessageType.Error);
                return;
            }

            foreach (var component in es3Go.GetComponents<Component>())
            {
                var markedToBeSaved = es3Go.components.Contains(component);
                var newMarkedToBeSaved = EditorGUILayout.Toggle(component.GetType().Name, markedToBeSaved);

                if(markedToBeSaved && !newMarkedToBeSaved)
                {
                    Undo.RecordObject(es3Go, "Marked Component to save");
                    es3Go.components.Remove(component);
                }

                if (!markedToBeSaved && newMarkedToBeSaved)
                {
                    Undo.RecordObject(es3Go, "Unmarked Component to save");
                    es3Go.components.Add(component);
                }
            }
        }
    }
}