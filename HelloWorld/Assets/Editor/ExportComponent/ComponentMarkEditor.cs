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
    public override void OnInspectorGUI()
    {
        GUI.backgroundColor = Color.green;
        if (GUILayout.Button("Refresh", GUILayout.Height(30)))
        {
            refresh = true;
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
                if (markList.Exists(a => a.name == tempMark.name)) return "变量名重复" + tempMark.name;
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
                if (tempMark == null) continue;
                string fieldType = tempMark.GetType().ToString();
                string fieldName = char.ToLower(tempMark.name[0]) + tempMark.name.Substring(1) + fieldType.Substring(fieldType.LastIndexOf('.') + 1);
                if (components.Exists(a => a.field_name == fieldName))
                {
                    fieldName += counter;
                    counter++;
                }
                if (markList[i].isArray)
                {
                    components.Add(new MarkComponentInfo
                    {
                        component_type = fieldType + "[]",
                        field_name = fieldName
                    });
                    break;
                }
                else
                {
                    components.Add(new MarkComponentInfo
                    {
                        component_type = fieldType,
                        field_name = fieldName
                    });
                }
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
        List<object> components = new List<object>();
        for (int i = 0; i < markList.Count; i++)
        {
            if (markList[i].isArray)
            {
                var elementType = markList[i].components[0].GetType();
                var array = System.Array.CreateInstance(elementType, markList[i].components.Length);
                for (int j = 0; j < markList[i].components.Length; j++)
                {
                    var tempMark = markList[i].components[j];
                    if (tempMark == null) continue;
                    if (tempMark.GetType() == elementType) array.SetValue(tempMark, j);
                }
                components.Add(array);
            }
            else
            {
                for (int j = 0; j < markList[i].components.Length; j++)
                {
                    var tempMark = markList[i].components[j];
                    if (tempMark == null) continue;
                    components.Add(tempMark);
                }
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