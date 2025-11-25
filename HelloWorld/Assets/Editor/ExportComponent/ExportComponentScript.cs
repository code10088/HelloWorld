using Scriban;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class ExportComponentScript
{
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
        var ec = obj.GetComponent<ExportComponent>();
        if (ec) GUI.DrawTexture(new Rect(selectionRect.x - (obj.transform.childCount > 0 ? 26 : 13), selectionRect.y, 16, 16), texture);
    }

    [MenuItem("GameObject/Tools/ExportScript", false, -1)]
    public static void ExportScript()
    {
        string result = BeginExportScript();
        if (string.IsNullOrEmpty(result))
        {
            EditorUtility.DisplayDialog("Success", "成功！！！", "OK");
            AssetDatabase.Refresh();
        }
        else
        {
            EditorUtility.DisplayDialog("Fail", result, "OK");
        }
    }

    private static string BeginExportScript()
    {
        GameObject tempObj = Selection.activeGameObject;
        ExportClass[] allClass = tempObj.GetComponentsInChildren<ExportClass>(true);
        if (allClass.Length <= 0) return "没有挂载ExportClass的物体";

        var classInfoList = new List<ClassInfo>();
        foreach (var tempClass in allClass)
        {
            if (classInfoList.Any(a => a.class_name == tempClass.className)) return "类名重复: " + tempClass.className;
            var classInfo = new ClassInfo
            {
                path = tempClass.path,
                class_name = tempClass.className
            };
            var componentArray = tempClass.GetComponentsInChildren<ExportComponent>(true);
            var componentList = new List<ExportComponent>();
            var componentIndex = new List<int>();
            for (int i = 0; i < componentArray.Length; i++)
            {
                var component = componentArray[i];
                var selfClass = component.GetComponent<ExportClass>();
                var parentClass = component.transform.parent.GetComponentInParent<ExportClass>(true);
                if (selfClass == tempClass || parentClass == tempClass)
                {
                    if (componentList.Any(a => a.name == component.name))
                    {
                        return tempClass.className + "类中变量名重复" + component.name;
                    }
                    else
                    {
                        componentList.Add(component);
                        componentIndex.Add(i);
                    }
                }
            }
            for (int i = 0; i < componentList.Count; i++)
            {
                var component = componentList[i];
                var componentInfo = new ComponentInfo
                {
                    index = componentIndex[i],
                    name = component.name
                };
                for (int j = 0; j < component.exportComponent.Length; j++)
                {
                    var export = component.exportComponent[j];
                    string fieldType, fieldName, baseName;
                    if (export == null)
                    {
                        fieldType = "GameObject";
                        baseName = component.name + "Obj";
                    }
                    else
                    {
                        fieldType = export.GetType().ToString();
                        baseName = component.name + fieldType.Substring(fieldType.LastIndexOf('.') + 1);
                    }
                    fieldName = baseName;
                    int counter = 1;
                    while (componentInfo.export_components.Any(a => a.field_name == fieldName))
                    {
                        fieldName = baseName + counter;
                        counter++;
                    }
                    fieldName = char.ToLower(fieldName[0]) + fieldName.Substring(1);
                    componentInfo.export_components.Add(new ExportComponentInfo
                    {
                        component = export,
                        component_type = fieldType,
                        field_name = fieldName,
                        export_index = j
                    });
                }
                classInfo.components.Add(componentInfo);
            }
            classInfoList.Add(classInfo);
        }

        string result = File.ReadAllText($"{Application.dataPath}/Editor/Template/ExportComponentTemplate.txt");
        if (string.IsNullOrEmpty(result)) return "模板不存在";
        try
        {
            var template = Template.Parse(result);
            if (template.HasErrors) return "模板解析错误：" + template.Messages;
            result = template.Render(new { class_info_list = classInfoList });
        }
        catch (System.Exception ex)
        {
            return "模板渲染异常：" + ex.Message;
        }

        if (Directory.Exists(classInfoList[0].path))
        {
            string filePath = Path.Combine(classInfoList[0].path, classInfoList[0].class_name + ".cs");
            File.WriteAllText(filePath, result);
            return string.Empty;
        }
        else
        {
            return "目标文件夹不存在";
        }
    }
    private class ClassInfo
    {
        public string path;
        public string class_name;
        public List<ComponentInfo> components = new List<ComponentInfo>();
    }
    private class ComponentInfo
    {
        public int index;
        public string name;
        public List<ExportComponentInfo> export_components = new List<ExportComponentInfo>();
    }
    private class ExportComponentInfo
    {
        public Component component;
        public string component_type;
        public string field_name;
        public int export_index;
    }
}