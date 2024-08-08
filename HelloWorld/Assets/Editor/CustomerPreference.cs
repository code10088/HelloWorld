using UnityEngine;
using UnityEditor;
using System;
using UnityEditorInternal;

public enum CustomerPreferenceEnum
{
    BuildPlayerPath,
    MSBuildPath,
    LubanPath,
}
public class CustomerPreference
{
    private static string path = "ProjectSettings/CustomerPreference.asset";
    private static CustomerPreferenceData data;

    static void Init()
    {
        var objs = InternalEditorUtility.LoadSerializedFileAndForget(path);
        if (objs.Length > 0)
        {
            data = (CustomerPreferenceData)objs[0];
        }
        else
        {
            data = new CustomerPreferenceData();
            Save();
        }
    }
    static void Save()
    {
        var obj = new UnityEngine.Object[] { data };
        InternalEditorUtility.SaveToSerializedFileAndForget(obj, path, true);
    }
    [SettingsProvider]
    static SettingsProvider OnGUI()
    {
        if (data == null) Init();
        return new SettingsProvider("Preferences/Customer", SettingsScope.User)
        {
            guiHandler = (searchContext) =>
            {
                BuildPlayerPathGUI();
                MSBuildPathGUI();
                LubanPathGUI();
            }
        };
    }
    public static T GetConfig<T>(CustomerPreferenceEnum c)
    {
        if (data == null) Init();
        object result = typeof(CustomerPreferenceData).GetField(c.ToString()).GetValue(data);
        if (result == null)
        {
            EditorUtility.DisplayDialog("Error", "需要配置{c}：Preference->Customer", "Ok");
            throw new Exception($"需要配置{c}：Preference->Customer");
        }
        else
        {
            return (T)result;
        }
    }

    static void BuildPlayerPathGUI()
    {
        GUILayout.BeginHorizontal();
        {
            GUILayout.Label("BuildPlayer路径", GUILayout.Width(50f));
            GUILayout.TextField(data.BuildPlayerPath);
            if (GUILayout.Button("浏览", GUILayout.Width(50f)))
            {
                data.BuildPlayerPath = EditorUtility.OpenFolderPanel("选择文件", Environment.CurrentDirectory, "Build");
                Save();
            }
        }
        GUILayout.EndHorizontal();
    }
    static void MSBuildPathGUI()
    {
        GUILayout.BeginHorizontal();
        {
            GUILayout.Label("MSBuild路径", GUILayout.Width(50f));
            GUILayout.TextField(data.MSBuildPath);
            if (GUILayout.Button("浏览", GUILayout.Width(50f)))
            {
                data.MSBuildPath = EditorUtility.OpenFilePanel("选择文件", Application.dataPath, "exe");
                Save();
            }
        }
        GUILayout.EndHorizontal();
    }
    static void LubanPathGUI()
    {
        GUILayout.BeginHorizontal();
        {
            GUILayout.Label("Luban路径", GUILayout.Width(50f));
            GUILayout.TextField(data.LubanPath);
            if (GUILayout.Button("浏览", GUILayout.Width(50f)))
            {
                data.LubanPath = EditorUtility.OpenFolderPanel("选择文件夹", Environment.CurrentDirectory, "Luban");
                Save();
            }
        }
        GUILayout.EndHorizontal();
    }
}
