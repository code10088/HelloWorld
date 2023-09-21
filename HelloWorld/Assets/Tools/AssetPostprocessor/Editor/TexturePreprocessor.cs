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
            var textureImporter = assetImporter as TextureImporter;

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
            bool haveAlpha = textureImporter.DoesSourceTextureHaveAlpha();
            textureImporter.SetPlatformTextureSettings(new TextureImporterPlatformSettings
            {
                name = "PC",
                overridden = true,
                maxTextureSize = textureSize,
                resizeAlgorithm = config.ResizeAlgorithm,
                format = haveAlpha ? config.PCRGBAFormat : config.PCRGBFormat,
                compressionQuality = (int)config.CompressorQuality,
                crunchedCompression = false,
                allowsAlphaSplitting = false,
            });
            textureImporter.SetPlatformTextureSettings(new TextureImporterPlatformSettings
            {
                name = "Android",
                overridden = true,
                maxTextureSize = textureSize,
                resizeAlgorithm = config.ResizeAlgorithm,
                format = haveAlpha ? config.MobileRGBAFormat : config.MobileRGBFormat,
                compressionQuality = (int)config.CompressorQuality,
                crunchedCompression = false,
                allowsAlphaSplitting = false,
                androidETC2FallbackOverride = config.OverrideETC2Fallback
            });
            textureImporter.SetPlatformTextureSettings(new TextureImporterPlatformSettings
            {
                name = "iOS",
                overridden = true,
                maxTextureSize = textureSize,
                resizeAlgorithm = config.ResizeAlgorithm,
                format = haveAlpha ? config.MobileRGBAFormat : config.MobileRGBFormat,
                compressionQuality = (int)config.CompressorQuality,
                crunchedCompression = false,
                allowsAlphaSplitting = false,
                androidETC2FallbackOverride = config.OverrideETC2Fallback
            });
            textureImporter.SetPlatformTextureSettings(new TextureImporterPlatformSettings
            {
                name = "WebGL",
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
    }
}
