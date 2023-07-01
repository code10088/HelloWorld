using UnityEditor;
using UnityEngine;

public class LubanEditor
{
    [MenuItem("Tools/CopyConfig")]
    public static void CopyConfig()
    {
        string path = System.Environment.CurrentDirectory.Substring(0, System.Environment.CurrentDirectory.LastIndexOf(@"\"));
        FileUtil.ReplaceDirectory($"{path}\\Luban\\Client\\OutCodes", $"{Application.dataPath}\\Scripts\\SubScripts\\Config\\Auto");
        FileUtil.ReplaceDirectory($"{path}\\Luban\\Client\\OutBytes", $"{Application.dataPath}\\ZRes\\DataConfig");
        AssetDatabase.Refresh();
    }
}
