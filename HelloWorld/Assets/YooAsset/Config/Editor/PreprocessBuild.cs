using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

public class PreprocessBuild : IPreprocessBuildWithReport
{
    public int callbackOrder => 0;
    public void OnPreprocessBuild(BuildReport report)
    {
        BuiltinFileList list = new BuiltinFileList();
        list.BuiltinFiles = new List<BuiltinFileItem>();
        string path = $"{Application.streamingAssetsPath}/yoo";
        List<FileInfo> fileList = new List<FileInfo>();
        FileUtils.GetAllFilePath(path, fileList);
        foreach (var fileInfo in fileList)
        {
            if (fileInfo.Extension == ".meta")
                continue;
            if (fileInfo.Name.StartsWith("PackageManifest_"))
                continue;

            BuiltinFileItem temp = new BuiltinFileItem();
            temp.PackageName = fileInfo.Directory.Name;
            temp.FileCRC32 = YooAsset.Editor.EditorTools.GetFileCRC32(fileInfo.FullName);
            temp.FileName = fileInfo.Name;
            list.BuiltinFiles.Add(temp);
        }
        string saveFilePath = $"{Application.dataPath}/YooAsset/Config/Resources/BuildinFile.txt";
        string json = JsonConvert.SerializeObject(list);
        StreamWriter fs = new StreamWriter(saveFilePath, false, Encoding.UTF8);
        fs.Write(json);
        fs.Dispose();
        AssetDatabase.Refresh();
    }
}
