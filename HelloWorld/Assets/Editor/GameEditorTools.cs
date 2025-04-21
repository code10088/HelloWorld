using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using Unity.Collections;
using UnityEditor.Build;
using System;

public class GameEditorTools
{
    [MenuItem("Tools/CopyConfig", false, (int)ToolsMenuSort.CopyConfig)]
    public static void CopyConfig()
    {
        string path = CustomerPreference.GetConfig<string>(CustomerPreferenceEnum.LubanPath);
        Process process = new Process();
        ProcessStartInfo startInfo = process.StartInfo;
        startInfo.FileName = $"{path}/gen.bat";
        startInfo.WorkingDirectory = path;
        startInfo.CreateNoWindow = true;
        startInfo.UseShellExecute = false;
        startInfo.RedirectStandardOutput = true;
        startInfo.RedirectStandardError = true;
        process.StartInfo = startInfo;
        process.Start();
        process.WaitForExit();
        string log = process.StandardOutput.ReadToEnd();
        process.Close();
        if (log.Contains("bye~")) UnityEngine.Debug.Log("bye~");
        else UnityEngine.Debug.LogError(log);
        List<FileInfo> list = new List<FileInfo>();
        FileUtils.GetAllFilePath($"{Application.dataPath}/Scripts/SubScripts/Config/Auto", list);
        for (int i = 0; i < list.Count; i++) File.Delete(list[i].FullName);
        list.Clear();
        FileUtils.GetAllFilePath($"{path}/Client/OutCodes", list);
        for (int i = 0; i < list.Count; i++)
        {
            string target = $"{Application.dataPath}/Scripts/SubScripts/Config/Auto/{list[i].Name}";
            File.Copy(list[i].FullName, target, true);
        }
        list.Clear();

        FileUtils.GetAllFilePath($"{Application.dataPath}/ZRes/DataConfig", list);
        for (int i = 0; i < list.Count; i++) File.Delete(list[i].FullName);
        list.Clear();
        FileUtils.GetAllFilePath($"{path}/Client/OutBytes", list);
        for (int i = 0; i < list.Count; i++)
        {
            string target = $"{Application.dataPath}/ZRes/DataConfig/{list[i].Name}";
            File.Copy(list[i].FullName, target, true);
        }
        AssetDatabase.Refresh();
    }
    [MenuItem("Tools/CopyMsg", false, (int)ToolsMenuSort.CopyMsg)]
    public static void CopyMsg()
    {
        string path = CustomerPreference.GetConfig<string>(CustomerPreferenceEnum.MsgPath);
        Process process = new Process();
        ProcessStartInfo startInfo = process.StartInfo;
        startInfo.FileName = $"{path}/Net/gen.bat";
        startInfo.WorkingDirectory = path;
        startInfo.CreateNoWindow = true;
        startInfo.UseShellExecute = false;
        startInfo.RedirectStandardOutput = true;
        startInfo.RedirectStandardError = true;
        process.StartInfo = startInfo;
        process.Start();
        process.WaitForExit();
        string log = process.StandardOutput.ReadToEnd();
        process.Close();
        if (log.Contains("finish")) UnityEngine.Debug.Log("finish");
        else UnityEngine.Debug.LogError(log);
        List<FileInfo> list = new List<FileInfo>();
        FileUtils.GetAllFilePath($"{Application.dataPath}/Scripts/SubScripts/Network/Message", list);
        for (int i = 0; i < list.Count; i++) File.Delete(list[i].FullName);
        list.Clear();
        FileUtils.GetAllFilePath($"{path}/Net/OutCodes", list);
        for (int i = 0; i < list.Count; i++)
        {
            string target = $"{Application.dataPath}/Scripts/SubScripts/Network/Message/{list[i].Name}";
            File.Copy(list[i].FullName, target, true);
        }

        string str1 = string.Empty;
        string str2 = string.Empty;
        string[] lines = File.ReadAllLines($"{path}/Proto/AConfig.txt");
        for (int i = 0; i < lines.Length; i++)
        {
            string temp = lines[i];
            if (temp.StartsWith("//")) continue;
            var temps = temp.Split('=', StringSplitOptions.RemoveEmptyEntries);
            temp = temps[1].Replace('.', '_');
            str1 += WriteLine(1, $"public const ushort {temp} = {temps[0]};");
            if (int.Parse(temps[0]) > 10000) str2 += WriteLine(4, $"case NetMsgId.{temp}: msg = Serializer.Deserialize<{temps[1]}>(mm); break;");
        }
        string result = string.Empty;
        result += WriteLine(0, "using ProtoBuf;");
        result += WriteLine(0, "using System;");
        result += WriteLine(0, "public class NetMsgId");
        result += WriteLine(0, "{");
        result += str1;
        result += WriteLine(0, "}");
        result += WriteLine(0, "public partial class NetMsgDispatch");
        result += WriteLine(0, "{");
        result += WriteLine(1, "public bool Deserialize(byte[] bytes)");
        result += WriteLine(1, "{");
        result += WriteLine(2, "try");
        result += WriteLine(2, "{");
        result += WriteLine(3, "var id = BitConverter.ToUInt16(bytes, 0);");
        result += WriteLine(3, "var mm = new Memory<byte>(bytes, 2, bytes.Length - 2);");
        result += WriteLine(3, "IExtensible msg = null;");
        result += WriteLine(3, "switch (id)");
        result += WriteLine(3, "{");
        result += str2;
        result += WriteLine(3, "}");
        result += WriteLine(3, "Add(id, msg);");
        result += WriteLine(3, "return true;");
        result += WriteLine(2, "}");
        result += WriteLine(2, "catch (Exception e)");
        result += WriteLine(2, "{");
        result += WriteLine(3, "GameDebug.LogError(e.Message);");
        result += WriteLine(3, "return false;");
        result += WriteLine(2, "}");
        result += WriteLine(1, "}");
        result += WriteLine(0, "}");
        File.WriteAllText($"{Application.dataPath}/Scripts/SubScripts/Network/NetMsgDeserialize.cs", result);
        AssetDatabase.Refresh();
    }

