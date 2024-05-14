using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace AssetPreprocessor.Scripts.Editor
{
    [CreateAssetMenu(menuName="Tools/CreateTexturePreprocessorConfig")]
    public class TexturePreprocessorConfig : BasePreprocessorConfig
    {
        [Header("贴图设置")]
        public TextureImporterType TextureType = TextureImporterType.Default;

        [Header("Default")]
        public TextureImporterNPOTScale NPOTScale = TextureImporterNPOTScale.ToNearest;
        public bool StreamingMipmaps = false;
        public bool VirtualTextureOnly = false;

        [Header("Sprite")]
        public SpriteImportMode SpriteMode = SpriteImportMode.Single;
        public float PixelsPerUnit = 100;
        public SpriteMeshType MeshType = SpriteMeshType.Tight;
        public uint ExtrudeEdges = 1;
        public Vector2 spritePivot = Vector2.zero;
        public bool GeneratePhysicsShape = false;

        [Header("Common")]
        public bool sRGB = true;
        public TextureImporterAlphaSource AlphaSource = TextureImporterAlphaSource.FromInput;
        public bool AlphaIsTransparency = true;
        public bool IgnorePNGFileGamma = false;
        public bool ReadWrite = false;
        public bool GenerateMipMaps = false;
        public bool BorderMipMaps = false;
        public TextureImporterMipFilter MipMapFiltering = TextureImporterMipFilter.BoxFilter;
        public bool MipMapPreserveCoverage = false;
        public bool FadeoutMipMaps = false;

        [Header("FilterMode")]
        public FilterMode FilterMode = FilterMode.Bilinear;
        public int AnisoLevel = 1;

        [Header("格式")]
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

