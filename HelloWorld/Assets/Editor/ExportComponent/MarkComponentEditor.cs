using SuperScrollView;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

[CustomEditor(typeof(MarkComponent), true)]
[CanEditMultipleObjects]
public class MarkComponentEditor : Editor
{
    public override void OnInspectorGUI()
    {
        GUI.backgroundColor = Color.green;
        if (GUILayout.Button("Refresh", GUILayout.Height(30)))
        {
            var component = target as MarkComponent;
            var components = component.components = new Object[1];
            if (components[0] == null) components[0] = component.GetComponent<LoopListView2>();
            if (components[0] == null) components[0] = component.GetComponent<LoopGridView>();
            if (components[0] == null) components[0] = component.GetComponent<Button>();
            if (components[0] == null) components[0] = component.GetComponent<Slider>();
            if (components[0] == null) components[0] = component.GetComponent<Toggle>();
            if (components[0] == null) components[0] = component.GetComponent<Image>();
            if (components[0] == null) components[0] = component.GetComponent<RawImage>();
            if (components[0] == null) components[0] = component.GetComponent<TextMeshProUGUI>();
        }
        GUI.backgroundColor = Color.white;
        base.OnInspectorGUI();
    }


    private static Texture2D texture;
    [ExecuteInEditMode, InitializeOnLoadMethod]
    public static void Init()
    {
        if (texture == null) texture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Editor/ExportComponent/1.png");
        EditorApplication.hierarchyWindowItemOnGUI -= DrawItemGUI;
        EditorApplication.hierarchyWindowItemOnGUI += DrawItemGUI;
        EditorApplication.playModeStateChanged -= Hide;
        EditorApplication.playModeStateChanged += Hide;
    }
    private static void Hide(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredEditMode)
        {
            EditorApplication.hierarchyWindowItemOnGUI -= DrawItemGUI;
            EditorApplication.hierarchyWindowItemOnGUI += DrawItemGUI;
        }
        else if (state == PlayModeStateChange.EnteredPlayMode)
        {
            EditorApplication.hierarchyWindowItemOnGUI -= DrawItemGUI;
        }
    }
    private static void DrawItemGUI(int instanceID, Rect selectionRect)
    {
        var obj = EditorUtility.InstanceIDToObject(instanceID) as GameObject;
        if (obj == null) return;
        var ec = obj.GetComponent<MarkComponent>();
        if (ec) GUI.DrawTexture(new Rect(selectionRect.x - (obj.transform.childCount > 0 ? 26 : 13), selectionRect.y, 16, 16), texture);
    }
}