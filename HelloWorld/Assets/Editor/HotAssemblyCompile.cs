using System.IO;
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class HotAssemblyCompile
{
    //新建代码工程名
    private static string newProjectName = "HotAssembly";
    //新建代码工程目录
    private static string newProjectPath = System.Environment.CurrentDirectory + "/" + newProjectName;

    [MenuItem("Tools/HotAssemblyCompile", false, (int)ToolsMenuSort.HotAssemblyCompile)]
    public static void Generate()
    {
        StreamReader sr = new StreamReader(newProjectPath + "/" + newProjectName + ".txt");
        string allStr = sr.ReadToEnd();
        sr.Close();

        string strEditorInstallPath = System.AppDomain.CurrentDomain.BaseDirectory.Replace("/", "\\");
        allStr = allStr.Replace("EditorInstallPath", strEditorInstallPath);

        List<FileInfo> fileList = new List<FileInfo>();
        FileUtils.GetAllFilePath(Application.dataPath + "/Scripts/SubScripts", fileList, ".cs");
        string dataPath = (Application.dataPath + "/").Replace("/", "\\");
        string str = "    <Compile Include=\"..\\Assets\\{0}\">\n       <Link>{1}</Link>\n    </Compile>\n";
        string replaceStr = "";
        for (int i = 0; i < fileList.Count; i++)
        {
            if (fileList[i].FullName.Contains("\\Editor\\")) continue;
            string tempStr = fileList[i].FullName.Replace(dataPath, "");
            tempStr = string.Format(str, tempStr, fileList[i].Name);
            replaceStr += tempStr;
        }
        allStr = allStr.Replace("    LinkScripts", replaceStr);

        StreamWriter sw = new StreamWriter(newProjectPath + "/" + newProjectName + ".csproj");
        sw.Write(allStr);
        sw.Close();

        //生成dll
        string strMSBuildPath = EditorPrefs.GetString(CustomerPreference.MSBuildPath);
        if (string.IsNullOrEmpty(strMSBuildPath) || !File.Exists(strMSBuildPath))
        {
            EditorUtility.DisplayDialog("Error", "需要配置MSBuild路径：Preference->Customer->MSBuild路径", "Ok");
        }
        else
        {
            string strParam = newProjectPath + "/" + newProjectName + ".sln /t:Rebuild /p:Configuration=Debug";
            System.Diagnostics.Process msbuild = System.Diagnostics.Process.Start(strMSBuildPath, strParam);
            msbuild.WaitForExit();
            msbuild.Close();
            File.Copy($"{System.Environment.CurrentDirectory}\\HotAssembly\\obj\\Debug\\HotAssembly.dll", $"{Application.dataPath}\\ZRes\\Assembly\\HotAssembly.bytes", true);
            AssetDatabase.Refresh();
        }
    }
}