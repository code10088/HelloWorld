using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.U2D;

public static class UIPanelModifier
{
    /// <summary>
    /// 刷新面板清单
    /// </summary>
    public static void Refresh(PanelManifest manifest)
    {
        CacheReferenceAtals(manifest);
    }

    /// <summary>
    /// 更新组件
    /// </summary>
    private static void CacheReferenceAtals(PanelManifest manifest)
    {
        manifest.ReferencesAtlas.Clear();

        string spriteDirectory = UIPanelSettings.GetSpriteDirecotry();
        string altasDirectory = UIPanelSettings.GetAtlasDirecotry();

        // 获取依赖的图集名称
        Transform root = manifest.transform;
        Image[] allImage = root.GetComponentsInChildren<Image>(true);
        for (int i = 0; i < allImage.Length; i++)
        {
            Image image = allImage[i];
            if (image.sprite == null)
                continue;

            // 文件路径
            string spriteAssetPath = UnityEditor.AssetDatabase.GetAssetPath(image.sprite);

            // 跳过系统内置资源
            if (spriteAssetPath.Contains("_builtin_"))
                continue;

            // 跳过非图集精灵
            if (spriteAssetPath.StartsWith(spriteDirectory) == false)
                continue;

            string atlasAssetPath = GetAtlasPath(altasDirectory, spriteAssetPath);
            SpriteAtlas spriteAtlas = UnityEditor.AssetDatabase.LoadAssetAtPath<SpriteAtlas>(atlasAssetPath);
            if (spriteAtlas == null)
            {
                throw new System.Exception($"Not found SpriteAtlas : {atlasAssetPath}");
            }
            else
            {
                if (manifest.ReferencesAtlas.Contains(spriteAtlas) == false)
                    manifest.ReferencesAtlas.Add(spriteAtlas);
            }
        }
    }

    /// <summary>
    /// 获取精灵所属图集
    /// </summary>
    private static string GetAtlasPath(string atlasDirectory, string assetPath)
    {
        string directory = Path.GetDirectoryName(assetPath);
        DirectoryInfo directoryInfo = new DirectoryInfo(directory);
        string atlasName = directoryInfo.Name;
        return $"{atlasDirectory}/{atlasName}.spriteatlas";
    }
}