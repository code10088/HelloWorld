//using System.Collections.Generic;
//using System.Linq;
//using UnityEditor;
//using UnityEngine;

//namespace AssetPreprocessor.Scripts.Editor
//{
//    public class ModelPreprocessor : AssetPostprocessor
//    {
//        private void OnPreprocessModel()
//        {
//        }
        
//        /// <summary>
//        /// https://docs.unity3d.com/ScriptReference/AssetPostprocessor.OnPostprocessModel.html
//        /// </summary>
//        /// <param name="gameObject">GameObject of the model.</param>
//        private void OnPostprocessModel(GameObject model)
//        {
//            var modelImporter = (ModelImporter) assetImporter;
//            var modelName = AssetPreprocessorUtils.GetAssetNameFromPath(modelImporter.assetPath);

//            var configs = AssetPreprocessorUtils.GetScriptableObjectsOfType<ModelPreprocessorConfig>();

//            if (configs.Count == 0)
//            {
//                return;
//            }
            
//            configs = configs
//                .Where(conf => conf.Check(assetImporter))
//                .ToList();
            
//            configs.Sort((config1, config2) => config1.ConfigSortOrder.CompareTo(config2.ConfigSortOrder));

//            ModelPreprocessorConfig config = null;

//            for (var i = 0; i < configs.Count; i++)
//            {
//                var configToTest = configs[i];

//                // Found matching config.
//                config = configToTest;

//                Debug.Log($"Processing: {modelName}", model);
//                Debug.Log($"Using: {config.name}", config);
//                break;
//            }

//            // If could not find a matching config, don't process the asset.
//            if (config == null) return;

//            modelImporter.sortHierarchyByName = config.SortHierarchyByName;
//            modelImporter.importLights = config.ImportLights;
//            modelImporter.importVisibility = config.ImportVisibility;
//            modelImporter.importCameras = config.ImportCameras;
//            modelImporter.isReadable = config.EnableReadWrite;
//            modelImporter.meshCompression = config.MeshCompression;
//            modelImporter.resampleCurves = config.ResampleCurves;
            
//            if (config.ForceGenerateLightmapUVs)
//            {
//                modelImporter.generateSecondaryUV = true;
//            }
            
//            if (modelImporter.importAnimation)
//            {
//                modelImporter.animationCompression = config.ModelImporterAnimationCompression;
//            }

//            #if UNITY_2020_1_OR_NEWER
//            modelImporter.materialImportMode = config.ModelImporterMaterialImportMode;
//            #else
//            modelImporter.importMaterials = config.ImportMaterials;
//            #endif

//            if (modelImporter.importBlendShapes != config.ImportBlendShapes)
//            {
//                modelImporter.importBlendShapes = config.ImportBlendShapes;
//            }
            
//            if (config.EnableAnimationPreprocessing &&
//                modelImporter.animationType != config.ModelImporterAnimationType)
//            {
//                modelImporter.animationType = config.ModelImporterAnimationType;
//            }

//            if (config.EnableRigPreprocessing)
//            {
//                modelImporter.optimizeGameObjects = config.OptimizeGameObjects;
//                modelImporter.extraExposedTransformPaths = config.ExtraExposedTransformPaths;
//                EditorUtility.SetDirty(model);
//            }
            
//            if (config.EnableAnimationPreprocessing &&
//                modelImporter.animationType == ModelImporterAnimationType.Human)
//            {
//                var clips = modelImporter.clipAnimations.ToArray();
                
//                // Enable the bones for ALL of the model's animation clips.
//                for (var i = 0; i < clips.Length; i++)
//                {
//                    var clip = clips[i];

//                    if (clip.maskType != ClipAnimationMaskType.CreateFromThisModel) continue;
                    
//                    var serializedObject = new SerializedObject(modelImporter);
                    
//                    var transformMask = serializedObject.FindProperty($"m_ClipAnimations.Array.data[{i}].transformMask");

//                    var arrayProperty = transformMask.FindPropertyRelative("Array");

//                    for (var j = 0; j < arrayProperty.arraySize; j++)
//                    {
//                        var element = arrayProperty.GetArrayElementAtIndex(j);
                        
//                        var bonePath = element.FindPropertyRelative("m_Path").stringValue;
                        
//                        var shouldEnable = false;
                        
//                        if (AssetPreprocessorUtils.DoesRegexStringListMatchString(config.MaskBonesToEnable, bonePath))
//                        {
//                            Debug.Log($"{clip.name} - Enabling Mask bone: {bonePath}", model);

//                            shouldEnable = true;
//                        }
                        
//                        element.FindPropertyRelative("m_Weight").floatValue = shouldEnable ? 1 : 0;
                        
//                        serializedObject.ApplyModifiedProperties();
//                    }
//                }
                    
//                EditorUtility.SetDirty(model);
//            }

//            Debug.Log($"Processed: {model.name}", model);
//        }
        
//        private void OnPreprocessAnimation()
//        {
//            var modelImporter = assetImporter as ModelImporter;
            
//            if (modelImporter.defaultClipAnimations.Length == 0) return;
            
//            var modelName = AssetPreprocessorUtils.GetAssetNameFromPath(modelImporter.assetPath);
            
//            var configs = AssetPreprocessorUtils.GetScriptableObjectsOfType<ModelPreprocessorConfig>();

//            if (configs.Count == 0)
//            {
//                Debug.Log($"Could not find a {nameof(ModelPreprocessorConfig)} in project.");

//                return;
//            }
            
//            configs = configs
//                .Where(conf => conf.Check(assetImporter))
//                .Where(conf => conf.EnableAnimationPreprocessing)
//                .ToList();
            
//            configs.Sort((config1, config2) => config1.ConfigSortOrder.CompareTo(config2.ConfigSortOrder));

//            ModelPreprocessorConfig config = null;

//            for (var i = 0; i < configs.Count; i++)
//            {
//                var configToTest = configs[i];

//                // Found matching config.
//                config = configToTest;
//                break;
//            }

//            // If could not find a matching config, don't process the asset.
//            if (config == null) return;
            
//            if (modelImporter.clipAnimations.Length > 0 && !config.ForcePreprocess)
//            {
//                Debug.Log($"Skipping Animation Preprocess - {modelName} - Animation has been preprocessed already before.", config);
                
//                return;
//            }
            
//            Debug.Log($"Processing animations for: {modelName}");
//            Debug.Log($"Using: {config.name}", config);
            
//            var clips = modelImporter.defaultClipAnimations.ToArray();

//            for (var i = 0; i < clips.Length; i++)
//            {
//                var clip = clips[i];
                
//                clip.keepOriginalPositionXZ = config.KeepOriginalPositionXZ;
//                clip.keepOriginalOrientation = config.KeepOriginalOrientation;
//                clip.keepOriginalPositionY = config.KeepOriginalPositionY;
                
//                clip.maskType = config.ClipAnimationMaskType;
                
//                clips[i] = clip;
//            }

//            modelImporter.clipAnimations = clips;
//        }
//    }
//}
