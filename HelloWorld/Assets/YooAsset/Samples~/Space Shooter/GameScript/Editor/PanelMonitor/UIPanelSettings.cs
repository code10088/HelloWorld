using UnityEngine;
using UnityEditor;

public static class UIPanelSettings
{
    /// <summary>
    /// 面板文件夹GUID
    /// </summary>
    private const string UIPanelDirectoryGUID = "12d33f33f3a55224c9c747d7bffa1c68";

    /// <summary>
    /// 精灵文件夹GUID
    /// </summary>
    private const string UISpriteDirectoryGUID = "935d7f20c085cc141a3daf9cacfabfae";

    /// <summary>
    /// 图集文件夹GUID
    /// </summary>
    private const string UIAtlasDirectoryGUID = "c355c783476322b4cacac98c5e1b46d8";


    public static string GetPanelDirecotry()
    {
        string result = AssetDatabase.GUIDToAssetPath(UIPanelDirectoryGUID);
        if (string.IsNullOrEmpty(result))
        {
            throw new System.Exception($"Can not found panel direcotry : {UIPanelDirectoryGUID}");
        }
        return result;
    }
    public static string GetSpriteDirecotry()
    {
        string result = AssetDatabase.GUIDToAssetPath(UISpriteDirectoryGUID);
        if (string.IsNullOrEmpty(result))
        {
            throw new System.Exception($"Can not found sprite direcotry : {UISpriteDirectoryGUID}");
        }
        return result;
    }
    public static string GetAtlasDirecotry()
    {
        string result = AssetDatabase.GUIDToAssetPath(UIAtlasDirectoryGUID);
        if (string.IsNullOrEmpty(result))
        {
            throw new System.Exception($"Can not found atlas direcotry : {UIAtlasDirectoryGUID}");
        }
        return result;
    }
}