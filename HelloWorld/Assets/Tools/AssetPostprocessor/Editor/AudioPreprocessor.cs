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
            var configs = AssetDatabase.FindAssets("t:AudioPreprocessorConfig")
                .Select(guid => AssetDatabase.GUIDToAssetPath(guid))
                .Select(path => AssetDatabase.LoadAssetAtPath<AudioPreprocessorConfig>(path))
                .Where(c => c.Check(assetImporter))
                .ToList();
            if (configs.Count == 0) return;

            AudioPreprocessorConfig config = configs[0];
            audioImporter.forceToMono = config.ForceToMono;
            audioImporter.loadInBackground = config.LoadInBackground;
            audioImporter.ambisonic = config.Ambisonic;

            audioImporter.defaultSampleSettings = new AudioImporterSampleSettings
            {
                loadType = config.LoadType,
                preloadAudioData = config.PreloadAudioData,
                compressionFormat = config.CompressionFormat,
                quality = config.Quality,
                sampleRateSetting = config.SampleRateSetting,
                sampleRateOverride = config.SampleRate,
            };
            audioImporter.SetOverrideSampleSettings("PC", new AudioImporterSampleSettings
            {
                loadType = config.LoadType,
                preloadAudioData = config.PreloadAudioData,
                compressionFormat = config.CompressionFormat,
                quality = config.Quality,
                sampleRateSetting = config.SampleRateSetting,
                sampleRateOverride = config.SampleRate,
            });
            audioImporter.SetOverrideSampleSettings("Android", new AudioImporterSampleSettings
            {
                loadType = config.LoadType,
                preloadAudioData = config.PreloadAudioData,
                compressionFormat = config.CompressionFormat,
                quality = config.Quality,
                sampleRateSetting = config.SampleRateSetting,
                sampleRateOverride = config.SampleRate,
            });
            audioImporter.SetOverrideSampleSettings("iOS", new AudioImporterSampleSettings
            {
                loadType = config.LoadType,
                preloadAudioData = config.PreloadAudioData,
                compressionFormat = config.CompressionFormat,
                quality = config.Quality,
                sampleRateSetting = config.SampleRateSetting,
                sampleRateOverride = config.SampleRate,
            });
            audioImporter.SetOverrideSampleSettings("WebGL", new AudioImporterSampleSettings
            {
                loadType = config.LoadType,
                preloadAudioData = config.PreloadAudioData,
                compressionFormat = AudioCompressionFormat.AAC,
                quality = config.Quality,
                sampleRateSetting = config.SampleRateSetting,
                sampleRateOverride = config.SampleRate,
            });
        }
    }
}
