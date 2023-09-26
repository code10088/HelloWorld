using UnityEditor;
using UnityEngine;

namespace AssetPreprocessor.Scripts.Editor
{
    [CreateAssetMenu(menuName="ScriptableObject/AssetPreprocessor/ModelPreprocessorConfig")]
    public class ModelPreprocessorConfig : BasePreprocessorConfig
    {
        public float ScaleFactor = 1f;
        public bool ConvertUnits = true;
        public bool BakeAxisConversion = false;
        public bool ImportBlendShapes = false;
        public bool ImportVisibility = true;
        public bool ImportCameras = false;
        public bool ImportLights = false;
        public bool PreserveHierarchy = true;
        public bool SortHierarchyByName = false;
        public ModelImporterMeshCompression MeshCompression = ModelImporterMeshCompression.Off;
        public bool ReadWrite = false;
        public MeshOptimizationFlags OptimizeMesh = MeshOptimizationFlags.Everything;
        public bool GenerateColliders = false;
        public bool KeepQuads = false;
        public bool WeldVertices = true;
        public ModelImporterIndexFormat IndexFormat = ModelImporterIndexFormat.Auto;
        public bool LegacyBlendShapeNormals = true;
        public ModelImporterNormals Normals = ModelImporterNormals.Import;
        public ModelImporterNormalCalculationMode NormalsMode = ModelImporterNormalCalculationMode.AreaAndAngleWeighted;
        public ModelImporterNormalSmoothingSource SmoothnessSource = ModelImporterNormalSmoothingSource.PreferSmoothingGroups;
        public int SmoothingAngle = 60;
        public ModelImporterTangents Tangents = ModelImporterTangents.Import;
        public bool SwapUVs = false;
        public bool GenerateLightmapUVs = false;
        [Tooltip("暂不使用人型动画")]
        public ModelImporterAnimationType AnimationType = ModelImporterAnimationType.Generic;
        public ModelImporterAvatarSetup AvatarDefinition = ModelImporterAvatarSetup.NoAvatar;
        public ModelImporterSkinWeights SkinWeights = ModelImporterSkinWeights.Standard;
        public bool StripBones = true;
        public bool ImportConstraints = false;
        public bool ImportAnimation = false;
        public ModelImporterMaterialImportMode MaterialCreationMode = ModelImporterMaterialImportMode.None;
    }
}
