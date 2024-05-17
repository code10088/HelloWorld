using System;
using UnityEngine;
using YooAsset;

public class QueryServices : IBuildinQueryServices
{
    private string[] builtin;

    public void Init()
    {
        TextAsset ta = Resources.Load<TextAsset>("BuildinFile");
        builtin = ta.text.Split('\n', StringSplitOptions.RemoveEmptyEntries);
    }
    public bool Query(string packageName, string fileName, string fileCRC)
    {
        if (builtin == null) Init();
        return Array.Exists(builtin, a => a == fileName);
    }
}
