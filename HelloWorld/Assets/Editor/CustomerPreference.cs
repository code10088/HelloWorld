using UnityEngine;
using UnityEditor;
using System;
using UnityEditorInternal;

public enum CustomerPreferenceEnum
{
    BuildPlayerPath,
    LubanPath,
    MsgPath,
    CosBucketConfig,
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
                LubanPathGUI();
                MsgPathGUI();
                CosBucketConfigGUI();
            }
        };
    }
    public static T GetConfig<T>(CustomerPreferenceEnum c)
    {
        if (data == null) Init();
        object result = typeof(CustomerPreferenceData).GetField(c.ToString()).GetValue(data);
        if (result == null)
        {
            EditorUtility.DisplayDialog("Error", "��Ҫ����{c}��Preference->Customer", "Ok");
            throw new Exception($"��Ҫ����{c}��Preference->Customer");
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
            GUILayout.Label("BuildPlayer·��", GUILayout.Width(100f));
            GUILayout.TextField(data.BuildPlayerPath);
            if (GUILayout.Button("���", GUILayout.Width(50f)))
            {
                data.BuildPlayerPath = EditorUtility.OpenFolderPanel("ѡ���ļ�", Environment.CurrentDirectory, "Build");
                Save();
            }
        }
        GUILayout.EndHorizontal();
    }
    static void LubanPathGUI()
    {
        GUILayout.BeginHorizontal();
        {
            GUILayout.Label("Luban·��", GUILayout.Width(100f));
            GUILayout.TextField(data.LubanPath);
            if (GUILayout.Button("���", GUILayout.Width(50f)))
            {
                data.LubanPath = EditorUtility.OpenFolderPanel("ѡ���ļ���", Environment.CurrentDirectory, "Luban");
                Save();
            }
        }
        GUILayout.EndHorizontal();
    }
    static void MsgPathGUI()
    {
        GUILayout.BeginHorizontal();
        {
            GUILayout.Label("Msg·��", GUILayout.Width(100f));
            GUILayout.TextField(data.MsgPath);
            if (GUILayout.Button("���", GUILayout.Width(50f)))
            {
                data.MsgPath = EditorUtility.OpenFolderPanel("ѡ���ļ���", Environment.CurrentDirectory, "NetMsg");
                Save();
            }
        }
        GUILayout.EndHorizontal();
    }
    static void CosBucketConfigGUI()
    {
        GUILayout.BeginHorizontal();
        {
            GUILayout.Label("��Ѷ����Ͱ����", GUILayout.Width(100f));
            var temp = GUILayout.TextField(data.CosBucketConfig.Name);
            if (temp != data.CosBucketConfig.Name)
            {
                data.CosBucketConfig.Name = temp;
                Save();
            }
            temp = GUILayout.TextField(data.CosBucketConfig.SecretId);
            if (temp != data.CosBucketConfig.SecretId)
            {
                data.CosBucketConfig.SecretId = temp;
                Save();
            }
            temp = GUILayout.TextField(data.CosBucketConfig.SecretKey);
            if (temp != data.CosBucketConfig.SecretKey)
            {
                data.CosBucketConfig.SecretKey = temp;
                Save();
            }
        }
        GUILayout.EndHorizontal();
    }
}
