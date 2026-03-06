using Scriban;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ComponentMark), true)]
public class ComponentMarkEditor : Editor
{
    private static bool refresh = true;
    [UnityEditor.Callbacks.DidReloadScripts]
    private static void Reload()
    {
        refresh = true;
    }
    public override void OnInspectorGUI()
    {
        GUI.backgroundColor = Color.green;
        if (GUILayout.Button("Refresh", GUILayout.Height(30)))
        {
            string result = ExportScript();
            if (string.IsNullOrEmpty(result)) AssetDatabase.Refresh();
            else EditorUtility.DisplayDialog("Fail", result, "OK");
        }
        if (refresh)
        {
            refresh = false;
            Refresh();
        }
        GUI.backgroundColor = Color.white;
        base.OnInspectorGUI();
    }
    private string ExportScript()
    {
        var component = target as ComponentMark;
        var markArray = component.GetComponentsInChildren<MarkComponent>(true);
        var markList = new List<MarkComponent>();
        for (int i = 0; i < markArray.Length; i++)
        {
            var tempMark = markArray[i];
            var selfComp = tempMark.GetComponent<ComponentMark>();
            var parentComp = tempMark.transform.parent.GetComponentInParent<ComponentMark>(true);
            if (selfComp == component || parentComp == component)
            {
                if (markList.Exists(a => a.name == tempMark.name)) return "变量名重复" + component.name;
                else markList.Add(tempMark);
            }
        }
        List<MarkComponentInfo> components = new List<MarkComponentInfo>();
        for (int i = 0; i < markList.Count; i++)
        {
            int counter = 1;
            for (int j = 0; j < markList[i].components.Length; j++)
            {
                var tempMark = markList[i].components[j];
                if (tempMark == null) tempMark = markList[i].gameObject;
                string fieldType = tempMark.GetType().ToString();
                string fieldName = char.ToLower(tempMark.name[0]) + tempMark.name.Substring(1) + fieldType.Substring(fieldType.LastIndexOf('.') + 1);
                if (components.Exists(a => a.field_name == fieldName))
                {
                    fieldName += counter;
                    counter++;
                }
                components.Add(new MarkComponentInfo
                {
                    component_type = fieldType,
                    field_name = fieldName
                });
            }
        }

        var filePath = AssetDatabase.GetAssetPath(MonoScript.FromMonoBehaviour(component));
        string result = File.ReadAllText($"{Application.dataPath}/Editor/Template/ComponentMarkTemplate.txt");
        if (string.IsNullOrEmpty(result)) return "模板不存在";
        try
        {
            var template = Template.Parse(result);
            if (template.HasErrors) return "模板解析错误：" + template.Messages;
            result = template.Render(new { class_name = Path.GetFileNameWithoutExtension(filePath), components });
        }
        catch (System.Exception ex)
        {
            return "模板渲染异常：" + ex.Message;
        }
        File.WriteAllText(filePath, result);
        return string.Empty;
    }
    private void Refresh()
    {
        var component = target as ComponentMark;
        var markArray = component.GetComponentsInChildren<MarkComponent>(true);
        var markList = new List<MarkComponent>();
        for (int i = 0; i < markArray.Length; i++)
        {
            var tempMark = markArray[i];
            var selfComp = tempMark.GetComponent<ComponentMark>();
            var parentComp = tempMark.transform.parent.GetComponentInParent<ComponentMark>(true);
            if (selfComp == component || parentComp == component) markList.Add(tempMark);
        }
        List<Object> components = new List<Object>();
        for (int i = 0; i < markList.Count; i++)
        {
            for (int j = 0; j < markList[i].components.Length; j++)
            {
                var tempMark = markList[i].components[j];
                if (tempMark == null) tempMark = markList[i].gameObject;
                components.Add(tempMark);
            }
        }
        try
        {
            var fields = component.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance);
            for (int i = 0; i < fields.Length; i++)
            {
                fields[i].SetValue(component, components[i]);
            }
        }
        catch
        {

        }
        EditorUtility.SetDirty(component);
    }
    private class MarkComponentInfo
    {
        public string component_type;
        public string field_name;
    }
}