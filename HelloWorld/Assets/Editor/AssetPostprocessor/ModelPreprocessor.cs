using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEngine;

namespace AssetPreprocessor.Scripts.Editor
{
    public class ModelPreprocessor : AssetPostprocessor
    {
        private void OnPreprocessModel()
        {
            var modelImporter = assetImporter as ModelImporter;

            var configs = AssetDatabase.FindAssets("t:ModelPreprocessorConfig")
                .Select(guid => AssetDatabase.GUIDToAssetPath(guid))
                .Select(path => AssetDatabase.LoadAssetAtPath<ModelPreprocessorConfig>(path))
                .Where(c => c.Check(assetImporter))
                .ToList();
            if (configs.Count == 0) return;

            ModelPreprocessorConfig config = configs[0];
            modelImporter.globalScale = config.ScaleFactor;
            modelImporter.useFileUnits = config.ConvertUnits;
            modelImporter.bakeAxisConversion = config.BakeAxisConversion;
            modelImporter.importBlendShapes = config.ImportBlendShapes;
            modelImporter.importVisibility = config.ImportVisibility;
            modelImporter.importCameras = config.ImportCameras;
            modelImporter.importLights = config.ImportLights;
            modelImporter.preserveHierarchy = config.PreserveHierarchy;
            modelImporter.sortHierarchyByName = config.SortHierarchyByName;
            modelImporter.meshCompression = config.MeshCompression;
            modelImporter.isReadable = config.ReadWrite;
            modelImporter.meshOptimizationFlags = config.OptimizeMesh;
            modelImporter.addCollider = config.GenerateColliders;
            modelImporter.keepQuads = config.KeepQuads;
            modelImporter.weldVertices = config.WeldVertices;
            modelImporter.indexFormat = config.IndexFormat;
            string functionName = "legacyComputeAllNormalsFromSmoothingGroupsWhenMeshHasBlendShapes";
            PropertyInfo prop = modelImporter.GetType().GetProperty(functionName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            prop.SetValue(modelImporter, config.LegacyBlendShapeNormals);
            modelImporter.importNormals = config.Normals;
            modelImporter.normalCalculationMode = config.NormalsMode;
            modelImporter.normalSmoothingSource = config.SmoothnessSource;
            modelImporter.normalSmoothingAngle = config.SmoothingAngle;
            modelImporter.importTangents = config.Tangents;
            modelImporter.swapUVChannels = config.SwapUVs;
            modelImporter.generateSecondaryUV = config.GenerateLightmapUVs;
            modelImporter.animationType = config.AnimationType;
            modelImporter.avatarSetup = config.AvatarDefinition;
            modelImporter.skinWeights = config.SkinWeights;
            modelImporter.optimizeBones = config.StripBones;
            modelImporter.importConstraints = config.ImportConstraints;
            modelImporter.importAnimation = config.ImportAnimation;
            modelImporter.importAnimatedCustomProperties = config.ImportAnimatedCustomProperties;
            modelImporter.resampleCurves = config.ResampleCurves;
            modelImporter.animationCompression = config.AnimationCompression;
            modelImporter.animationRotationError = config.AnimationRotationError;
            modelImporter.animationPositionError = config.AnimationPositionError;
            modelImporter.animationScaleError = config.AnimationScaleError;
            modelImporter.removeConstantScaleCurves = config.RemoveConstantScaleCurves;
            modelImporter.materialImportMode = config.MaterialCreationMode;
            modelImporter.materialLocation = config.MaterialLocation;
        }
        private void OnPreprocessMaterialDescription(MaterialDescription description, Material material, AnimationClip[] materialAnimation)
        {
            material.shader = Shader.Find("URP/Template");
        }
    }
}
