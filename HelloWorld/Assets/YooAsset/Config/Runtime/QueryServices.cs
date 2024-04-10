using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using YooAsset;

public class QueryServices : IBuildinQueryServices
{
    private Dictionary<string, BuiltinFileItem> builtin = new Dictionary<string, BuiltinFileItem>();
    private bool init = false;

    public bool Query(string packageName, string fileName, string fileCRC)
    {
#if UNITY_EDITOR && !HotUpdateDebug
        string filePath = $"{Application.streamingAssetsPath}/yoo/{packageName}/{fileName}";
        return File.Exists(filePath);
#else
        if (!init)
        {
            init = true;
            TextAsset ta = Resources.Load<TextAsset>("BuildinFile");
            var list = JsonConvert.DeserializeObject<BuiltinFileList>(ta.text);
            for (int i = 0; i < list.BuiltinFiles.Count; i++)
            {
                var item = list.BuiltinFiles[i];
                builtin[item.FileName] = item;
            }
        }
        if (builtin.TryGetValue(fileName, out var temp))
        {
            return temp.FileCRC32 == fileCRC;
        }
        return false;
#endif
    }
}
