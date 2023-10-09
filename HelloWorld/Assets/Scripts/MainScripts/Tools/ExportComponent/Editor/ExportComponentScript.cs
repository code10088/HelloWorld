using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;

public class ExportComponentScript
{
    private static string Head1 = "using UnityEngine;\nnamespace HotAssembly\n{\n";
    private static string TabEmpty = "    ";
    private static Texture2D texture;

    [ExecuteInEditMode,InitializeOnLoadMethod]
    public static void Init()
    {
        if (texture == null) texture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Scripts/MainScripts/ExportComponent/Editor/1.png");
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
        if (ec) GUI.DrawTexture(new Rect(selectionRect.x - (obj.transform.childCount > 0 ? 20 : 8), selectionRect.y + 6, 6, 6), texture);
    }

    [MenuItem("GameObject/Tools/ExportScript", false, 1)]
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

    private class ClassInfo
    {
        public string path;
        public string className;
        public List<int> componentIndex;
        public List<ExportComponent> component;
    }

    private static string BeginExportScript()
    {
        GameObject tempObj = Selection.activeGameObject;
        ExportClass[] allClass = tempObj.GetComponentsInChildren<ExportClass>(true);
        if (allClass.Length <= 0) return "没有挂载ExportClass的物体";

        //筛选
        List<ClassInfo> classInfoList = new List<ClassInfo>();
        for (int i = 0; i < allClass.Length; i++)
        {
            ExportClass tempClass = allClass[i];
            int tempIndex = classInfoList.FindIndex(a => a.className == tempClass.className);
            if (tempIndex >= 0) return "类名重复" + tempClass.className;
            ExportComponent[] tempComponentArray = tempClass.GetComponentsInChildren<ExportComponent>(true);
            List<ExportComponent> tempComponentList = new List<ExportComponent>();
            List<int> tempComponentIdList = new List<int>();
            for (int j = 0; j < tempComponentArray.Length; j++)
            {
                ExportComponent tempComponent = tempComponentArray[j];
                ExportClass selfClass = tempComponent.GetComponent<ExportClass>();
                ExportClass tempParentClass = tempComponent.transform.parent.GetComponentInParent<ExportClass>(true);
                if (selfClass == tempClass || tempParentClass == tempClass)
                {
                    ExportComponent b = tempComponentList.Find(a => a.name == tempComponent.name);
                    if (b == null)
                    {
                        tempComponentList.Add(tempComponent);
                        tempComponentIdList.Add(j);
                    }
                    else
                    {
                        return tempClass.className + "类中变量名重复" + tempComponent.name;
                    }
                }
            }
            if (tempComponentList.Count > 0)
            {
                ClassInfo classInfo = new ClassInfo();
                classInfo.path = tempClass.path;
                classInfo.className = tempClass.className;
                classInfo.componentIndex = tempComponentIdList;
                classInfo.component = tempComponentList;
                classInfoList.Add(classInfo);
            }
        }
        if (classInfoList.Count == 0) return "沒有可用的变量名";

        //导出
        if (!Directory.Exists(classInfoList[0].path)) Directory.CreateDirectory(classInfoList[0].path);
        string scriptName = classInfoList[0].path + classInfoList[0].className + ".cs";
        FileStream fs = File.Open(scriptName, FileMode.Create);
        WriteString(fs, 0, Head1);
        for (int i = 0; i < classInfoList.Count; i++)
        {
            ClassInfo ci = classInfoList[i];
            WriteString(fs, 1, "public partial class " + ci.className + "\n");
            WriteString(fs, 1, "{\n");
            WriteString(fs, 2, "public GameObject obj;\n");
            for (int j = 0; j < ci.component.Count; j++)
            {
                ExportComponent tempComponent = ci.component[j];
                Dictionary<string, int> tempNameDic = new Dictionary<string, int>();
                for (int k = 0; k < tempComponent.exportComponent.Length; k++)
                {
                    MonoBehaviour tempBehaviour = tempComponent.exportComponent[k];
                    if (tempBehaviour == null)
                    {
                        string tempName = tempComponent.name + "Obj";
                        if (tempNameDic.ContainsKey(tempName))
                        {
                            tempNameDic[tempName]++;
                            tempName += tempNameDic[tempName];
                        }
                        else
                        {
                            tempNameDic.Add(tempName, 0);
                        }
                        tempName = tempName.Substring(0, 1).ToLower() + tempName.Substring(1);
                        WriteString(fs, 2, "public GameObject " + tempName + " = null;\n");
                    }
                    else
                    {
                        string tempStr = tempBehaviour.GetType().ToString();
                        string tempName = tempStr.Substring(tempStr.LastIndexOf(".") + 1);
                        tempName = tempComponent.name + tempName;
                        if (tempNameDic.ContainsKey(tempName))
                        {
                            tempNameDic[tempName]++;
                            tempName += tempNameDic[tempName];
                        }
                        else
                        {
                            tempNameDic.Add(tempName, 0);
                        }
                        tempName = tempName.Substring(0, 1).ToLower() + tempName.Substring(1);
                        WriteString(fs, 2, "public " + tempStr + " " + tempName + " = null;\n");
                    }
                }
            }
            WriteString(fs, 2, "public void Init(GameObject obj)\n");
            WriteString(fs, 2, "{\n");
            WriteString(fs, 3, "this.obj = obj;\n");
            WriteString(fs, 3, "ExportComponent[] allData = obj.GetComponentsInChildren<ExportComponent>(true);\n");
            for (int j = 0; j < ci.component.Count; j++)
            {
                ExportComponent tempComponent = ci.component[j];
                Dictionary<string, int> tempNameDic = new Dictionary<string, int>();
                for (int k = 0; k < tempComponent.exportComponent.Length; k++)
                {
                    MonoBehaviour tempBehaviour = tempComponent.exportComponent[k];
                    if (tempBehaviour == null)
                    {
                        string tempName = tempComponent.name + "Obj";
                        if (tempNameDic.ContainsKey(tempName))
                        {
                            tempNameDic[tempName]++;
                            tempName += tempNameDic[tempName];
                        }
                        else
                        {
                            tempNameDic.Add(tempName, 0);
                        }
                        tempName = tempName.Substring(0, 1).ToLower() + tempName.Substring(1);
                        WriteString(fs, 3, tempName + " = allData[" + ci.componentIndex[j] + "].gameObject;\n");
                    }
                    else
                    {
                        string tempStr = tempBehaviour.GetType().ToString();
                        string tempName = tempStr.Substring(tempStr.LastIndexOf(".") + 1);
                        tempName = tempComponent.name + tempName;
                        if (tempNameDic.ContainsKey(tempName))
                        {
                            tempNameDic[tempName]++;
                            tempName += tempNameDic[tempName];
                        }
                        else
                        {
                            tempNameDic.Add(tempName, 0);
                        }
                        tempName = tempName.Substring(0, 1).ToLower() + tempName.Substring(1);
                        WriteString(fs, 3, tempName + " = allData[" + ci.componentIndex[j] + "].exportComponent[" + k + "] as " + tempStr + ";\n");
                    }
                }
            }
            WriteString(fs, 2, "}\n");
            WriteString(fs, 1, "}\n");
        }
        WriteString(fs, 0, "}\n");

        fs.Flush();
        fs.Close();
        return string.Empty;
    }
    private static void WriteString(FileStream fs, int tabCount, string str)
    {
        if (fs == null || string.IsNullOrEmpty(str)) return;
        string tab = string.Empty;
        for (int i = 0; i < tabCount; i++)
        {
            tab += TabEmpty;
        }
        tab += str;
        byte[] bytes = Encoding.UTF8.GetBytes(tab);
        fs.Write(bytes, 0, bytes.Length);
    }
}