using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace AssetPreprocessor.Scripts.Editor
{
    [CreateAssetMenu(menuName="Tools/CreateTexturePreprocessorConfig")]
    public class TexturePreprocessorConfig : BasePreprocessorConfig
    {
        [Header("贴图设置")]
        public bool sRGB = true;
        public TextureImporterAlphaSource AlphaSource = TextureImporterAlphaSource.FromInput;
        public bool AlphaIsTransparency = false;
        public bool IgnorePNGFileGamma = false;
        public bool GeneratePhysicsShape = false;
        [Header("Advanced")]
        public TextureImporterNPOTScale NPOTScale = TextureImporterNPOTScale.ToNearest;
        public bool ReadWrite = false;
        public bool StreamingMipmaps = false;
        public bool VirtualTextureOnly = false;
        public bool GenerateMipMaps = false;
        public bool BorderMipMaps = false;
        public TextureImporterMipFilter MipMapFiltering = TextureImporterMipFilter.BoxFilter;
        public bool MipMapPreserveCoverage = false;
        public bool FadeoutMipMaps = false;

        [Header("FilterMode")]
        public FilterMode FilterMode = FilterMode.Bilinear;
        public int AnisoLevel = 0;

        [Header("平台")]
        public List<string> PlatformsRegexList = new List<string> { "Default", "PC", "Android", "iOS", };
        public int MaxSize = 2048;
        public TextureResizeAlgorithm ResizeAlgorithm = TextureResizeAlgorithm.Mitchell;
        public TextureImporterFormat PCRGBFormat = TextureImporterFormat.DXT1;
        public TextureImporterFormat PCRGBAFormat = TextureImporterFormat.DXT5;
        [Tooltip("普通贴图无Alpha6*6有Alpha5*5 通道贴图6*6 细节贴图4*4 法线贴图4*4")]
        public TextureImporterFormat MobileRGBFormat = TextureImporterFormat.ASTC_6x6;
        public TextureImporterFormat MobileRGBAFormat = TextureImporterFormat.ASTC_6x6;
        public TextureCompressionQuality CompressorQuality = TextureCompressionQuality.Normal;
        public AndroidETC2FallbackOverride OverrideETC2Fallback = AndroidETC2FallbackOverride.UseBuildSettings;
    }
}

