using UnityEditor;
using UnityEditor.UI;

[CustomEditor(typeof(UIButton), true)]
[CanEditMultipleObjects]
public class UIButtonEditor : ButtonEditor
{
    SerializedProperty m_SoundPathProperty;

    protected override void OnEnable()
    {
        base.OnEnable();
        m_SoundPathProperty = serializedObject.FindProperty("soundPath");
    }

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        EditorGUILayout.Space();

        serializedObject.Update();
        EditorGUILayout.PropertyField(m_SoundPathProperty);
        serializedObject.ApplyModifiedProperties();
    }
}
