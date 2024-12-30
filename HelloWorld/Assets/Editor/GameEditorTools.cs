using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;

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
                newObject = Object.Instantiate(t.gameObject, parent);
            }
            else
            {
                Object prefab = PrefabUtility.GetCorrespondingObjectFromSource(t.gameObject);
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
        var symbols = PlayerSettings.GetScriptingDefineSymbolsForGroup(buildTargetGroup);
        if (string.IsNullOrEmpty(symbols)) PlayerSettings.SetScriptingDefineSymbolsForGroup(buildTargetGroup, str);
        else if (!symbols.Contains(str)) PlayerSettings.SetScriptingDefineSymbolsForGroup(buildTargetGroup, $"{symbols};{str}");
        AssetDatabase.Refresh();
    }
    public static void RemoveScriptingDefineSymbols(string str)
    {
        var buildTargetGroup = EditorUserBuildSettings.selectedBuildTargetGroup;
        var symbols = PlayerSettings.GetScriptingDefineSymbolsForGroup(buildTargetGroup);
        symbols = symbols.Replace(str, string.Empty);
        PlayerSettings.SetScriptingDefineSymbolsForGroup(buildTargetGroup, symbols);
        AssetDatabase.Refresh();
    }
}
