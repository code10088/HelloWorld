// GPU Instancer Pro
// Copyright (c) GurBu Technologies

using System;
using System.Collections.Generic;
using UnityEngine;
using System.Text.RegularExpressions;
using Unity.Collections;
using UnityEngine.Jobs;
using UnityEngine.Rendering;
using System.IO;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace GPUInstancerPro
{
    public static class GPUITextureUtility
    {
        public static void CopyTextureWithComputeShader(Texture source, Texture destination, int offsetX, int sourceMip = 0, int destinationMip = 0)
        {
            int sourceW = source.width;
            int sourceH = source.height;
            for (int i = 0; i < sourceMip; i++)
            {
                sourceW >>= 1;
                sourceH >>= 1;
            }

            ComputeShader cs = GPUIConstants.CS_TextureUtility;
            int kernelIndex = 0;

            cs.SetTexture(kernelIndex, GPUIConstants.PROP_source, source, sourceMip);
            cs.SetTexture(kernelIndex, GPUIConstants.PROP_destination, destination, destinationMip);

            cs.SetInt(GPUIConstants.PROP_offsetX, offsetX);
            cs.SetInt(GPUIConstants.PROP_sourceSizeX, sourceW);
            cs.SetInt(GPUIConstants.PROP_sourceSizeY, sourceH);

            cs.DispatchXY(kernelIndex, sourceW, sourceH);
        }

        public static void CopyTextureSamplerWithComputeShader(Texture source, Texture destination)
        {
            int destinationW = destination.width;
            int destinationH = destination.height;

            ComputeShader cs = GPUIConstants.CS_TextureUtility;
            int kernelIndex = 1;

            cs.SetTexture(kernelIndex, GPUIConstants.PROP_source, source);
            cs.SetTexture(kernelIndex, GPUIConstants.PROP_destination, destination);

            cs.SetInt(GPUIConstants.PROP_destinationSizeX, destinationW);
            cs.SetInt(GPUIConstants.PROP_destinationSizeY, destinationH);

            cs.DispatchXY(kernelIndex, destinationW, destinationH);
        }

        public static void SetTextureDataWithComputeShaderSingleChannel(GraphicsBuffer textureData, Texture destination)
        {
            int destinationW = destination.width;
            int destinationH = destination.height;

            ComputeShader cs = GPUIConstants.CS_TextureUtility;
            int kernelIndex = 2;

            cs.SetBuffer(kernelIndex, GPUIConstants.PROP_textureDataSingleChannel, textureData);
            cs.SetTexture(kernelIndex, GPUIConstants.PROP_destination, destination);

            cs.SetInt(GPUIConstants.PROP_destinationSizeX, destinationW);
            cs.SetInt(GPUIConstants.PROP_destinationSizeY, destinationH);

            cs.DispatchXY(kernelIndex, destinationW, destinationH);
        }

        public static Texture2D RenderTextureToTexture2D(RenderTexture renderTexture, TextureFormat textureFormat, bool linear, FilterMode filterMode = FilterMode.Bilinear)
        {
            Texture2D texture2d = new Texture2D(renderTexture.width, renderTexture.height, textureFormat, false, linear)
            {
                name = renderTexture.name,
                filterMode = filterMode
            };
            RenderTexture activeRT = RenderTexture.active;
            RenderTexture.active = renderTexture;
            texture2d.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
            texture2d.Apply(false);
            RenderTexture.active = activeRT;

            return texture2d;
        }

#if UNITY_EDITOR
        public static Texture2D SaveRenderTextureToPNG(RenderTexture renderTexture, TextureFormat textureFormat, string folderPath, string fileName = null, bool linear = false, FilterMode filterMode = FilterMode.Bilinear, TextureImporterType textureImporterType = TextureImporterType.Default, int maxTextureSize = 2048, bool mipmapEnabled = true, bool sRGBTexture = true, bool alphaIsTransparency = false, TextureImporterCompression compression = TextureImporterCompression.Compressed, bool isReadable = false)
        {
            if (string.IsNullOrEmpty(fileName))
                fileName = string.IsNullOrEmpty(renderTexture.name) ? "RenderTexture" : renderTexture.name;

            return SaveTexture2DToPNG(RenderTextureToTexture2D(renderTexture, textureFormat, linear, filterMode), folderPath, fileName, filterMode, textureImporterType, maxTextureSize, mipmapEnabled, sRGBTexture, alphaIsTransparency, compression, isReadable);
        }

        public static Texture2D SaveTexture2DToPNG(Texture2D texture2D, string folderPath, string fileName = null, FilterMode filterMode = FilterMode.Bilinear, TextureImporterType textureImporterType = TextureImporterType.Default, int maxTextureSize = 2048, bool mipmapEnabled = true, bool sRGBTexture = true, bool alphaIsTransparency = false, TextureImporterCompression compression = TextureImporterCompression.Compressed, bool isReadable = false)
        {
            return SaveTextureDataToFile<Texture2D>(".png", texture2D.EncodeToPNG(), texture2D.name, folderPath, fileName, true, textureImporterType, maxTextureSize, mipmapEnabled, sRGBTexture, alphaIsTransparency, filterMode, compression, isReadable);
        }

        public static T SaveTextureDataToFile<T>(string fileExtension, byte[] textureData, string textureName, string folderPath, string fileName = null, bool modifyTextureImporter = true, TextureImporterType textureImporterType = TextureImporterType.Default, int maxTextureSize = 2048, bool mipmapEnabled = true, bool sRGBTexture = true, bool alphaIsTransparency = false, FilterMode filterMode = FilterMode.Bilinear, TextureImporterCompression compression = TextureImporterCompression.Compressed, bool isReadable = false) where T : UnityEngine.Object
        {
            if (!Directory.Exists(folderPath))
                Directory.CreateDirectory(folderPath);
            if (string.IsNullOrEmpty(fileName))
                fileName = string.IsNullOrEmpty(textureName) ? "Texture2D" : textureName;
            string assetPath = folderPath + fileName + fileExtension;
            File.WriteAllBytes(assetPath, textureData);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            if (modifyTextureImporter)
                ApplyTextureImporterSettings(assetPath, textureImporterType, maxTextureSize, mipmapEnabled, sRGBTexture, alphaIsTransparency, filterMode, compression, isReadable);

            AssetDatabase.ImportAsset(assetPath);

            return AssetDatabase.LoadAssetAtPath<T>(assetPath);
        }

        public static void ApplyTextureImporterSettings(string assetPath, TextureImporterType textureImporterType = TextureImporterType.Default, int maxTextureSize = 2048, bool mipmapEnabled = true, bool sRGBTexture = true, bool alphaIsTransparency = false, FilterMode filterMode = FilterMode.Bilinear, TextureImporterCompression compression = TextureImporterCompression.Compressed, bool isReadable = false)
        {
            AssetImporter assetImporter = AssetImporter.GetAtPath(assetPath);
            if (assetImporter == null || assetImporter is not TextureImporter importer)
                return;
            importer.maxTextureSize = Mathf.NextPowerOfTwo(maxTextureSize);
            importer.textureType = textureImporterType;
            importer.mipmapEnabled = mipmapEnabled;
            if (!sRGBTexture)
                importer.sRGBTexture = false;
            importer.mipMapsPreserveCoverage = mipmapEnabled;
            importer.alphaIsTransparency = alphaIsTransparency;
            importer.filterMode = filterMode;
            importer.textureCompression = compression;
            importer.isReadable = isReadable;
            if (importer.maxTextureSize != maxTextureSize)
                importer.npotScale = TextureImporterNPOTScale.None;

            if (textureImporterType == TextureImporterType.SingleChannel)
            {
                TextureImporterSettings textureImporterSettings = new TextureImporterSettings();
                importer.ReadTextureSettings(textureImporterSettings);
                textureImporterSettings.singleChannelComponent = TextureImporterSingleChannelComponent.Red;
                importer.SetTextureSettings(textureImporterSettings);
            }

            AssetDatabase.ImportAsset(assetPath);
        }
#endif

        public static void DestroyRenderTexture(this RenderTexture rt)
        {
            if (rt == null)
                return;
            if (RenderTexture.active == rt)
                RenderTexture.active = null;
            rt.Release();
            GPUIUtility.DestroyGeneric(rt);
        }

        public static void ClearRenderTexture(this RenderTexture rt)
        {
            ClearRenderTexture(rt, Color.clear);
        }

        public static void ClearRenderTexture(this RenderTexture rt, Color color)
        {
            RenderTexture activeRT = RenderTexture.active;
            RenderTexture.active = rt;
            GL.Clear(true, true, color);
            RenderTexture.active = activeRT;
        }
    }
}