    public static string WriteLine(int tabCount, string str)
    {
        string result = string.Empty;
        for (int i = 0; i < tabCount; i++) result += "    ";
        result += str;
        result += "\n";
        return result;
    }

    [MenuItem("Tools/Editor/CopyAutoIndex &d")]
    public static void Copy()
    {
        foreach (Transform t in Selection.GetTransforms(SelectionMode.TopLevel | SelectionMode.ExcludePrefab | SelectionMode.Editable))
        {
            var parent = t.parent;
            if (parent == null) continue;
            GameObject newObject;
            PrefabAssetType pt = PrefabUtility.GetPrefabAssetType(t.gameObject);
            if (pt == PrefabAssetType.NotAPrefab)
            {
                newObject = UnityEngine.Object.Instantiate(t.gameObject, parent);
            }
            else
            {
                UnityEngine.Object prefab = PrefabUtility.GetCorrespondingObjectFromSource(t.gameObject);
                newObject = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
                PropertyModification[] overrides = PrefabUtility.GetPropertyModifications(t.gameObject);
                PrefabUtility.SetPropertyModifications(newObject, overrides);
            }
            var trimChars = new[] { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9' };
            string str = t.name.TrimEnd(trimChars);
            int index = 1, sibling = 0;
            var child = parent.Find(str + index);
            while (child)
            {
                sibling = child.GetSiblingIndex();
                index++;
                child = parent.Find(str + index);
            }
            newObject.name = str + index;
            newObject.transform.position = t.position;
            newObject.transform.rotation = t.rotation;
            newObject.transform.SetSiblingIndex(sibling + 1);
            Undo.RegisterCreatedObjectUndo(newObject, "CopyAutoIndex");
        }
    }
    [MenuItem("Tools/Editor/EditorFastForward %RIGHT")]
    public static void FastForward()
    {
        Time.timeScale = Time.timeScale * 2;
    }
    [MenuItem("Tools/Editor/EditorFastRewind %LEFT")]
    public static void FastRewind()
    {
        Time.timeScale = Time.timeScale / 2;
    }
    [MenuItem("Tools/Editor/EditorFastNormal %DOWN")]
    public static void FastNormal()
    {
        Time.timeScale = 1;
    }
    public static void AddScriptingDefineSymbols(string str)
    {
        var buildTargetGroup = EditorUserBuildSettings.selectedBuildTargetGroup;
        var namedBuildTarget = NamedBuildTarget.FromBuildTargetGroup(buildTargetGroup);
        var symbols = PlayerSettings.GetScriptingDefineSymbols(namedBuildTarget);
        if (string.IsNullOrEmpty(symbols)) PlayerSettings.SetScriptingDefineSymbols(namedBuildTarget, str);
        else if (!symbols.Contains(str)) PlayerSettings.SetScriptingDefineSymbols(namedBuildTarget, $"{symbols};{str}");
        AssetDatabase.Refresh();
    }
    public static void RemoveScriptingDefineSymbols(string str)
    {
        var buildTargetGroup = EditorUserBuildSettings.selectedBuildTargetGroup;
        var namedBuildTarget = NamedBuildTarget.FromBuildTargetGroup(buildTargetGroup);
        var symbols = PlayerSettings.GetScriptingDefineSymbols(namedBuildTarget);
        symbols = symbols.Replace(str, string.Empty);
        PlayerSettings.SetScriptingDefineSymbols(namedBuildTarget, symbols);
        AssetDatabase.Refresh();
    }

    [MenuItem("Tools/Editor/内存泄漏检测/启用")]
    static void LeakDetectionEnabled()
    {
        NativeLeakDetection.Mode = NativeLeakDetectionMode.Enabled;
    }
    [MenuItem("Tools/Editor/内存泄漏检测/启用", true)]
    static bool ValidateLeakDetectionEnabled()
    {
        return NativeLeakDetection.Mode != NativeLeakDetectionMode.Enabled;
    }
    [MenuItem("Tools/Editor/内存泄漏检测/禁用")]
    static void LeakDetectionDisable()
    {
        NativeLeakDetection.Mode = NativeLeakDetectionMode.Disabled;
    }
    [MenuItem("Tools/Editor/内存泄漏检测/禁用", true)]
    static bool ValidateLeakDetectionDisable()
    {
        return NativeLeakDetection.Mode != NativeLeakDetectionMode.Disabled;
    }
    [MenuItem("Tools/Editor/内存泄漏检测/启用堆栈跟踪")]
    static void LeakDetectionEnabledWithStackTrace()
    {
        NativeLeakDetection.Mode = NativeLeakDetectionMode.EnabledWithStackTrace;
    }
    [MenuItem("Tools/Editor/内存泄漏检测/启用堆栈跟踪", true)]
    static bool ValidateLeakDetectionEnabledWithStackTrace()
    {
        return NativeLeakDetection.Mode != NativeLeakDetectionMode.EnabledWithStackTrace;
    }
}
