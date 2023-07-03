using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class CustomerPreference
{
    public static string MSBuildPath = "MSBuildPath";

    private static bool bInit = false;
    private static string _MSBuildPath;

    static void Init()
    {
        bInit = true;
        _MSBuildPath = EditorPrefs.GetString(MSBuildPath);
    }
    [SettingsProvider]
    static SettingsProvider OnGUI()
    {
        if (!bInit) Init();
        return new SettingsProvider("Preferences/Customer", SettingsScope.User)
        {
            guiHandler = (searchContext) =>
            {
                MSBuildPathGUI();
            }
        };
    }

    static void MSBuildPathGUI()
    {
        GUILayout.BeginHorizontal();
        {
            GUILayout.Label("MSBuild路径", GUILayout.Width(50f));
            GUILayout.TextField(_MSBuildPath);
            if (GUILayout.Button("浏览", GUILayout.Width(50f)))
            {
                _MSBuildPath = EditorUtility.OpenFilePanel("选择文件", Application.dataPath, "exe");
                EditorPrefs.SetString(MSBuildPath, _MSBuildPath);
            }
        }
        GUILayout.EndHorizontal();
    }
}
