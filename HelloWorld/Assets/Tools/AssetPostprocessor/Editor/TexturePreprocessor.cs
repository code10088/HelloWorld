using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace AssetPreprocessor.Scripts.Editor
{
    class TexturePreprocessor : AssetPostprocessor
    {
        private void OnPreprocessTexture()
        {
            var textureImporter = (TextureImporter)assetImporter;

            var configs = AssetDatabase.FindAssets("t:ScriptableObject")
                .ToList()
                .Select(guid => AssetDatabase.GUIDToAssetPath(guid))
                .Select(path => AssetDatabase.LoadAssetAtPath<TexturePreprocessorConfig>(path))
                .Where(obj => obj)
                .Where(t => t is TexturePreprocessorConfig)
                .Where(c => c.Check(assetImporter))
                .ToList();
            if (configs.Count == 0) return;
            configs.Sort((c1, c2) => c1.ConfigSortOrder.CompareTo(c2.ConfigSortOrder));

            TexturePreprocessorConfig config = configs[0];
            TextureImporterSettings dest = new TextureImporterSettings();
            textureImporter.ReadTextureSettings(dest);
            dest.spriteGenerateFallbackPhysicsShape = config.GeneratePhysicsShape;
            textureImporter.SetTextureSettings(dest);
            textureImporter.sRGBTexture = config.sRGB;
            textureImporter.alphaSource = config.AlphaSource;
            textureImporter.alphaIsTransparency = config.AlphaIsTransparency;
            textureImporter.ignorePngGamma = config.IgnorePNGFileGamma;
            textureImporter.npotScale = config.NPOTScale;
            textureImporter.isReadable = config.ReadWrite;
            textureImporter.streamingMipmaps = config.StreamingMipmaps;
            textureImporter.vtOnly = config.VirtualTextureOnly;
            textureImporter.mipmapEnabled = config.GenerateMipMaps;
            textureImporter.borderMipmap = config.BorderMipMaps;
            textureImporter.mipmapFilter = config.MipMapFiltering;
            textureImporter.mipMapsPreserveCoverage = config.MipMapPreserveCoverage;
            textureImporter.fadeout = config.FadeoutMipMaps;
            textureImporter.filterMode = config.FilterMode;
            textureImporter.anisoLevel = config.AnisoLevel;
            int w, h;
            textureImporter.GetSourceTextureWidthAndHeight(out w, out h);
            int textureSize = Mathf.NextPowerOfTwo(Mathf.Max(w, h));
            textureSize = Mathf.Min(textureSize, config.MaxSize);
            config.PlatformsRegexList.ForEach(name =>
            {
                bool haveAlpha = textureImporter.DoesSourceTextureHaveAlpha();
                if (name == "Default")
                {
                    textureImporter.SetPlatformTextureSettings(new TextureImporterPlatformSettings
                    {
                        name = textureImporter.GetDefaultPlatformTextureSettings().name,
                        maxTextureSize = textureSize,
                        resizeAlgorithm = config.ResizeAlgorithm,
                        format = TextureImporterFormat.Automatic,
                        compressionQuality = (int)config.CompressorQuality,
                        crunchedCompression = false,
                        allowsAlphaSplitting = false,
                    });
                }
                else if (name == "PC")
                {
                    textureImporter.SetPlatformTextureSettings(new TextureImporterPlatformSettings
                    {
                        name = name,
                        overridden = true,
                        maxTextureSize = textureSize,
                        resizeAlgorithm = config.ResizeAlgorithm,
                        format = haveAlpha ? config.PCRGBAFormat : config.PCRGBFormat,
                        compressionQuality = (int)config.CompressorQuality,
                        crunchedCompression = false,
                        allowsAlphaSplitting = false,
                    });
                }
                else
                {
                    textureImporter.SetPlatformTextureSettings(new TextureImporterPlatformSettings
                    {
                        name = name,
                        overridden = true,
                        maxTextureSize = textureSize,
                        resizeAlgorithm = config.ResizeAlgorithm,
                        format = haveAlpha ? config.MobileRGBAFormat : config.MobileRGBFormat,
                        compressionQuality = (int)config.CompressorQuality,
                        crunchedCompression = false,
                        allowsAlphaSplitting = false,
                        androidETC2FallbackOverride = config.OverrideETC2Fallback
                    });
                }
            });
        }
    }
}
