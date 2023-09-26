using System.Linq;
using System.Reflection;
using UnityEditor;

namespace AssetPreprocessor.Scripts.Editor
{
    public class ModelPreprocessor : AssetPostprocessor
    {
        private void OnPreprocessModel()
        {
            var modelImporter = assetImporter as ModelImporter;

            var configs = AssetDatabase.FindAssets("t:ScriptableObject")
                .ToList()
                .Select(guid => AssetDatabase.GUIDToAssetPath(guid))
                .Select(path => AssetDatabase.LoadAssetAtPath<ModelPreprocessorConfig>(path))
                .Where(obj => obj)
                .Where(t => t is ModelPreprocessorConfig)
                .Where(c => c.Check(assetImporter))
                .ToList();
            if (configs.Count == 0) return;
            configs.Sort((c1, c2) => c1.ConfigSortOrder.CompareTo(c2.ConfigSortOrder));

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
            modelImporter.materialImportMode = config.MaterialCreationMode;
        }
    }
}
