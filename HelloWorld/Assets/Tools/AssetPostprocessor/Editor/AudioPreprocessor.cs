using System.Linq;
using UnityEditor;
using UnityEngine;

namespace AssetPreprocessor.Scripts.Editor
{
    public class AudioPreprocessor : AssetPostprocessor
    {
        private void OnPostprocessAudio(AudioClip audioClip)
        {
            var audioImporter = assetImporter as AudioImporter;

            var assetPath = audioImporter.assetPath;
            var configs = AssetDatabase.FindAssets("t:ScriptableObject")
                .ToList()
                .Select(guid => AssetDatabase.GUIDToAssetPath(guid))
                .Select(path => AssetDatabase.LoadAssetAtPath<AudioPreprocessorConfig>(path))
                .Where(obj => obj)
                .Where(t => t is AudioPreprocessorConfig)
                .Where(c => c.Check(assetImporter))
                .ToList();
            if (configs.Count == 0) return;
            configs.Sort((c1, c2) => c1.ConfigSortOrder.CompareTo(c2.ConfigSortOrder));

            AudioPreprocessorConfig config = configs[0];
            audioImporter.forceToMono = config.ForceToMono;
            audioImporter.loadInBackground = config.LoadInBackground;
            audioImporter.ambisonic = config.Ambisonic;
            audioImporter.preloadAudioData = config.PreloadAudioData;

            audioImporter.defaultSampleSettings = new AudioImporterSampleSettings
            {
                loadType = config.LoadType,
                compressionFormat = config.CompressionFormat,
                quality = config.Quality,
                sampleRateSetting = config.SampleRateSetting,
                sampleRateOverride = config.SampleRate,
            };
            audioImporter.SetOverrideSampleSettings("PC", new AudioImporterSampleSettings
            {
                loadType = config.LoadType,
                compressionFormat = config.CompressionFormat,
                quality = config.Quality,
                sampleRateSetting = config.SampleRateSetting,
                sampleRateOverride = config.SampleRate,
            });
            audioImporter.SetOverrideSampleSettings("Android", new AudioImporterSampleSettings
            {
                loadType = config.LoadType,
                compressionFormat = config.CompressionFormat,
                quality = config.Quality,
                sampleRateSetting = config.SampleRateSetting,
                sampleRateOverride = config.SampleRate,
            });
            audioImporter.SetOverrideSampleSettings("iOS", new AudioImporterSampleSettings
            {
                loadType = config.LoadType,
                compressionFormat = config.CompressionFormat,
                quality = config.Quality,
                sampleRateSetting = config.SampleRateSetting,
                sampleRateOverride = config.SampleRate,
            });
            audioImporter.SetOverrideSampleSettings("WebGL", new AudioImporterSampleSettings
            {
                loadType = config.LoadType,
                compressionFormat = AudioCompressionFormat.AAC,
                quality = config.Quality,
                sampleRateSetting = config.SampleRateSetting,
                sampleRateOverride = config.SampleRate,
            });
        }
    }
}
