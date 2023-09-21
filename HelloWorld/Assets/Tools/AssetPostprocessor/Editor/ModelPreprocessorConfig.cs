using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace AssetPreprocessor.Scripts.Editor
{
    [CreateAssetMenu(menuName="ScriptableObject/AssetPreprocessor/ModelPreprocessorConfig")]
    public class ModelPreprocessorConfig : BasePreprocessorConfig
    {
        [Header("Import Settings")]
        #if UNITY_2020_1_OR_NEWER
        public ModelImporterMaterialImportMode ModelImporterMaterialImportMode = ModelImporterMaterialImportMode.ImportStandard;
        #else
        public bool ImportMaterials;
        #endif
        
        public bool ImportBlendShapes;
        public bool SortHierarchyByName = true;
        public bool ForceGenerateLightmapUVs;
        public bool EnableReadWrite = true;
        public ModelImporterMeshCompression MeshCompression = ModelImporterMeshCompression.Off;

        [Header("Animation Settings")]
        public bool ResampleCurves = true;
        public ModelImporterAnimationCompression ModelImporterAnimationCompression = ModelImporterAnimationCompression.Optimal;
        
        [Header("Animation Processing")]
        public bool EnableAnimationPreprocessing;
        #if ODIN_INSPECTOR
        [ShowIf(nameof(EnableAnimationPreprocessing))]
        #endif
        public ModelImporterAnimationType ModelImporterAnimationType = ModelImporterAnimationType.Generic;
        #if ODIN_INSPECTOR
        [ShowIf(nameof(EnableAnimationPreprocessing))]
        #endif
        public bool KeepOriginalOrientation;
        #if ODIN_INSPECTOR
        [ShowIf(nameof(EnableAnimationPreprocessing))]
        #endif
        public bool KeepOriginalPositionXZ;
        #if ODIN_INSPECTOR
        [ShowIf(nameof(EnableAnimationPreprocessing))]
        #endif
        public bool KeepOriginalPositionY = true;
        #if ODIN_INSPECTOR
        [ShowIf(nameof(EnableAnimationPreprocessing))]
        #endif
        public ClipAnimationMaskType ClipAnimationMaskType = ClipAnimationMaskType.None;
        #if ODIN_INSPECTOR
        [ShowIf(nameof(ClipAnimationMaskType), ClipAnimationMaskType.CreateFromThisModel)]
        [ShowIf(nameof(EnableAnimationPreprocessing))]
        #endif
        public List<string> MaskBonesToEnable;

        [Header("Rig Settings")]
        public bool EnableRigPreprocessing;
        #if ODIN_INSPECTOR
        [ShowIf(nameof(EnableRigPreprocessing))]
        #endif
        public bool OptimizeGameObjects;
        #if ODIN_INSPECTOR
        [ShowIf(nameof(EnableRigPreprocessing))]
        #endif
        public string[] ExtraExposedTransformPaths;
        
        [Header("Scene Settings")]
        public bool ImportLights;
        public bool ImportVisibility;
        public bool ImportCameras;
    }
}
